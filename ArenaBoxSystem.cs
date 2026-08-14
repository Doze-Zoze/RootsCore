using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static RootsCore.ArenaSystem;

namespace RootsCore
{

    public class ArenaSystem : ModSystem
    {
        public static List<Box> ActiveBoxes = [];
        public class Box
        {

            public Func<bool> RemovalCondition = () => false;

            public Vector2 position;

            public Vector4 boxDimensions;

            public Vector4 NewDimensions;

            public Vector2 NewPosition;

            public float borderThickness;

            public Color borderColor = Color.White;
            public float DistanceUp => boxDimensions.X;
            public float DistanceRight => boxDimensions.Y;
            public float DistanceDown => boxDimensions.Z;
            public float DistanceLeft => boxDimensions.W;

            public Vector2 TopLeft => position + new Vector2(-DistanceLeft, -DistanceUp);
            public Vector2 TopRight => position + new Vector2(DistanceRight, -DistanceUp);
            public Vector2 BottomLeft => position + new Vector2(-DistanceLeft, DistanceDown);
            public Vector2 BottomRight => position + new Vector2(DistanceRight, DistanceDown);

            public Vector2 BorderTopLeft => position + new Vector2(-DistanceLeft - borderThickness, -DistanceUp - borderThickness);
            public Vector2 BorderTopRight => position + new Vector2(DistanceRight + borderThickness, -DistanceUp - borderThickness);
            public Vector2 BorderBottomLeft => position + new Vector2(-DistanceLeft - borderThickness, DistanceDown + borderThickness);
            public Vector2 BorderBottomRight => position + new Vector2(DistanceRight + borderThickness, DistanceDown + borderThickness);

            public Vector2 Center => (TopLeft + BottomRight) * 0.5f;

            public Vector2 Size => new Vector2(DistanceLeft + DistanceRight, DistanceUp + DistanceDown);

            public Vector2 SizeWithBorder => new Vector2(DistanceLeft + DistanceRight + borderThickness * 2, DistanceUp + DistanceDown + borderThickness * 2);

            public Vector4 Hitbox => new Vector4(TopLeft.X, TopLeft.Y, Size.X, Size.Y);
            public Rectangle HitboxRectangle => new Rectangle((int)TopLeft.X, (int)TopLeft.Y, (int)Size.X, (int)Size.Y);
            public Rectangle BorderHitboxRectangle => new Rectangle((int)BorderTopLeft.X, (int)BorderTopLeft.Y, (int)(Size.X + borderThickness * 2), (int)(Size.Y + borderThickness * 2));

            public Box oldData = null;

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
            public void DrawBoxWithOffset(float Offset, float Thickness, Color color)
            {
                void DrawLineActuallyAccurate(Vector2 start, Vector2 end)
                {
                    if (start == end)
                        return;

                    start -= Main.screenPosition;
                    end -= Main.screenPosition;

                    Texture2D line = ModContent.Request<Texture2D>("RootsCore/WhitePixel").Value;
                    float rotation = (end - start).ToRotation();
                    Vector2 scale = new Vector2(Vector2.Distance(start, end), Thickness);

                    Main.spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
                }


                DrawLineActuallyAccurate(TopLeft + new Vector2(-(Offset + Thickness * 0.5f), -Offset), TopRight + new Vector2((Offset + Thickness * 0.5f), -Offset));
                DrawLineActuallyAccurate(BottomLeft + new Vector2(-(Offset + Thickness * 0.5f), Offset), BottomRight + new Vector2((Offset + Thickness * 0.5f), Offset));
                DrawLineActuallyAccurate(TopLeft + new Vector2(-Offset, -(Offset - Thickness * 0.5f)), BottomLeft + new Vector2(-Offset, (Offset - Thickness * 0.5f)));
                DrawLineActuallyAccurate(BottomRight + new Vector2(Offset, (Offset - Thickness * 0.5f)), TopRight + new Vector2(Offset, -(Offset - Thickness * 0.5f)));
            }

            public void SetOldData()
            {
                if (oldData == null)
                    oldData = new Box() { borderColor = borderColor, borderThickness = borderThickness, boxDimensions = boxDimensions, position = position };
                else
                {
                    oldData.borderThickness = borderThickness;
                    oldData.boxDimensions = boxDimensions;
                    oldData.position = position;
                }
            }
            public bool Contains(Vector2 Position, Vector2 size)
            {
                //0-height or 0-width boxes can never work as containers, so we always try to exclude the player so they work as solid blocks
                if (Size.X <= 0 || Size.Y <= 0)
                    return false;
                return Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(borderThickness * 0.5f), Size + new Vector2(borderThickness), Position, size);
            }

            public bool ContainsIncludingBorder(Vector2 Position, Vector2 size)
            {
                return Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(borderThickness), Size + new Vector2(borderThickness * 2), Position, size);
            }

            public bool ShouldEffectPlayer(Player player) => Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(borderThickness + 16), Size + new Vector2(borderThickness + 16) * 2, player.TopLeft, player.Size);
            public bool InsideInnerHalfOfBorder(Vector2 Position, Vector2 size) => Collision.CheckAABBvAABBCollision(TopLeft - new Vector2(borderThickness) * 0.5f, Size + new Vector2(borderThickness), Position, size);

            public bool PointInWall(Vector2 pos)
            {
                static bool Vector2PointCollision(Vector2 position, Vector2 size, Vector2 point)
                {
                    return (point.X >= position.X && point.X <= position.X + size.X && point.Y >= position.Y && point.Y <= position.Y + size.Y);
                }
                return !Vector2PointCollision(TopLeft, Size, pos) && Vector2PointCollision(TopLeft - new Vector2(borderThickness), Size + new Vector2(borderThickness) * 2, pos);
            }

            public bool CollidesWithBorder(Vector2 pos, Vector2 size)
            {
                return Collision.CheckAABBvAABBCollision(BorderTopLeft, BorderTopRight + new Vector2(0, borderThickness) - BorderTopLeft, pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderTopLeft, BorderBottomLeft + new Vector2(borderThickness, 0) - BorderTopLeft, pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderTopRight - new Vector2(borderThickness, 0), BorderBottomRight - BorderTopRight + new Vector2(borderThickness, 0), pos, size)
                    || Collision.CheckAABBvAABBCollision(BorderBottomLeft - new Vector2(0, borderThickness), BorderBottomRight - BorderBottomLeft + new Vector2(0, borderThickness), pos, size);
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
                    var color = Color.Black * 0.75f;
                    //Inside Fill
                    box.DrawBoxWithOffset(box.borderThickness * 0.5f, box.borderThickness, Color.Black * 0.75f);
                    //Inner Border, if box can actually contain things
                    if (box.Size.X > 0 || box.Size.Y > 0)
                        box.DrawBoxWithOffset(4, 8, box.borderColor);
                    //Outer Border
                    box.DrawBoxWithOffset(box.borderThickness - 4, 8, box.borderColor);
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
                    box.boxDimensions = box.NewDimensions;
                }
                if (box.NewPosition != Vector2.Zero)
                {
                    box.position = box.NewPosition;
                }
                if (box.UpdateBox != null)
                    box.UpdateBox(box);
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
            foreach (var box in ArenaSystem.ActiveBoxes)
            {
                if (box.ShouldEffectPlayer(Player))
                {
                    if (box.Contains(Player.position, Player.Size))
                        ContainPlayerLogic(box);
                    else
                        ExcludePlayerLogic(box);
                }
            }
        }

        void ExcludePlayerLogic(ArenaSystem.Box box)
        {
            if (box.oldData is not null && box.PullPlayerWhenSizeChanged)
            {

                if (box.oldData.CollidesWithBorder(Player.position - Vector2.One, Player.Size + Vector2.One * 2))
                {
                    Vector2 oldRatio = new(Utils.Remap(Player.Center.X, box.oldData.BorderTopLeft.X, box.BorderBottomRight.X, 0, 1, true), Utils.Remap(Player.Center.Y, box.oldData.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                    Vector2 newPos = box.BorderTopLeft + (box.SizeWithBorder) * oldRatio;
                    Vector2 oldPos = box.oldData.BorderTopLeft + box.oldData.SizeWithBorder * oldRatio;
                    Player.position += newPos - oldPos;
                }
            }

            Vector2 playerPos = Player.oldPosition + Player.Size * 0.5f;

            bool InXLine = Player.Right.X > box.BorderTopLeft.X && Player.Left.X < box.BorderBottomRight.X;
            bool InYLine = Player.Bottom.Y > box.BorderTopLeft.Y && Player.Top.Y < box.BorderBottomRight.Y;
            #region Snapping
            if (playerPos.Y > box.BorderTopLeft.Y && playerPos.Y < box.BorderBottomRight.Y)
            {
                if (playerPos.X < box.Center.X && Player.Right.X > box.BorderTopLeft.X)
                {
                    Player.position.X = box.BorderTopLeft.X - Player.width;
                }
                if (playerPos.X > box.Center.X && Player.Left.X < box.BorderBottomRight.X)
                {
                    Player.position.X = box.BorderBottomRight.X;
                }
            }

            if (playerPos.X > box.BorderTopLeft.X && playerPos.X < box.BorderBottomRight.X)
            {
                if (playerPos.Y < box.Center.Y && Player.Bottom.Y > box.BorderTopLeft.Y)
                {
                    Player.position.Y = box.BorderTopLeft.Y - Player.height;
                }
                if (playerPos.Y > box.Center.Y && Player.Top.Y < box.BorderBottomRight.Y)
                {
                    Player.position.Y = box.BorderBottomRight.Y;
                }
            }

            #endregion
            #region Velocity

            var originalVelocity = Player.velocity;
            var originalTopLeft = Player.TopLeft;
            var originalBottomRight = Player.BottomRight;
            var originalCenter = Player.Center;
            Player.position += originalVelocity;

            if (Player.Left.X < box.BorderBottomRight.X && playerPos.X > box.BorderBottomRight.X && InYLine)
            {
                Player.velocity.X = box.BorderBottomRight.X - originalTopLeft.X;
                if (Player.controlLeft && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                {
                    Player.slideDir = -1;
                    applyShoeSpikes();
                }
            }
            if (Player.Right.X > box.BorderTopLeft.X && playerPos.X < box.BorderTopLeft.X && InYLine)
            {
                Player.velocity.X = box.BorderTopLeft.X - originalBottomRight.X;
                if (Player.controlRight && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                {
                    Player.slideDir = 1;
                    applyShoeSpikes();
                }
            }
            if (Player.TopLeft.Y < box.BorderBottomRight.Y && playerPos.Y > box.BorderBottomRight.Y && InXLine)
            {
                Player.velocity.Y = box.BorderBottomRight.Y - originalTopLeft.Y;
                if (Player.velocity.Y == 0)
                    Player.velocity.Y -= 0.000001f;
            }

            if (Player.BottomRight.Y > box.BorderTopLeft.Y && playerPos.Y < box.BorderTopLeft.Y && InXLine)
            {
                Player.velocity.Y = box.BorderTopLeft.Y - originalBottomRight.Y;
            }

            Player.position -= originalVelocity;
            #endregion
        }
        void ContainPlayerLogic(ArenaSystem.Box box)
        {
            if (box.oldData is not null && box.PullPlayerWhenSizeChanged)
            {

                if (box.oldData.CollidesWithBorder(Player.position - Vector2.One, Player.Size + Vector2.One * 2))
                {
                    Vector2 oldRatio = new(Utils.Remap(Player.Center.X, box.oldData.TopLeft.X, box.BottomRight.X, 0, 1, true), Utils.Remap(Player.Center.Y, box.oldData.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                    Vector2 newPos = box.TopLeft + box.Size * oldRatio;
                    Vector2 oldPos = box.oldData.TopLeft + box.oldData.Size * oldRatio;
                    Player.position += newPos - oldPos;
                }
            }

            #region Snapping
            if (Player.Left.X < box.TopLeft.X)
            {
                Player.position.X = box.TopLeft.X;
            }
            if (Player.Right.X > box.BottomRight.X)
            {
                Player.position.X = box.BottomRight.X - Player.width;
            }
            if (Player.TopLeft.Y < box.TopLeft.Y)
            {
                Player.position.Y = box.TopLeft.Y;
            }
            if (Player.BottomRight.Y > box.BottomRight.Y)
            {
                Player.position.Y = box.BottomRight.Y - Player.height;
            }

            #endregion
            #region Velocity

            var originalVelocity = Player.velocity;
            var originalTopLeft = Player.TopLeft;
            var originalBottomRight = Player.BottomRight;
            Player.position += originalVelocity;

            if (Player.Left.X < box.TopLeft.X)
            {
                Player.velocity.X = box.TopLeft.X - originalTopLeft.X;
                if (Player.controlLeft && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                {
                    Player.slideDir = -1;
                    applyShoeSpikes();
                }
            }
            if (Player.Right.X > box.BottomRight.X)
            {
                Player.velocity.X = box.BottomRight.X - originalBottomRight.X;
                if (Player.controlRight && Player.spikedBoots > 0 && Player.velocity.Y >= 0 && !Player.mount.Active)
                {
                    Player.slideDir = 1;
                    applyShoeSpikes();
                }
            }
            if (Player.TopLeft.Y < box.TopLeft.Y)
            {

                Player.velocity.Y = box.TopLeft.Y - originalTopLeft.Y;
                if (Player.velocity.Y == 0)
                    Player.velocity.Y += 0.001f;
            }

            if (Player.BottomRight.Y > box.BottomRight.Y)
            {
                Player.velocity.Y = box.BottomRight.Y - originalBottomRight.Y;
            }
            Player.position -= originalVelocity;
            #endregion
        }

        void spawnShoeDust()
        {

            int num4 = Dust.NewDust(new Vector2(Player.position.X + (float)(Player.width / 2) + (float)((Player.width / 2 - 4) * Player.slideDir), Player.position.Y + (float)(Player.height / 2) + (float)(Player.height / 2 - 4) * Player.gravDir), 8, 8, DustID.Smoke);
            if (Player.slideDir < 0)
            {
                Main.dust[num4].position.X -= 10f;
            }
            if (Player.gravDir < 0f)
            {
                Main.dust[num4].position.Y -= 12f;
            }
            Main.dust[num4].velocity *= 0.1f;
            Main.dust[num4].scale *= 1.2f;
            Main.dust[num4].noGravity = true;
            Main.dust[num4].shader = GameShaders.Armor.GetSecondaryShader(Player.cShoe, Player);
        }
        void applyShoeSpikes()
        {
            if (Player.spikedBoots == 0 || Player.velocity.Y < 0 || Player.mount.Active)
                return;
            if (Player.controlDown && Player.spikedBoots > 0)
            {
                Player.velocity.Y = 4f * Player.gravDir;
                spawnShoeDust();
            }
            else if (Player.spikedBoots >= 2)
            {
                Player.velocity.Y = 0;
            }
            else if (Player.spikedBoots == 1)
            {

                Player.velocity.Y = 0.5f * Player.gravDir;
                spawnShoeDust();
            }
        }
    }

    public class ArenaWallProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        /// <summary>
        /// The location of this projectile in an arena box, used for maintaining this when the arena box moves.
        /// </summary>
        public Vector2 arenaBoxPosition = Vector2.Zero;
        /// <summary>
        /// Which Box this projectile is attached to
        /// </summary>
        public ArenaSystem.Box arenaBox = null;

    }

    public partial class RootsCore : Mod
    {

        #region Allow Grappling Hooks to grab arena walls
        public static void AllowHooksToGrabArenabox(On_Projectile.orig_AI_007_GrapplingHooks orig, Projectile self)
        {
            if (Main.player[self.owner].dead || Main.player[self.owner].stoned || Main.player[self.owner].webbed || Main.player[self.owner].frozen)
            {
                self.Kill();
                return;
            }
            static bool Vector2PointCollision(Vector2 position, Vector2 size, Vector2 point)
            {
                return (point.X >= position.X && point.X <= position.X + size.X && point.Y >= position.Y && point.Y <= position.Y + size.Y);
            }

            bool intersectingWall = false;
            var modproj = self.GetGlobalProjectile<ArenaWallProj>();
            if (modproj.arenaBox is not null)
            {
                var box = modproj.arenaBox;
                if (ArenaSystem.ActiveBoxes.Contains(box))
                {
                    self.Center = box.TopLeft + box.Size * modproj.arenaBoxPosition;
                }
                else
                {
                    modproj.arenaBox = null;
                }
            }
            if (ArenaSystem.ActiveBoxes.Count > 0)
            {
                foreach (var box in ArenaSystem.ActiveBoxes)
                {
                    if (!Vector2PointCollision(box.TopLeft, box.Size, self.Center) && Vector2PointCollision(box.TopLeft - new Vector2(box.borderThickness), box.Size + new Vector2(box.borderThickness) * 2, self.Center))
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
                            modproj.arenaBoxPosition = new Vector2(Utils.Remap(self.Center.X, box.TopLeft.X, box.BottomRight.X, 0, 1, false), Utils.Remap(self.Center.Y, box.TopLeft.Y, box.BottomRight.Y, 0, 1, false));
                            modproj.arenaBox = box;
                        }
                        if (self.ai[0] == 2) //don't run this if the hook is returning!
                        {
                            self.rotation = self.DirectionFrom(Main.player[self.owner].Center).ToRotation() + MathHelper.PiOver2;
                            intersectingWall = true;

                            if (Main.player[self.owner].grapCount < 10)
                            {
                                Main.player[self.owner].grappling[Main.player[self.owner].grapCount] = self.whoAmI;
                                Main.player[self.owner].grapCount++;
                            }
                        }
                    }
                }
            }
            if (!intersectingWall)
                orig(self);
        }
        #endregion

        #region Arena Collision for other things

        private static bool ArenaCollision_Vector2_int_int_bool(On_Collision.orig_SolidCollision_Vector2_int_int_bool orig, Vector2 Position, int Width, int Height, bool acceptTopSurfaces)
        {
            if (ArenaSystem.ActiveBoxes.Count > 0)
            {
                foreach (var item in ArenaSystem.ActiveBoxes)
                {
                    if (item.CollidesWithBorder(Position, new(Width, Height)))
                        return true;
                }
            }
            return orig(Position, Width, Height, acceptTopSurfaces);
        }

        private static bool ArenaCollision_Vector2_int_int(On_Collision.orig_SolidCollision_Vector2_int_int orig, Vector2 Position, int Width, int Height)
        {
            if (ArenaSystem.ActiveBoxes.Count > 0)
            {
                foreach (var item in ArenaSystem.ActiveBoxes)
                {
                    if (item.CollidesWithBorder(Position, new(Width, Height)))
                        return true;
                }
            }
            return orig(Position, Width, Height);
        }


        private static Vector2 ArenaCollision_TileCollision(On_Collision.orig_TileCollision orig, Vector2 Position, Vector2 Velocity, int Width, int Height, bool fallThrough, bool fall2, int gravDir)
        {
            Velocity = orig(Position, Velocity, Width, Height, fallThrough, fall2, gravDir);
            if (ArenaSystem.ActiveBoxes.Count > 0 && Velocity != Vector2.Zero)
            {
                foreach (var item in ArenaSystem.ActiveBoxes)
                {
                    var oldVel = Velocity;
                    if (item.ContainsIncludingBorder(Position + Velocity, new Vector2(Width, Height)))
                        if (item.InsideInnerHalfOfBorder(Position, new Vector2(Width, Height)))
                            Velocity = ContainLogic(item, Position, Width, Height, Velocity);
                        else
                            Velocity = ExcludeLogic(item, Position, Width, Height, Velocity);
                    var dif = (oldVel - Velocity).Length();
                }
            }
            return Velocity;
        }

        private static Vector2 ContainLogic(ArenaSystem.Box box, Vector2 Position, int Width, int Height, Vector2 Velocity)
        {
            var originalVelocity = Velocity;
            var originalTopLeft = Position;
            var originalBottomRight = Position + new Vector2(Height, Width);
            Position += originalVelocity;

            if (Position.X < box.TopLeft.X)
            {
                Velocity.X = box.TopLeft.X - originalTopLeft.X;
            }
            if (Position.X + Width > box.BottomRight.X)
            {
                Velocity.X = box.BottomRight.X - originalBottomRight.X;
            }
            if (Position.Y < box.TopLeft.Y)
            {
                Velocity.Y = box.TopLeft.Y - originalTopLeft.Y;
            }

            if (Position.Y + Height > box.BottomRight.Y)
            {
                Velocity.Y = box.BottomRight.Y - originalBottomRight.Y;
            }
            return Velocity;
        }
        private static Vector2 ExcludeLogic(ArenaSystem.Box box, Vector2 Position, int Width, int Height, Vector2 Velocity)
        {
            var originalVelocity = Velocity;
            var originalTopLeft = Position;
            var originalBottomRight = Position + new Vector2(Height, Width);
            var playerPos = Position + new Vector2(Width, Height) * 0.5f;
            bool InXLine = originalBottomRight.X > box.BorderTopLeft.X && originalTopLeft.X < box.BorderBottomRight.X;
            bool InYLine = originalBottomRight.Y > box.BorderTopLeft.Y && originalTopLeft.Y < box.BorderBottomRight.Y;
            Position += originalVelocity;

            var topLeft = Position;
            var bottomRight = Position + new Vector2(Height, Width);

            if (topLeft.X <= box.BorderBottomRight.X && playerPos.X >= box.BorderBottomRight.X && InYLine)
            {
                Velocity.X = box.BorderBottomRight.X - originalTopLeft.X;
            }
            if (bottomRight.X >= box.BorderTopLeft.X && playerPos.X <= box.BorderTopLeft.X && InYLine)
            {
                Velocity.X = box.BorderTopLeft.X - originalBottomRight.X;
            }
            if (topLeft.Y <= box.BorderBottomRight.Y && playerPos.Y >= box.BorderBottomRight.Y && InXLine)
            {
                Velocity.Y = box.BorderBottomRight.Y - originalTopLeft.Y;
            }
            var c = Math.Ceiling(bottomRight.Y);
            if ( c >= box.BorderTopLeft.Y && playerPos.Y <= box.BorderTopLeft.Y && InXLine)
            {
                Velocity.Y = box.BorderTopLeft.Y - originalBottomRight.Y;
            }
            if (originalVelocity == Velocity)
                return -Velocity;
            return Velocity;
        }
        #endregion

    }
}