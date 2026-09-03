using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace RootsCore
{
    public class ArenaSystem : ModSystem
    {
        public static List<Box> ActiveBoxes { get; set; } = [];
        public class Box
        {
            public Func<bool> RemovalCondition = () => false;

            public Vector2 Position;

            public Vector4 BoxDimensions;

            public Vector4 NewDimensions;

            public Vector2 NewPosition;

            public float BorderThickness;

            public Color BorderColor = Color.White;
            public float DistanceUp => BoxDimensions.X;
            public float DistanceRight => BoxDimensions.Y;
            public float DistanceDown => BoxDimensions.Z;
            public float DistanceLeft => BoxDimensions.W;

            public Vector2 TopLeft => Position + new Vector2(-DistanceLeft, -DistanceUp);
            public Vector2 TopRight => Position + new Vector2(DistanceRight, -DistanceUp);
            public Vector2 BottomLeft => Position + new Vector2(-DistanceLeft, DistanceDown);
            public Vector2 BottomRight => Position + new Vector2(DistanceRight, DistanceDown);

            public Vector2 BorderTopLeft => Position + new Vector2(-DistanceLeft - BorderThickness, -DistanceUp - BorderThickness);
            public Vector2 BorderTopRight => Position + new Vector2(DistanceRight + BorderThickness, -DistanceUp - BorderThickness);
            public Vector2 BorderBottomLeft => Position + new Vector2(-DistanceLeft - BorderThickness, DistanceDown + BorderThickness);
            public Vector2 BorderBottomRight => Position + new Vector2(DistanceRight + BorderThickness, DistanceDown + BorderThickness);

            public Vector2 Center => (TopLeft + BottomRight) * 0.5f;

            public Vector2 Size => new (DistanceLeft + DistanceRight, DistanceUp + DistanceDown);

            public Vector2 SizeWithBorder => new (DistanceLeft + DistanceRight + BorderThickness * 2, DistanceUp + DistanceDown + BorderThickness * 2);

            public Vector4 Hitbox => new (TopLeft.X, TopLeft.Y, Size.X, Size.Y);
            public Rectangle HitboxRectangle => new ((int)TopLeft.X, (int)TopLeft.Y, (int)Size.X, (int)Size.Y);
            public Rectangle BorderHitboxRectangle => new ((int)BorderTopLeft.X, (int)BorderTopLeft.Y, (int)(Size.X + BorderThickness * 2), (int)(Size.Y + BorderThickness * 2));

            public Box OldData = null;

            public Action<Box> DrawBox = null;

            public Action<Box> UpdateBox = null;
            /// <summary>
            /// The despawn logic to kill this box. Only runs when RemovalCondition is true. Return true to delete the box from ActiveBoxes.
            /// </summary>
            public Func<Box, bool> DespawnAction = (box) => true;

            /// <summary>
            /// When resizing, should this move the player too?
            /// May have unintentional effects in 
            /// </summary>
            public bool PullPlayerWhenSizeChanged { get; set; } = false;
            public void DrawBoxWithOffset(float offset, float thickness, Color color)
            {
                DrawLineActuallyAccurate(TopLeft + new Vector2(-(offset + thickness * 0.5f), -offset), TopRight + new Vector2((offset + thickness * 0.5f), -offset));
                DrawLineActuallyAccurate(BottomLeft + new Vector2(-(offset + thickness * 0.5f), offset), BottomRight + new Vector2((offset + thickness * 0.5f), offset));
                DrawLineActuallyAccurate(TopLeft + new Vector2(-offset, -(offset - thickness * 0.5f)), BottomLeft + new Vector2(-offset, (offset - thickness * 0.5f)));
                DrawLineActuallyAccurate(BottomRight + new Vector2(offset, (offset - thickness * 0.5f)), TopRight + new Vector2(offset, -(offset - thickness * 0.5f)));
                return;

                void DrawLineActuallyAccurate(Vector2 start, Vector2 end)
                {
                    if (start == end)
                        return;

                    start -= Main.screenPosition;
                    end -= Main.screenPosition;
                    
                    float rotation = (end - start).ToRotation();
                    Vector2 scale = new Vector2(Vector2.Distance(start, end), thickness);

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, start, null, color, rotation, TextureAssets.MagicPixel.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }

            public void SetOldData()
            {
                if (OldData == null)
                    OldData = new Box { BorderColor = BorderColor, BorderThickness = BorderThickness, BoxDimensions = BoxDimensions, Position = Position };
                else
                {
                    OldData.BorderThickness = BorderThickness;
                    OldData.BoxDimensions = BoxDimensions;
                    OldData.Position = Position;
                }
            }
            public bool Contains(Vector2 position, Vector2 size)
            {
                //0-height or 0-width boxes can never work as containers, so we always try to exclude the player so they work as solid blocks
                if (Size.X <= 0 || Size.Y <= 0)
                    return false;
                return Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(BorderThickness * 0.5f), Size + new Vector2(BorderThickness), position, size);
            }

            public bool ContainsIncludingBorder(Vector2 position, Vector2 size)
            {
                return Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(BorderThickness), Size + new Vector2(BorderThickness * 2), position, size);
            }

            public bool ShouldAffectPlayer(Player player) => Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(BorderThickness + 16), Size + new Vector2(BorderThickness + 16) * 2, player.TopLeft, player.Size);
            public bool InsideInnerHalfOfBorder(Vector2 position, Vector2 size) => Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(BorderThickness) * 0.5f, Size + new Vector2(BorderThickness), position, size);

            public bool PointInBorder(Vector2 pos)
            {
                return !Vector2PointCollision(TopLeft, Size, pos) && Vector2PointCollision(TopLeft - new Vector2(BorderThickness), Size + new Vector2(BorderThickness) * 2, pos);
            }

            public bool CollidesWithBorder(Vector2 pos, Vector2 size)
            {
                return Collision.CheckAABBvAABBCollision(BorderTopLeft, BorderTopRight + new Vector2(0, BorderThickness) - BorderTopLeft, pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderTopLeft, BorderBottomLeft + new Vector2(BorderThickness, 0) - BorderTopLeft, pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderTopRight - new Vector2(BorderThickness, 0), BorderBottomRight - BorderTopRight + new Vector2(BorderThickness, 0), pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderBottomLeft - new Vector2(0, BorderThickness), BorderBottomRight - BorderBottomLeft + new Vector2(0, BorderThickness), pos, size);
            }
            
            private static bool Vector2PointCollision(Vector2 position, Vector2 size, Vector2 point)
            {
                return (point.X >= position.X && point.X <= position.X + size.X && point.Y >= position.Y && point.Y <= position.Y + size.Y);
            }
        }
        public override void PostDrawTiles()
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.Transform
            );
            foreach (var box in ActiveBoxes)
            {
                if (box.DrawBox is not null)
                    box.DrawBox(box);
                else
                {
                    //Inside Fill
                    box.DrawBoxWithOffset(box.BorderThickness * 0.5f, box.BorderThickness, Color.Black * 0.75f);
                    //Inner Border, if box can actually contain things
                    if (box.Size.X > 0 || box.Size.Y > 0)
                        box.DrawBoxWithOffset(4, 8, box.BorderColor);
                    //Outer Border
                    box.DrawBoxWithOffset(box.BorderThickness - 4, 8, box.BorderColor);
                }
            }
            Main.spriteBatch.End();
        }

        public override void PreUpdateEntities()
        {
            for (var i = 0; i < ActiveBoxes.Count; i++)
            {
                var box = ActiveBoxes[i];
                if (box.RemovalCondition())
                {
                    if (box.DespawnAction(box))
                    {
                        ActiveBoxes.Remove(box);
                        i--;
                    }

                    continue;
                }

                box.SetOldData();
                if (box.NewDimensions != Vector4.Zero)
                {
                    box.BoxDimensions = box.NewDimensions;
                }
                if (box.NewPosition != Vector2.Zero)
                {
                    box.Position = box.NewPosition;
                }
                box.UpdateBox?.Invoke(box);
            }
        }

        public override void OnWorldUnload()
        {
            ActiveBoxes = [];
        }
    }

    public class ArenaPlayer : ModPlayer
    {
        public override void PreUpdateMovement()
        {
            foreach (var box in ArenaSystem.ActiveBoxes.Where(box => box.ShouldAffectPlayer(Player)))
            {
                if (box.Contains(Player.position, Player.Size))
                    ContainPlayerLogic(box);
                else
                    ExcludePlayerLogic(box);
            }
            if (!Main.gameMenu && ArenaSystem.ActiveBoxes.Count < 1) 
            {
                
                ArenaSystem.ActiveBoxes.Add(new ArenaSystem.Box
                {
                    Position = Main.LocalPlayer.Center - new Vector2(0, 500),
                    BoxDimensions = new Vector4(100, 100, 100, 100),
                    UpdateBox = (box) => {
                        box.BoxDimensions = new Vector4(100, 500, 100, 100);
                        box.BorderThickness = 0;
                    }
                });
            }
        }

        private void ExcludePlayerLogic(ArenaSystem.Box box)
        {
            if (box.OldData is not null && box.PullPlayerWhenSizeChanged)
            {
                if (box.OldData.CollidesWithBorder(Player.position - Vector2.One, Player.Size + Vector2.One * 2))
                {
                    Vector2 oldRatio = new(Utils.Remap(Player.Center.X, box.OldData.BorderTopLeft.X, box.BorderBottomRight.X, 0, 1, true), Utils.Remap(Player.Center.Y, box.OldData.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                    Vector2 newPos = box.BorderTopLeft + (box.SizeWithBorder) * oldRatio;
                    Vector2 oldPos = box.OldData.BorderTopLeft + box.OldData.SizeWithBorder * oldRatio;
                    Player.position += newPos - oldPos;
                }
            }

            Vector2 playerPos = Player.oldPosition + Player.Size * 0.5f;

            #region Snapping

            //Max distance possible while still touching - actual distance from player center and box center
            Vector2 howMuchInThere = new (
                (Player.width * 0.5f + box.SizeWithBorder.X * 0.5f) - Math.Abs(Player.Center.X - box.Center.X),
                (Player.height * 0.5f + box.SizeWithBorder.Y * 0.5f) - Math.Abs(Player.Center.Y - box.Center.Y));

            if (howMuchInThere is { X: > 0, Y: > 0 })
            {
                //Only push once, in the axis that is more embedded than the other
                if (howMuchInThere.X < howMuchInThere.Y)
                {
                    Player.position.X = (playerPos.X < box.Center.X)
                        ? box.BorderTopLeft.X - Player.width
                        : box.BorderBottomRight.X;
                }
                else
                {
                    Player.position.Y = (playerPos.Y < box.Center.Y)
                        ? box.BorderTopLeft.Y - Player.height
                        : box.BorderBottomRight.Y;
                }
            }

            #endregion
            #region Velocity
            Vector2 originalVelocity = Player.velocity;
            Vector2 originalCenter = Player.Center;
            howMuchInThere.X =
                (Player.width * 0.5f + box.SizeWithBorder.X * 0.5f) - Math.Abs(Player.Center.X - box.Center.X);
            howMuchInThere.Y =
                (Player.height * 0.5f + box.SizeWithBorder.Y * 0.5f) - Math.Abs(Player.Center.Y - box.Center.Y);
            Player.position += originalVelocity;

            bool headBonk = false;

            if (box.ContainsIncludingBorder(Player.position, Player.Size))
            {
                //Only push once, in the axis that is more embedded than the other
                if (howMuchInThere.X < howMuchInThere.Y)
                {
                    if (playerPos.X < box.Center.X)
                    {
                        Player.position.X = box.BorderTopLeft.X - Player.width;
                        if (Player.controlRight && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                        {
                            Player.slideDir = 1;
                        }
                    }
                    else
                    {
                        Player.position.X = box.BorderBottomRight.X;
                        if (Player.controlLeft && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                        {
                            Player.slideDir = -1;
                        }
                    }
                }
                else
                {
                    if (playerPos.Y < box.Center.Y)
                    {
                        Player.position.Y = box.BorderTopLeft.Y - Player.height;
                    }
                    else
                    {
                        Player.position.Y = box.BorderBottomRight.Y;
                        headBonk = true;
                    }
                }
            }

            Player.velocity = Player.Center - originalCenter;
            
            if (headBonk) 
                Player.velocity.Y -= 0.00001f;
            if (Player.slideDir != 0)
                ApplyShoeSpikes();

            Player.Center = originalCenter;
            #endregion
        }

        private void ContainPlayerLogic(ArenaSystem.Box box)
        {
            if (box.OldData is not null && box.PullPlayerWhenSizeChanged)
            {

                if (box.OldData.CollidesWithBorder(Player.position - Vector2.One, Player.Size + Vector2.One * 2))
                {
                    Vector2 oldRatio = new(Utils.Remap(Player.Center.X, box.OldData.TopLeft.X, box.BottomRight.X, 0, 1, true), Utils.Remap(Player.Center.Y, box.OldData.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                    Vector2 newPos = box.TopLeft + box.Size * oldRatio;
                    Vector2 oldPos = box.OldData.TopLeft + box.OldData.Size * oldRatio;
                    Player.position += newPos - oldPos;
                }
            }

            #region Snapping

            Player.Center = Vector2.Clamp(Player.Center, box.TopLeft + Player.Size * 0.5f, box.BottomRight - Player.Size * 0.5f);

            #endregion
            #region Velocity

            Vector2 playerPos = Player.oldPosition + Player.Size * 0.5f;

            Vector2 originalVelocity = Player.velocity;
            Vector2 originalCenter = Player.Center;
            Player.position += originalVelocity;
            Vector2 howMuchInThere = new((Player.width * 0.5f + box.Size.X * 0.5f) - Math.Abs(Player.Center.X - box.Center.X), 
                    (Player.height * 0.5f + box.Size.Y * 0.5f) - Math.Abs(Player.Center.Y - box.Center.Y));

            bool headBonk = false;

            if (box.CollidesWithBorder(Player.position, Player.Size))
            {
                //Only push once, in the axis that is more embedded than the other
                if (howMuchInThere.X < Player.width)
                {
                    if (playerPos.X < box.Center.X)
                    {
                        Player.position.X = box.TopLeft.X;
                        if (Player.controlRight && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                        {
                            Player.slideDir = 1;
                        }
                    }
                    else
                    {
                        Player.position.X = box.BottomRight.X - Player.width;
                        if (Player.controlLeft && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                        {
                            Player.slideDir = -1;
                        }
                    }
                }
                if (howMuchInThere.Y < Player.height)
                {
                    if (playerPos.Y < box.Center.Y)
                    {
                        Player.position.Y = box.TopLeft.Y;
                        headBonk = true;
                    }
                    else
                    {
                        Player.position.Y = box.BottomRight.Y - Player.height;
                    }
                }
            }

            Player.velocity = Player.Center - originalCenter;

            if (headBonk) 
                Player.velocity.Y -= 0.00001f;
            if (Player.slideDir != 0)
                ApplyShoeSpikes();

            Player.Center = originalCenter;
            #endregion
        }

        private void SpawnShoeDust()
        {

            int shoeDust = Dust.NewDust(new Vector2(Player.position.X + (Player.width * 0.5f) + ((Player.width * 0.5f - 4) * Player.slideDir), Player.position.Y + (Player.height * 0.5f) + (Player.height * 0.5f - 4) * Player.gravDir), 8, 8, DustID.Smoke);
            if (Player.slideDir < 0)
            {
                Main.dust[shoeDust].position.X -= 10f;
            }
            if (Player.gravDir < 0f)
            {
                Main.dust[shoeDust].position.Y -= 12f;
            }
            Main.dust[shoeDust].velocity *= 0.1f;
            Main.dust[shoeDust].scale *= 1.2f;
            Main.dust[shoeDust].noGravity = true;
            Main.dust[shoeDust].shader = GameShaders.Armor.GetSecondaryShader(Player.cShoe, Player);
        }

        private void ApplyShoeSpikes()
        {
            if (Player.spikedBoots == 0 || Player.velocity.Y < 0 || Player.mount.Active)
                return;
            if (Player.controlDown && Player.spikedBoots > 0)
            {
                Player.velocity.Y = 4f * Player.gravDir;
                SpawnShoeDust();
            }
            else if (Player.spikedBoots >= 2)
            {
                Player.velocity.Y = 0;
            }
            else if (Player.spikedBoots == 1)
            {

                Player.velocity.Y = 0.5f * Player.gravDir;
                SpawnShoeDust();
            }
        }
    }

    public class ArenaWallProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        /// <summary>
        /// The location of this projectile in an arena box, used for maintaining this when the arena box moves.
        /// </summary>
        public Vector2 ArenaBoxPosition = Vector2.Zero;
        /// <summary>
        /// Which Box this projectile is attached to
        /// </summary>
        public ArenaSystem.Box ArenaBox;

    }

    public partial class RootsCore : Mod
    {

        #region Allow Grappling Hooks to grab arena walls
        public static void AllowHooksToGrabArenaBox(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
        {
            if (Main.player[self.owner].dead || Main.player[self.owner].stoned || Main.player[self.owner].webbed || Main.player[self.owner].frozen)
            {
                self.Kill();
                return;
            }

            bool intersectingWall = false;
            var modProj = self.GetGlobalProjectile<ArenaWallProj>();
            if (modProj.ArenaBox is not null)
            {
                ArenaSystem.Box box = modProj.ArenaBox;
                if (ArenaSystem.ActiveBoxes.Contains(box))
                {
                    self.Center = box.TopLeft + box.Size * modProj.ArenaBoxPosition;
                }
                else
                {
                    modProj.ArenaBox = null;
                }
            }
            if (ArenaSystem.ActiveBoxes.Count > 0)
            {
                foreach (var box in ArenaSystem.ActiveBoxes.Where(box =>
                             !Vector2PointCollision(box.TopLeft, box.Size, self.Center) &&
                             Vector2PointCollision(box.TopLeft - new Vector2(box.BorderThickness),
                                 box.Size + new Vector2(box.BorderThickness) * 2, self.Center)))
                {
                    if (self.ai[0] == 0)
                    {
                        if (Main.myPlayer == self.owner)
                        {
                            if (self.type == ProjectileID.QueenSlimeHook)
                            {
                                Main.player[self.owner].DoQueenSlimeHookTeleport(self.Center);
                            }
                            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, self.owner);
                        }
                        SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with { Volume = 0.75f }, self.Center);
                        self.ai[0] = 2;
                        self.velocity = Vector2.Zero;
                        modProj.ArenaBoxPosition = new Vector2(Utils.Remap(self.Center.X, box.TopLeft.X, box.BottomRight.X, 0, 1, false), Utils.Remap(self.Center.Y, box.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                        modProj.ArenaBox = box;
                    }

                    if (self.ai[0] != 2) continue; //don't run this if the hook is returning!
                    self.rotation = self.DirectionFrom(Main.player[self.owner].Center).ToRotation() + MathHelper.PiOver2;
                    intersectingWall = true;

                    if (Main.player[self.owner].grapCount >= 10) continue;
                    Main.player[self.owner].grappling[Main.player[self.owner].grapCount] = self.whoAmI;
                    Main.player[self.owner].grapCount++;
                }
            }
            if (!intersectingWall)
                orig(self);
            return;

            static bool Vector2PointCollision(Vector2 position, Vector2 size, Vector2 point)
            {
                return (point.X >= position.X && point.X <= position.X + size.X && point.Y >= position.Y && point.Y <= position.Y + size.Y);
            }
        }
        #endregion

        #region Arena Collision for other things

        private static bool ArenaCollision_Vector2_int_int_bool(On_Collision.orig_SolidCollision_Vector2_int_int_bool orig, Vector2 position, int width, int height, bool acceptTopSurfaces)
        {
            if (ArenaSystem.ActiveBoxes.Count <= 0) return orig(position, width, height, acceptTopSurfaces);
            return ArenaSystem.ActiveBoxes.Any(item => item.CollidesWithBorder(position, new Vector2(width, height))) ||
                   orig(position, width, height, acceptTopSurfaces);
        }

        private static bool ArenaCollision_Vector2_int_int(On_Collision.orig_SolidCollision_Vector2_int_int orig, Vector2 position, int width, int height)
        {
            if (ArenaSystem.ActiveBoxes.Count <= 0) return orig(position, width, height);
            return ArenaSystem.ActiveBoxes.Any(item => item.CollidesWithBorder(position, new Vector2(width, height))) ||
                   orig(position, width, height);
        }


        private static Vector2 ArenaCollision_TileCollision(On_Collision.orig_TileCollision orig, Vector2 position, Vector2 velocity, int width, int height, bool fallThrough, bool fall2, int gravDir)
        {
            velocity = orig(position, velocity, width, height, fallThrough, fall2, gravDir);
            if (ArenaSystem.ActiveBoxes.Count <= 0 || velocity == Vector2.Zero) return velocity;
            foreach (var item in ArenaSystem.ActiveBoxes.Where(item =>
                         item.ContainsIncludingBorder(position + velocity, new Vector2(width, height))))
            {
                velocity = item.InsideInnerHalfOfBorder(position, new Vector2(width, height))
                    ? ContainLogic(item, position, width, height, velocity)
                    : ExcludeLogic(item, position, width, height, velocity);
            }
            return velocity;
        }

        private static Vector2 ContainLogic(ArenaSystem.Box box, Vector2 position, int width, int height, Vector2 velocity)
        {
            Vector2 originalVelocity = velocity;
            position += originalVelocity;
            
            Vector2 howMuchInThere = new (
                (width * 0.5f + box.Size.X * 0.5f) - Math.Abs(position.X + (width * 0.5f) - box.Center.X),
                (height * 0.5f + box.Size.Y * 0.5f) - Math.Abs(position.Y + (height * 0.5f) - box.Center.Y));

            bool headBonk = false;
            
            if (box.CollidesWithBorder(position, new Vector2(width, height)))
            {
                //Only push once, in the axis that is more embedded than the other
                if (howMuchInThere.X <= width)
                {
                    if (position.X > box.Center.X)
                    {
                        velocity.X = originalVelocity.X < 0 ? velocity.X : 0;
                    }
                    else
                    {
                        velocity.X = originalVelocity.X > 0 ? velocity.X : 0;
                    }
                }
                
                if (howMuchInThere.Y <= height)
                {
                    if (position.Y > box.Center.Y)
                    {
                        velocity.Y = originalVelocity.Y < 0 ? originalVelocity.Y : 0;
                    }
                    else
                    {
                        velocity.Y = originalVelocity.Y > 0 ? originalVelocity.Y : 0;
                        headBonk = true;
                    }
                }
            }
            
            
            if (headBonk) 
                velocity.Y -= 0.00001f;
            return velocity;
        }
        private static Vector2 ExcludeLogic(ArenaSystem.Box box, Vector2 Position, int Width, int Height, Vector2 Velocity)
        {
            Vector2 originalVelocity = Velocity;
            Vector2 originalCenter = Position + new Vector2(Width, Height) * 0.5f;
            Vector2 howMuchInThere = new (
                (Width * 0.5f + box.SizeWithBorder.X * 0.5f) - Math.Abs(originalCenter.X - box.Center.X),
                (Height * 0.5f + box.SizeWithBorder.Y * 0.5f) - Math.Abs(originalCenter.Y - box.Center.Y));
            
            Position += originalVelocity;

            bool headBonk = false;
            
            if (box.ContainsIncludingBorder(Position, new Vector2(Width, Height)))
            {
                //Only push once, in the axis that is more embedded than the other
                if (howMuchInThere.X < howMuchInThere.Y)
                {
                    if (Position.X < box.Center.X)
                    {
                        Velocity.X = originalVelocity.X < 0 ? Velocity.X : 0;
                    }
                    else
                    {
                        Velocity.X = originalVelocity.X > 0 ? Velocity.X : 0;
                    }
                }
                else
                {
                    if (Position.Y < box.Center.Y)
                    {
                        Velocity.Y = originalVelocity.Y < 0 ? originalVelocity.Y : 0;
                    }
                    else
                    {
                        Velocity.Y = originalVelocity.Y > 0 ? originalVelocity.Y : 0;
                        headBonk = true;
                    }
                }
            }

            if (headBonk) 
                Velocity.Y -= 0.00001f;
            return Velocity;
        }
        #endregion

    }
}