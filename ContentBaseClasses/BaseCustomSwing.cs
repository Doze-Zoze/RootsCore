using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RootsCore.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public override bool MeleePrefix() => SizeModifiers;

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

    public abstract class BaseCustomSwingProjectile<T> : ModProjectile where T : BaseCustomSwingProjectile<T>
    {
        /// <summary>
        /// Individual state the weapon can be in (i.e. startup, swing, cooldown)
        /// By default, instances of this class should be included in StateList for them to be used by the weapon
        /// When defining instances of this class, override parameters in the object initializer
        /// </summary>
        public class AttackState
        {
            /// <summary>
            /// The time that this state takes to finish.
            /// </summary>
            public required int Time { get; init; }
            /// <summary>
            /// The amount of after images that follow the weapon during this state.
            /// </summary>
            public int AfterImageCount { get; init; }
            /// <summary>
            /// The speed at which the weapon's angle moves towards the mouse.
            /// Set to 0 to disable this behaviour.
            /// </summary>
            public float RotationSpeed { get; init; }
            /// <summary>
            /// The sound that plays at the start of the state.
            /// Set to null to disable this behaviour.
            /// </summary>
            public SoundStyle? Sound { get; init; } = null;

            /// <summary>
            /// The base angle of the weapon during this state.
            /// </summary>
            public Vector2? Angle { get; init; } = null;

            /// <summary>
            /// The width, in radians, of the sword swing.
            /// </summary>
            public float SwingWidth { get; init; } = MathHelper.Pi;
            /// <summary>
            /// Distance in pixels of the projectile to the player
            /// </summary>
            public int OffsetDistance { get; init; }

            /// <summary>
            /// Whether this state's timer is affected by attack speed.
            /// </summary>
            public bool AffectedBySpeed { get; init; } = true;

            /// <summary>
            /// Whether this state deals damage
            /// </summary>
            public bool CanDamage { get; init; } = true;
            /// <summary>
            /// Whether the angle in this state alternates with every use of the weapon.
            /// </summary>
            public bool AlternateSwings { get; init; }
            /// <summary>
            /// Whether this state loops at the end of its timer.
            /// The state will not advance without manual intervention if this is true.
            /// </summary>
            public bool Loops { get; init; }

            /// <summary>
            /// Whether this state is reached when the previous state ends.
            /// This state will be skipped if it is attempted to be reach by any function (besides manually setting state.)
            /// This state will be ignored if it is at the end of the statelist, and the weapon will end before it is reached.
            /// </summary>
            public bool IsTransitionedTo { get; init; } = true;

            /// <summary>
            /// Function that decides the current offset angle of the swing.
            /// </summary>
            /// <returns>Angle in radians</returns>
            public Func<T, float> SwingOffsetAngle { get; init; } = null;

            /// <summary>
            /// Function that runs during the state's timer that can override any AI behaviour
            /// Runs after the base AdditionalAI()
            /// </summary>
            public Action<T> AdditionalAI { get; init; } = (_) => {};

            /// <summary>
            /// Function that runs once when a state starts
            /// Looped states will still only run this once
            /// </summary>
            public Action<T> Startup { get; init; } = (_) => {};
        }
        
        #region Overrideable Fields

        /// <summary>
        /// How far the held sword should be offset from the player
        /// Defaults to 0
        /// </summary>
        public virtual int OffsetDistance { get; set; }
        /// <summary>
        /// What item this projectile uses as a base.
        /// </summary>
        public virtual Item BaseItem { get; set; }
        /// <summary>
        /// Whether this projectile uses a base item at all.
        /// Defaults to true.
        /// </summary>
        public virtual bool UsesBaseItem { get; set; } = true;

        /// <summary>
        /// Length of after-image trail left by the projectile.
        /// Defaults to 0
        /// </summary>
        public virtual int AfterImageCount { get; set; }
        
        /// <summary>
        /// Whether this should get melee size bonuses (Titan Glove)
        /// Defaults to TRUE
        /// </summary>
        public virtual bool UseMeleeSize { get; set; } = true;

        /// <summary>
        /// The length (from the player) of the projectile's line collision.
        /// This helps to prevent blind spots.
        /// Defaults to 0
        /// </summary>
        public virtual float LineCollisionLength { get; set; }

        public virtual Color AfterImageColor { get; set; } = Color.White;

        #endregion

        #region Fields

        /// <summary>
        /// The width, in degrees, of the sword swing.
        /// Defaults to 180
        /// </summary>
        public float SwingWidth => State.SwingWidth;
        /// <summary>
        /// If the swing should alternate directions each use.
        /// Defaults to true
        /// </summary>
        public bool AlternateSwings => State.AlternateSwings;
        /// <summary>
        /// Whether this should get attack speed bonuses for its class
        /// Defaults to TRUE
        /// </summary>
        public bool UseAttackSpeed => State.AffectedBySpeed;
        /// <summary>
        /// Speed at which the projectile should rotate to match the mouse angle during StartupTime.
        /// Set to 0 to disable.
        /// </summary>
        public float RotationSpeed => State.RotationSpeed;
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
        /// Timer since the projectile was created
        /// </summary>
        public int Timer { get; set; }
        public float BaseScale;
        /// <summary>
        /// Old weapon scales used to track for trail drawing
        /// </summary>
        public List<float> OldScale = [];
        public List<float> OldProjectileRot = [];
        public List<Vector2> OldProjectilePos = [];
        private bool _hasFakedOnSpawn;

        /// <summary>
        /// The current State of the weapon
        /// </summary>
        public AttackState State { get; set; }
        /// <summary>
        /// The total time the current state will take to finish
        /// Gets multiplied by Projectile.MaxUpdates (and attack speed if relevant) automatically
        /// </summary>
        public int StateMaxTime { get; set; }
        /// <summary>
        /// The current timer (0 -> State.Time) of the current State
        /// </summary>
        public int StateTimer { get; set; }

        /// <summary>
        /// The ratio of the State's Timer to its total time
        /// </summary>
        public float StateCompletion => StateTimer / (float)(StateMaxTime - 1);
        /// <summary>
        /// The list of all states this weapon contains
        /// By default, when one state completes (i.e. StateTimer hits StateMaxTime),
        /// it moves to the next AttackState in the list
        /// </summary>
        public abstract List<AttackState> StateList { get; }
        
        #endregion
        
        #region Helpers
        public Player Player => Main.player[Projectile.owner];
        public RootsCorePlayer ModPlayer => Player.GetModPlayer<RootsCorePlayer>();
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
        #endregion

        #region Overrides
        /// <summary>
        /// DO NOT OVERRIDE IN MOST SITUATIONS
        /// use Defaults() instead.
        /// That will set defaults after everything is set in the base projectile.
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.timeLeft = 5;
            if (UsesBaseItem)
            {
                Projectile.width = Projectile.height = Math.Max(BaseItem.height, BaseItem.width);
                Projectile.DamageType = BaseItem.DamageType;
            }
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.localNPCHitCooldown = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.extraUpdates = 0;
            Projectile.aiStyle = -2;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.MaxUpdates = 5;
            Defaults();
        }
        public override void SetStaticDefaults()
        {

            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 100;
        }
        /// <summary>
        /// This hook runs at the beginning of AI the first time through.
        /// Do not override unless you know what you're doing
        /// </summary>
        public virtual void FakeOnSpawn()
        {
            Angle = (Player.MountedCenter - Player.MouseWorld).SafeNormalize(Vector2.One);
            SetState(StateList.First());
            
            if (AlternateSwings)
                ModPlayer.SwingCounter++;
            
            Spawn();
            
            if (UseMeleeSize)
            {
                Projectile.scale *= Player.GetMeleeScale();
            }
            BaseScale = Projectile.scale;
            Projectile.timeLeft = 5;
        }

        /// <summary>
        /// DO NOT OVERRIDE IN MOST SITUATIONS
        /// use AdditionalAI() instead
        /// That will run AI code at the right time.
        /// </summary>
        public override void AI()
        {
            if (!_hasFakedOnSpawn)
            {
                FakeOnSpawn();
                _hasFakedOnSpawn = true;
            }
            Projectile.gfxOffY = Player.gfxOffY;
            float adjust = MathHelper.ToRadians(225);
            
            if (RotationSpeed != 0)
                Angle = Vector2.Lerp(Angle, (Player.MountedCenter - Player.MouseWorld).SafeNormalize(Vector2.One), RotationSpeed).SafeNormalize(Vector2.One);
            if (Angle.X < 0)
            {
                Player.direction = 1;
                Projectile.spriteDirection = 1 * (int)Player.gravDir;
            }
            else
            {
                Player.direction = -1;
                Projectile.spriteDirection = -1 * (int)Player.gravDir;
            }
            
            if (AlternateSwings && ModPlayer.SwingCounter % 2 == 1)
            {
                Projectile.spriteDirection *= -1;
            }
            
            if (Projectile.spriteDirection == -1)
            {
                adjust = MathHelper.ToRadians(-45);
            }
            
            Vector2 armCenter = Player.MountedCenter - new Vector2(5 * Player.direction, 2);
            
            if (AfterImageCount > 0)
            {
                OldProjectileRot.Add(Projectile.rotation);
                OldProjectilePos.Add(Projectile.Center + new Vector2(0, Projectile.gfxOffY));
                if (OldProjectileRot.Count > AfterImageCount)
                {
                    OldProjectileRot.RemoveAt(0);
                    OldProjectilePos.RemoveAt(0);
                }
            }

            float swingAngle = State.SwingOffsetAngle?.Invoke((T)this) ??
                               MathHelper.SmoothStep(-SwingWidth / 2, SwingWidth / 2, StateCompletion);
            Projectile.Center = armCenter - (Angle * OffsetDistance * (1 + (Projectile.scale - 1) * 0.75f)).RotatedBy(Projectile.spriteDirection * swingAngle);
            Projectile.rotation = Angle.RotatedBy(Projectile.spriteDirection * swingAngle).ToRotation() + adjust;
            AdditionalAI();
            State.AdditionalAI((T)this);
            if (!Projectile.active)
                return;
            OldPlayerOffset = Projectile.Center - Player.MountedCenter;
            Player.itemTime++;
            Player.itemAnimation++;
            Projectile.timeLeft++;
            StateTimer++;
            Timer++;
            if (StateTimer >= StateMaxTime)
            {
                if (State.Loops)
                    SetState(State);
                else
                {
                    int stateIndex = StateList.FindIndex(state => state == State);
                    while (true)
                    {
                        if (stateIndex + 1 >= StateList.Count)
                        {
                            Player.itemTime = 0;
                            Player.itemAnimation = 0;
                            Projectile.Kill();
                            return;
                        }

                        if (!StateList[stateIndex + 1].IsTransitionedTo)
                        {
                            stateIndex++;
                            continue;
                        }

                        SetState(StateList[stateIndex + 1]);
                        break;
                    }
                }
            }
            Vector2 armDir = armCenter - Projectile.Center;
            armDir.Y *= Player.gravDir;
            Player.heldProj = Projectile.whoAmI;
            Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armDir.ToRotation() + MathHelper.ToRadians(90));
            OldScale.Insert(0, Projectile.scale);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            if (AfterImageCount > 0)
            {
                for (int i = 0; i < OldProjectileRot.Count; i++)
                {
                    var col = Projectile.Opacity * (i / (float)AfterImageCount) * 0.1f;
                    Main.EntitySpriteDraw(texture, OldProjectilePos[i] - Main.screenPosition, null,
                        AfterImageColor * col, OldProjectileRot[i], texture.Size() / 2, OldScale[i],
                        Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
                }
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, texture.Frame(),
                lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale,
                Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
            return false;
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            var center = hitbox.Center.ToVector2();
            hitbox.Height = (int)(Projectile.height * Projectile.scale);
            hitbox.Width = (int)(Projectile.width * Projectile.scale);
            hitbox.Location = (center - new Vector2(hitbox.Width * 0.5f, hitbox.Height * 0.5f)).ToPoint();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!(LineCollisionLength > 0)) return base.Colliding(projHitbox, targetHitbox);
            Vector2 armCenter = Player.MountedCenter - new Vector2(5 * Player.direction, 2);
            Vector2 swordDir = armCenter.DirectionTo(Projectile.Center);
            Vector2 collisionLine = new Vector2(LineCollisionLength / 2f, 0).RotatedBy(swordDir.ToRotation()) * Projectile.scale;
            bool collided = Collision.CheckAABBvLineCollision(targetHitbox.Location.ToVector2(), targetHitbox.Size(), Projectile.Center, Projectile.Center + collisionLine);
            if (collided && !float.IsNaN(collisionLine.X) && !float.IsNaN(collisionLine.Y))
                return true;
            return base.Colliding(projHitbox, targetHitbox);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Player.DirectionTo(target.Center).X >= 0 ? 1 : -1;
        }

        public override bool? CanDamage() => State.CanDamage;

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
            if (!Collision.CanHit(Player, target))
                return false;

            return null;
        }
        #endregion

        public void SetState(AttackState state)
        {
            bool shouldStart = state != State;
            State = state;
            StateMaxTime = State.Time;
            StateTimer = 0;
            Angle = State.Angle ?? Angle;

            AfterImageCount = State.AfterImageCount;
            
            Projectile.velocity = Vector2.Zero;
            
            if (Angle.X < 0)
            {
                Player.direction = 1;
                Projectile.spriteDirection = 1 * (int)Player.gravDir;
            }
            else
            {
                Player.direction = -1;
                Projectile.spriteDirection = -1 * (int)Player.gravDir;
            }
            if (AlternateSwings && ModPlayer.SwingCounter % 2 == 1)
                Projectile.spriteDirection *= -1;
                
            StateMaxTime *= Projectile.MaxUpdates;
            if (UseAttackSpeed)
            {
                var speed = Player.GetTotalAttackSpeed(Projectile.DamageType);
                if (speed > 3f)
                    speed = 3f;

                if (speed != 0f)
                    speed = 1f / speed;

                StateMaxTime = Math.Max((int)(StateMaxTime * speed), 1);
            }

            OffsetDistance = State.OffsetDistance;
            
            if (shouldStart)
            {
                State.Startup((T)this);
                if (State.Sound != null)
                    SoundEngine.PlaySound((SoundStyle)State.Sound, Player.Center);
            }

            Projectile.netUpdate = true;
        }
    }

}
