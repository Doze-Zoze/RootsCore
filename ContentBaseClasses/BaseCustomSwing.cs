using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RootsCore.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Prefixes;
using Terraria.ID;
using Terraria.ModLoader;

namespace RootsCore.ContentBaseClasses
{
    /// <summary>
    /// Manages all the required settings for custom sword holdouts done using BaseSwordHoldoutProjectile
    /// </summary>
    public abstract class BaseCustomSwingItem : ModItem
    {
        public virtual int ProjectileType { get; set; }
        public virtual bool SizeModifiers { get; set; } = true;
        public virtual bool RClickAutoswing { get; set; } = false;

        public override void SetStaticDefaults()
        {
            if (RClickAutoswing)
                ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override bool MeleePrefix()
        {
            return SizeModifiers;
        }
        public override void SetDefaults()
        {
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ProjectileType;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Shoot;
            if (SizeModifiers) PrefixLegacy.ItemSets.SwordsHammersAxesPicks[Item.type] = true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.itemTime > 0 || player.ownedProjectileCounts[ProjectileType] > 0)
            {
                return false;
            }
            return base.CanUseItem(player);
        }
    }
    public abstract class BaseCustomSwingProjectile : ModProjectile
    {
        #region Overrideable Fields
        /// <summary>
        /// The width, in degrees, of the sword swing.
        /// Defaults to 180
        /// </summary>
        public virtual int SwingWidth { get; set; } = 180;
        /// <summary>
        /// How many frames the sword swing should take.
        /// Defaults to 20
        /// </summary>
        public virtual int SwingTime { get; set; } = 20;
        /// <summary>
        /// If the swing should alternate directions each use.
        /// Defaults to true
        /// </summary>
        public virtual bool AlternateSwings { get; set; } = true;
        /// <summary>
        /// How far the held sword should be offset from the player
        /// Defaults to 0
        /// </summary>
        public virtual int OffsetDistance { get; set; } = 0;
        /// <summary>
        /// What item this projectile uses as a base.
        /// </summary>
        public virtual Item BaseItem { get; set; }
        /// <summary>
        /// Whether or not this projectile uses a base item at all.
        /// Defaults to true.
        /// </summary>
        public virtual bool UsesBaseItem { get; set; } = true;

        /// <summary>
        /// Length of after-image trail left by the projectile.
        /// Defaults to 0
        /// </summary>
        public virtual int AfterImageLength { get; set; } = 0;

        /// <summary>
        /// Whether or not this should get attack speed bonuses for its class
        /// Defaults to TRUE
        /// </summary>
        public virtual bool UseAttackSpeed { get; set; } = true;

        /// <summary>
        /// Whether or not this should get melee size bonuses (Titan Glove)
        /// Defaults to TRUE
        /// </summary>
        public virtual bool UseMeleeSize { get; set; } = true;

        /// <summary>
        /// How long before the weapon should begin it's actual swing once used
        /// </summary>
        public virtual int StartupTime { get; set; }
        /// <summary>
        /// How long the weapon should "cool down" after swinging before ending the item use
        /// </summary>
        public virtual int CooldownTime { get; set; }
        /// <summary>
        /// Speed at which the projectile should rotate to match the mouse angle during StartupTime.
        /// Set to 0 to disable.
        /// Defaults to 0.5f
        /// </summary>
        public virtual float RotateInStartup { get; set; } = 0.5f;

        /// <summary>
        /// Speed at which the projectile should rotate to match the mouse angle during Cooldown.
        /// Set to 0 to disable.
        /// Defaults to 0.5f
        /// </summary>
        public virtual float RotateInCooldown { get; set; } = 0.5f;

        /// <summary>
        /// What sound to use when the sword begins the actual swing (after startup frames)
        /// </summary>
        public virtual SoundStyle? UseSound { get; set; } = null;
        /// <summary>
        /// The length (from the player) of the projectile's line collision.
        /// This helps to prevent blindspots.
        /// Defaults to 0
        /// </summary>
        public virtual float LineCollisionLength { get; set; }

        public virtual Color AfterImageColor { get; set; } = Color.White;

        #endregion

        #region Fields

        /// <summary>
        /// Angle of the swing. By default, gets set to mouse angle. Can be set in Spawn(IEntitySource) for fixed angles.
        /// </summary>
        public Vector2 Angle { get; set; } = Vector2.Zero;
        /// <summary>
        /// The projectile's center based on offset to the player the previous update
        /// Can be used for effects that stay consistent in motion
        /// </summary>
        public Vector2 OldPlayerOffset { get; set; }
        /// <summary>
        /// Timer for the projectile's entire lifespan
        /// </summary>
        public int TotalTimer { get; set; }
        /// <summary>
        /// Timer for the projectile's swing animation
        /// </summary>
        public int SwingTimer { get; set; }
        public float baseScale;
        /// <summary>
        /// Old weapon scales used to track for trail drawing
        /// </summary>
        public List<float> oldScale = [];
        public List<float> oldProjectileRot = [];
        public List<Vector2> oldProjectilePos = [];
        public int ExistsTime = 20;
        public bool InStartup => TotalTimer < StartupTime;
        public bool InCooldown => TotalTimer > CooldownStartFrame;
        public bool InSwing => !(InStartup || InCooldown);
        public int CooldownStartFrame => SwingTime + StartupTime;
        public int CooldownTimer => TotalTimer - CooldownStartFrame;
        public float StartupCompletion => TotalTimer / (float)StartupTime;
        public float SwingCompletion => SwingTimer / (float)SwingTime;
        public float CooldownCompletion => CooldownTimer / (float)CooldownTime;
        private bool hasFakedOnSpawn = false;

        #endregion

        #region Overridable Methods  
        /// <summary>
        /// Happens after movement but before timer increases. Use as to not cancel the default AI behavior.
        /// </summary>
        public virtual void AdditionalAI() { }
        /// <summary>
        /// Happens at the beginning of AI the first frame.
        /// </summary>
        public virtual void Spawn() { }
        /// <summary>
        /// happens after SetDefaults. Use as not to cancel default SetDefaults behavior.
        /// </summary>
        public virtual void Defaults() { }
        /// <summary>
        /// Returns the swing offset from the center angle in radians. Automatically will be inverted if AlternateSwings is enabled.
        /// </summary>
        /// <returns></returns>
        public virtual float SwingFunction()
        {
            return MathHelper.ToRadians(MathHelper.SmoothStep(-SwingWidth / 2, SwingWidth / 2, SwingTimer / (float)SwingTime));
        }
        #endregion

        #region Overrides
        /// <summary>
        /// DO NOT OVERRIDE IN MOST SITUATIONS
        /// use Defaults() instead.
        /// That will set defaults after everything is set in the base projectile.
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.timeLeft = SwingTime * 2;
            if (UsesBaseItem)
            {
                Projectile.width = Projectile.height = Math.Max(BaseItem.height, BaseItem.width);
            }
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.extraUpdates = 0;
            Projectile.aiStyle = -2;
            Projectile.DamageType = ModLoader.GetMod("CalamityMod").Find<DamageClass>("TrueMeleeDamageClass");
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 100;
            Defaults();
        }
        /// <summary>
        /// This hook runs at the beginning of AI the first time through.
        /// </summary>
        private void FakeOnSpawn()
        {
            Player player = Main.player[Projectile.owner];
            Angle = (player.MountedCenter - player.MouseWorld).SafeNormalize(Vector2.One);
            Projectile.velocity = Vector2.Zero;
            if (Angle.X < 0)
            {
                player.direction = 1;
                Projectile.spriteDirection = 1 * (int)player.gravDir;
            }
            else
            {
                player.direction = -1;
                Projectile.spriteDirection = -1 * (int)player.gravDir;
            }
            if (AlternateSwings && player.GetModPlayer<RootsCorePlayer>().swingCounter % 2 == 1)
            {
                Projectile.spriteDirection *= -1;
            }
            if (AlternateSwings)
            {
                player.GetModPlayer<RootsCorePlayer>().swingCounter++;
            }
            SwingTime = Main.player[Projectile.owner].HeldItem.useTime;
            Spawn();
            StartupTime *= Projectile.MaxUpdates;
            CooldownTime *= Projectile.MaxUpdates;
            SwingTime *= Projectile.MaxUpdates;
            if (UseAttackSpeed)
            {
                var speed = Main.player[Projectile.owner].GetTotalAttackSpeed(Projectile.DamageType);
                if (speed > 3f)
                    speed = 3f;

                if (speed != 0f)
                    speed = 1f / speed;

                SwingTime = (int)(SwingTime * speed);
                if (SwingTime < 1)
                {
                    SwingTime = 1;
                }
                StartupTime = (int)(StartupTime * speed);
                CooldownTime = (int)(CooldownTime * speed);
            }
            if (UseMeleeSize)
            {
                Projectile.scale *= player.GetMeleeScale();
            }
            baseScale = Projectile.scale;
            ExistsTime = SwingTime + StartupTime + CooldownTime;
            Projectile.timeLeft = ExistsTime * 2;
            Projectile.netUpdate = true;
        }

        /// <summary>
        /// DO NOT OVERRIDE IN MOST SITUATIONS
        /// use AdditionalAI() instead
        /// That will run AI code at the right time.
        /// </summary>
        public override void AI()
        {
            if (!hasFakedOnSpawn)
            {
                FakeOnSpawn();
                hasFakedOnSpawn = true;
            }
            Player player = Main.player[Projectile.owner];
            Projectile.gfxOffY = player.gfxOffY;
            RootsCorePlayer modplayer = player.GetModPlayer<RootsCorePlayer>();
            float adust = MathHelper.ToRadians(225);
            if (TotalTimer < StartupTime || TotalTimer > StartupTime + SwingTime)
            {
                if (InStartup)
                    Angle = Vector2.Lerp(Angle, (player.MountedCenter - player.MouseWorld).SafeNormalize(Vector2.One), RotateInStartup);
                if (InCooldown)
                    Angle = Vector2.Lerp(Angle, (player.MountedCenter - player.MouseWorld).SafeNormalize(Vector2.One), RotateInCooldown);
                if (Angle.X < 0)
                {
                    player.direction = 1;
                    Projectile.spriteDirection = 1 * (int)player.gravDir;
                }
                else
                {
                    player.direction = -1;
                    Projectile.spriteDirection = -1 * (int)player.gravDir;
                }
                if (AlternateSwings && player.GetModPlayer<RootsCorePlayer>().swingCounter % 2 == 1)
                {
                    Projectile.spriteDirection *= -1;
                }
            }
            if (Projectile.spriteDirection == -1)
            {
                adust = MathHelper.ToRadians(-45);
            }
            Vector2 armCenter = player.MountedCenter - new Vector2(5 * player.direction, 2);
            if (AfterImageLength > 0)
            {
                oldProjectileRot.Add(Projectile.rotation);
                oldProjectilePos.Add(Projectile.Center + new Vector2(0, Projectile.gfxOffY));
                if (oldProjectileRot.Count > AfterImageLength)
                {
                    oldProjectileRot.RemoveAt(0);
                    oldProjectilePos.RemoveAt(0);
                }
            }
            if (InSwing && SwingTimer == 1 && UseSound != null)
            {
                SoundEngine.PlaySound((SoundStyle)UseSound, player.Center);
            }
            var angle2 = (AlternateSwings && modplayer.swingCounter % 2 == 1 ? SwingFunction() : SwingFunction());
            Projectile.Center = armCenter - (Angle * OffsetDistance * (1 + (Projectile.scale - 1) * 0.75f)).RotatedBy(Projectile.spriteDirection * angle2);
            Projectile.rotation = Angle.RotatedBy(Projectile.spriteDirection * angle2).ToRotation() + adust;
            AdditionalAI();
            if (!Projectile.active)
                return;
            OldPlayerOffset = Projectile.Center - player.MountedCenter;
            player.itemTime = ExistsTime + 2 - TotalTimer;
            player.itemAnimation = ExistsTime + 2 - TotalTimer;
            if (TotalTimer > ExistsTime)
            {
                player.itemTime = 0;
                player.itemAnimation = 0;
                Projectile.Kill();
            }
            TotalTimer++;
            if (TotalTimer >= StartupTime && TotalTimer < StartupTime + SwingTime)
            {
                SwingTimer++;
            }
            Vector2 armDir = armCenter - Projectile.Center;
            armDir.Y *= player.gravDir;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armDir.ToRotation() + MathHelper.ToRadians(90));
            oldScale.Insert(0, Projectile.scale);
        }
        public override bool PreDraw(ref Color lightColor)
        {

            Player player = Main.player[Projectile.owner];
            if (AfterImageLength > 0)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value ;
                for (int i = 0; i < oldProjectileRot.Count; i++)
                {
                    var col = Projectile.Opacity * (i / (float)AfterImageLength) * 0.1f;
                    if (Projectile.spriteDirection == 1)
                    {
                        Main.EntitySpriteDraw(texture, oldProjectilePos[i] - Main.screenPosition, null, AfterImageColor * col, oldProjectileRot[i], texture.Size() / 2, oldScale[i], SpriteEffects.None, 0);
                    }
                    else
                    {
                        Main.EntitySpriteDraw(texture, oldProjectilePos[i] - Main.screenPosition, null, AfterImageColor * col, oldProjectileRot[i], texture.Size() / 2, oldScale[i], SpriteEffects.FlipHorizontally, 0);
                    }
                }
            }
            Main.player[Projectile.owner].heldProj = Projectile.whoAmI;
            return true;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            var center = hitbox.Center.ToVector2();
            hitbox.Height = (int)(Projectile.height * Projectile.scale);
            hitbox.Width = (int)(Projectile.width * Projectile.scale);
            hitbox.Location = (center - new Vector2(hitbox.Width / 2, hitbox.Height / 2)).ToPoint();

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (LineCollisionLength > 0)
            {
                Player player = Main.player[Projectile.owner];
                Vector2 armcenter = player.MountedCenter - new Vector2(5 * player.direction, 2);
                Vector2 swordDir = armcenter.DirectionTo(Projectile.Center);
                Vector2 collisionline = new Vector2(LineCollisionLength / 2f, 0).RotatedBy(swordDir.ToRotation()) * Projectile.scale;
                bool c = Collision.CheckAABBvLineCollision(targetHitbox.Location.ToVector2(), targetHitbox.Size(), Projectile.Center, Projectile.Center + collisionline);
                if (c && !float.IsNaN(collisionline.X) && !float.IsNaN(collisionline.Y))
                    return true;
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = ((Main.player[Projectile.owner].DirectionTo(target.Center)).X >= 0 ? 1 : -1);
        }

        public override bool? CanDamage()
        {
            return InSwing;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(Angle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Angle = reader.ReadVector2();
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (target.noTileCollide)
                return null;
            if (!Collision.CanHit(Main.player[Projectile.owner], target))
                return false;

            return null;
        }
        #endregion
    }

}
