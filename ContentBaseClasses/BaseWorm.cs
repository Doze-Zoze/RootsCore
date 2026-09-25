using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RootsCore.ContentBaseClasses
{
    #region Animations
    public class WormAnimation
    {
        /// <summary>
        /// Used to store the animation key frames that an entity inheriting <see cref="BaseWormNPC"/> should use whilst invokving an active <see cref="WormAnimation"/> <br></br>
        /// The collection is indexed by the frame at which the section of the animation should begin. <br></br>
        /// The values at each index should specifiy the keyframe, which controls the rotations of a specified set of segments, and the rotation of the entire body.
        /// </summary>
        public Dictionary<int, (WormKeyframe SegmentPositions, float BodyRotation)> AnimationKeyframes = new();

        /// <summary>
        /// The rigidity of segments. 1f instantly snaps into place
        /// </summary>
        public float segmentRigidity = 0.75f;
        /// <summary>
        /// Should this animation mirror to match the worm's X direction?
        /// </summary>
        public bool mirror = true;
        /// <summary>
        /// Should this animation apply the animation's rotation values?
        /// </summary>
        public bool applyRotation = true;
        public class WormKeyframe
        {
            public float[] segmentOffsets = new float[1];
            /// <summary>
            /// Creates the segment rotation offsets for the given keyframe.
            /// Everything defaults to NaN, which will inherit the rotation offset of the frame before it.
            /// </summary>
            public WormKeyframe(params (float Segment, float Rotation)[] parameters)
            {
                segmentOffsets = new float[200];
                var off = new List<float>();
                for (var i = 0; i < 200; i++)
                {
                    bool broken = false;
                    for (var i2 = 0; i2 < parameters.Length; i2++)
                    {
                        if (parameters[i2].Item1 == i)
                        {
                            segmentOffsets[i] = parameters[i2].Item2;
                            broken = true;
                            break;
                        }
                    }
                    if (!broken)
                        segmentOffsets[i] = float.NaN;
                }
            }

            public static WormKeyframe GetCurrent(BaseWormNPC HeadNPC)
            {
                WormKeyframe kf = new();
                for (var i = 0; i < HeadNPC.Segments.Count(); i++)
                {
                    if (i == 0)
                    {

                        var dif = ShortestAngle(HeadNPC.NPC.rotation, HeadNPC.Segments[i].rotation);
                        kf.segmentOffsets[i] = dif;
                        //Main.NewText((HeadNPC.NPC.rotation, HeadNPC.Segments[i].DirectionFrom(HeadNPC.NPC.Center).ToRotation()));
                    }
                    else
                    {
                        var dif = ShortestAngle(HeadNPC.Segments[i - 1].rotation, HeadNPC.Segments[i].rotation);
                        kf.segmentOffsets[i] = dif;
                    }
                }
                return kf;
            }
            static float ShortestAngle(float from, float to)
            {
                float difference = to - from;

                while (difference < -MathHelper.Pi)
                    difference += MathHelper.TwoPi;
                while (difference > MathHelper.Pi)
                    difference -= MathHelper.TwoPi;

                return difference;
            }
            /// <summary>
            /// Converts this keyframe into a usable array.
            /// </summary>
            /// <returns></returns>
            public float[] ClearTheNaNs()
            {
                float[] offsets = new float[segmentOffsets.Length];
                for (var i = 0; i < segmentOffsets.Length; i++)
                {
                    if (float.IsNaN(segmentOffsets[i]))
                    {
                        if (i == 0)
                            offsets[i] = 0;
                        else
                            offsets[i] = offsets[i - 1];
                    }
                    else
                    {
                        offsets[i] = segmentOffsets[i];
                    }
                }
                return offsets;
            }
        }

        /// <summary>
        /// Applies this animation for the given frame
        /// </summary>
        /// <param name="HeadEntity"></param>
        /// <param name="frame"></param>
        public void ApplyAnimationFrame(Entity HeadEntity, float frame, float squshMult = 1)
        {
            var orderedKeyframes = AnimationKeyframes.OrderBy(x => x.Key);
            (int, float[], float)? prev = null;
            (int, float[], float)? next = null;
            float[] goalSegmentRotOffsets = new float[200];
            float goalRotation = float.NaN;
            foreach (var item in orderedKeyframes)
            {
                if (item.Key < frame)
                {
                    prev = (item.Key, item.Value.Item1.ClearTheNaNs(), item.Value.Item2);
                }
                else
                {
                    next = (item.Key, item.Value.Item1.ClearTheNaNs(), item.Value.Item2);
                    break;
                }
            }
            if (next.HasValue && prev.HasValue)
            {
                float completion = (frame - prev.Value.Item1) / (next.Value.Item1 - prev.Value.Item1);
                for (var i = 0; i < goalSegmentRotOffsets.Length; i++)
                {
                    goalSegmentRotOffsets[i] = MathHelper.Lerp(prev.Value.Item2[i], next.Value.Item2[i], completion);
                }
                if (float.IsNaN(next.Value.Item3) && frame == next.Value.Item1)
                    AnimationKeyframes[next.Value.Item1] = (AnimationKeyframes[next.Value.Item1].Item1, next.Value.Item3);
                if (!float.IsNaN(prev.Value.Item3))
                {
                    goalRotation = MathHelper.Lerp(prev.Value.Item3, next.Value.Item3, completion);
                }
            }
            else
            {
                if (prev.HasValue)
                    goalSegmentRotOffsets = prev.Value.Item2;
                if (next.HasValue)
                {
                    goalSegmentRotOffsets = next.Value.Item2;
                    if (float.IsNaN(next.Value.Item3) && frame == next.Value.Item1)
                        AnimationKeyframes[next.Value.Item1] = (AnimationKeyframes[next.Value.Item1].Item1, next.Value.Item3);
                }
            }
            //We use the same animation framework for both projectiles and NPCs, but applying it is different for each type and so we do the final applications here.
            if (HeadEntity is NPC n&& n.ModNPC is BaseWormNPC HeadNPC)
            {
                if (HeadNPC.Segments.Count > 0)
                {
                    if (applyRotation && !(float.IsNaN(goalRotation)))
                    {
                        HeadNPC.NPC.rotation = goalRotation * (mirror ? HeadNPC.NPC.velocity.X < 0 ? -1 : 1 : 1);
                    }
                    for (int i = 0; i < HeadNPC.Segments.Count; i++)
                    {

                        Vector2 pos1 = HeadNPC.NPC.Center;
                        float dist = HeadNPC.SegmentTypePositionOffsets[0] * squshMult;
                        if (i != 0)
                        {
                            dist = HeadNPC.SegmentTypePositionOffsets[HeadNPC.Segments[i - 1].segmentType + 1] * squshMult;
                        }
                        dist *= HeadNPC.NPC.scale;
                        Vector2 rot = (HeadNPC.NPC.rotation - MathHelper.PiOver2).ToRotationVector2();
                        if (rot == Vector2.Zero)
                            rot = HeadNPC.Segments[0].Center.AngleTo(HeadNPC.NPC.Center).ToRotationVector2();
                        if (i >= 1)
                        {
                            pos1 = HeadNPC.Segments[i - 1].Center;
                            rot = HeadNPC.Segments[i - 1].velocity.SafeNormalize(Vector2.Zero);
                        }
                        rot = rot.RotatedBy(goalSegmentRotOffsets[i] * (mirror ? HeadNPC.NPC.velocity.X < 0 ? -1 : 1 : 1));
                        var dir = HeadNPC.Segments[i].Center.DirectionFrom(pos1);
                        HeadNPC.Segments[i].Center = pos1 + Vector2.Lerp(dir, -(rot), segmentRigidity) * dist;
                        float rotationOffset = Vector2.Lerp(dir, -(rot), segmentRigidity).ToRotation();
                        float finalOffset = (-rot).ToRotation().AngleLerp(rotationOffset, 0);
                        HeadNPC.Segments[i].Center = pos1 + finalOffset.ToRotationVector2() * dist;
                        HeadNPC.Segments[i].velocity = HeadNPC.Segments[i].Center.DirectionTo(pos1);
                        HeadNPC.Segments[i].rotation = HeadNPC.Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                    }

                    for (int i = 1; i < HeadNPC.Segments.Count - 1; i++)
                    {
                        HeadNPC.Segments[i].rotation = HeadNPC.Segments[i + 1].Center.DirectionTo(HeadNPC.Segments[i - 1].Center).ToRotation() + MathHelper.PiOver2;
                    }
                }
            }
            else if (HeadEntity is Projectile p && p.ModProjectile is BaseWormProjectile HeadProjectile)
            {
                if (HeadProjectile.Segments.Count > 0)
                {
                    if (applyRotation && !(float.IsNaN(goalRotation)))
                    {
                        HeadProjectile.Projectile.rotation = goalRotation * (mirror ? HeadProjectile.Projectile.velocity.X < 0 ? -1 : 1 : 1);
                    }
                    for (int i = 0; i < HeadProjectile.Segments.Count; i++)
                    {

                        Vector2 pos1 = HeadProjectile.Projectile.Center;
                        float dist = HeadProjectile.SegmentTypePositionOffsets[0] * squshMult;
                        if (i != 0)
                        {
                            dist = HeadProjectile.SegmentTypePositionOffsets[HeadProjectile.Segments[i - 1].segmentType + 1] * squshMult;
                        }
                        dist *= HeadProjectile.Projectile.scale;
                        Vector2 rot = (HeadProjectile.Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
                        if (rot == Vector2.Zero)
                            rot = HeadProjectile.Segments[0].Center.AngleTo(HeadProjectile.Projectile.Center).ToRotationVector2();
                        if (i >= 1)
                        {
                            pos1 = HeadProjectile.Segments[i - 1].Center;
                            rot = HeadProjectile.Segments[i - 1].velocity.SafeNormalize(Vector2.Zero);
                        }
                        rot = rot.RotatedBy(goalSegmentRotOffsets[i] * (mirror ? HeadProjectile.Projectile.velocity.X < 0 ? -1 : 1 : 1));
                        var dir = HeadProjectile.Segments[i].Center.DirectionFrom(pos1);
                        HeadProjectile.Segments[i].Center = pos1 + Vector2.Lerp(dir, -(rot), segmentRigidity) * dist;
                        HeadProjectile.Segments[i].velocity = HeadProjectile.Segments[i].Center.DirectionTo(pos1);
                        HeadProjectile.Segments[i].rotation = HeadProjectile.Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                    }

                    for (int i = 1; i < HeadProjectile.Segments.Count - 1; i++)
                    {
                        HeadProjectile.Segments[i].rotation = HeadProjectile.Segments[i + 1].Center.DirectionTo(HeadProjectile.Segments[i - 1].Center).ToRotation() + MathHelper.PiOver2;
                    }
                }
            }
        }
    }
    #endregion
    public enum SegmentFollowLogic
    {
        Regular = 0, //Traditional worm segment logic
        Exact = 1, //Follows the path the head took exactly
                   // In the future, add a segment logic that supports solid tile collisions and forces applied to *any* segment
    }
    #region Segment Class
    /// <summary>
    /// This class is used to store the information of each segment's location, size, and texture path
    /// Due to the amount of segments, there is only one actual NPC for this boss
    /// </summary>
    public class BaseWormSegment
    {
        /// <summary>
        /// The type of segment this is. Segment type is used to determine spacing and textures.
        /// </summary>
        public int segmentType = 0;

        /// <summary>
        /// The position of the center of this segment
        /// </summary>
        public Vector2 Center = Vector2.Zero;

        /// <summary>
        /// The rotation of this segment in radians
        /// </summary>
        public float rotation = 0;

        /// <summary>
        /// The velocity this segment has.<br/>
        /// Currently doesn't actually do anything besides be a token value if needed to be read by something
        /// </summary>
        public Vector2 velocity = Vector2.Zero;
        /// <summary>
        /// How opaque this segment is. Ranges from 1 (fully opaque) to 0 (fully transparent)
        /// </summary>
        public float Opacity = 1;
        /// <summary>
        /// The rigidity of this segment's attachment to the segment before it. If null, uses the rigidity value of the worm.<br/>
        /// Defaults to null.
        /// </summary>
        public float? Rigidity = null;
        public BaseWormSegment(ModNPC Head, int segmentStyle = 0)
        {
            Center = Head.NPC.Center;
            rotation = Head.NPC.rotation;
            velocity = Head.NPC.velocity;
            segmentType = segmentStyle;
        }

        public BaseWormSegment(ModProjectile Head, int segmentStyle = 0)
        {
            Center = Head.Projectile.Center;
            rotation = Head.Projectile.rotation;
            velocity = Head.Projectile.velocity;
            segmentType = segmentStyle;
        }
    }
    #endregion
    public abstract class BaseWormNPC : ModNPC
    {
        #region Abstracts & Commonly Overriden Fields
        /// <summary>
        /// The amount of segments of this worm. This does not include the head
        /// </summary>
        public abstract int SegmentCount { get; }
        /// <summary>
        /// A list of the offsets to the next segment in the worm from this segment.
        /// DOES include the head
        /// </summary>
        public abstract List<float> SegmentTypePositionOffsets { get; }
        /// <summary>
        /// A list of all textures for the segments to draw with.
        /// does NOT include the head
        /// </summary>
        public abstract List<string> SegmentTextures { get; }

        /// <summary>
        /// A list of all glow textures for the segments to draw with.
        /// DOES include the head
        /// </summary>
        public virtual List<string?> GlowTextures { get; }
        /// <summary>
        /// The type of the worm hitbox NPC<br/>
        /// Make sure that NPC is a child of BaseWormHitboxNPC!
        /// </summary>
        public abstract int WormHitboxNpcType { get; }
        /// <summary>
        /// Offsets for drawing each segment
        /// </summary>
        public List<Vector2> SegmentTypeDrawOffsets = new List<Vector2>();


        /// <summary>
        /// How far through the current animation this worm is
        /// </summary>
        public float AnimationFrame = 0;
        /// <summary>
        /// The active animation for the worm
        /// </summary>
        public WormAnimation ActiveAnimation = null;
        #endregion

        #region Segments
        /// <summary>
        /// The list of all segments of this worm
        /// </summary>
        public List<BaseWormSegment> Segments = new();

        /// <summary>
        /// The textures for each segment type of this worm. Works like getting a texture from TextureAssets
        /// </summary>
        public List<Asset<Texture2D>> SegmentTextureAssets
        {
            get
            {
                if (internalTexAssets.Count == 0)
                    for (var i = 0; i < SegmentTextures.Count; i++)
                    {
                        internalTexAssets.Add(ModContent.Request<Texture2D>(SegmentTextures[i]));
                        if (SegmentTypeDrawOffsets.Count <= i)
                        {
                            SegmentTypeDrawOffsets.Add(Vector2.Zero);
                        }
                    }
                return internalTexAssets;
            }
        }

        /// <summary>
        /// Internal list that stores the textureassets.<br/>
        /// Use SegmentTextureAssets to get the data stored here.
        /// </summary>
        private List<Asset<Texture2D>> internalTexAssets = new List<Asset<Texture2D>>();
        /// <summary>
        /// The textures for each glow type of this worm. Works like getting a texture from TextureAssets
        /// </summary>
        public List<Asset<Texture2D>> GlowTextureAssets
        {
            get
            {
                if (internalGlowAssets.Count == 0)
                    for (var i = 0; i < GlowTextures.Count; i++)
                    {
                        if (GlowTextures[i] is not null)
                            internalGlowAssets.Add(ModContent.Request<Texture2D>(GlowTextures[i]));
                        else internalGlowAssets.Add(ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj"));
                    }
                return internalGlowAssets;
            }
        }

        /// <summary>
        /// Internal list that stores the glow textureassets.<br/>
        /// Use SegmentTextureAssets to get the data stored here.
        /// </summary>
        private List<Asset<Texture2D>> internalGlowAssets = new List<Asset<Texture2D>>();



        /// <summary>
        /// Which type of segment following logic this worm should currently use
        /// </summary>
        public SegmentFollowLogic SegmentFollowType;

        /// <summary>
        /// How rigid the segment should be when using default segment logic
        /// </summary>
        public float SegmentRigidity = 0.2f;

        /// <summary>
        /// The max rotational offset from the direction of the previous segment
        /// </summary>
        public float SegmentMaxRotation = MathHelper.TwoPi;

        /// <summary>
        /// The points used by ExactSegmentLogic to exactly follow the head
        /// </summary>
        private List<Vector2> segmentPoints = new List<Vector2>();

        /// <summary>
        /// Updates the positions of the segments based on the value set in SegmentFollowType
        /// </summary>
        public void UpdateSegments(float squshMult = 1)
        {
            NPC.position += NPC.velocity; //Update this segment's movement so that all other segments use this as the base. This is undone at the end of the method.
            if (ActiveAnimation is not null)
            {
                ActiveAnimation.ApplyAnimationFrame(NPC, AnimationFrame, squshMult);
                AnimationFrame++;
                if (AnimationFrame > ActiveAnimation.AnimationKeyframes.Keys.Max())
                {
                    AnimationFrame = 0;
                    ActiveAnimation = null;
                }
            }
            else
            {
                if (segmentPoints.Count < 1 || segmentPoints[0].Distance(NPC.Center) > 8)
                {
                    segmentPoints.Insert(0, NPC.Center);
                }
                while (segmentPoints.Count > 300)
                {
                    segmentPoints.RemoveAt(segmentPoints.Count - 1);
                }
                switch (SegmentFollowType)
                {
                    case (SegmentFollowLogic.Regular):
                        RegularSegmentLogic(squshMult);
                        break;
                    case (SegmentFollowLogic.Exact):
                        ExactSegmentLogic(squshMult);
                        break;
                }
            }
            NPC.position -= NPC.velocity;

            //Spawning hitboxes for all the segments near the player
            SpawnHitboxes();

        }

        private void RegularSegmentLogic(float squshMult)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                float segmentDistance = SegmentTypePositionOffsets[0] * squshMult;
                var thisSeg = Segments[i];
                var aheadSeg = new BaseWormSegment(this);
                if (i != 0)
                {
                    aheadSeg = Segments[i - 1];
                    segmentDistance = SegmentTypePositionOffsets[Segments[i - 1].segmentType + 1] * squshMult;
                }
                segmentDistance *= NPC.scale;
                Vector2 nexSegDir = aheadSeg.Center - thisSeg.Center;
                if (aheadSeg.rotation != thisSeg.rotation)
                {
                    nexSegDir = nexSegDir.RotatedBy(MathHelper.WrapAngle(aheadSeg.rotation - thisSeg.rotation) * (thisSeg.Rigidity ?? SegmentRigidity));
                    nexSegDir = nexSegDir.MoveTowards((aheadSeg.rotation - thisSeg.rotation).ToRotationVector2(), 1f);
                }
                thisSeg.rotation = nexSegDir.ToRotation() + MathHelper.PiOver2;
                float angledif = MathHelper.WrapAngle(thisSeg.rotation - aheadSeg.rotation);
                thisSeg.rotation = thisSeg.rotation.AngleLerp(aheadSeg.rotation + MathHelper.Clamp(angledif, -SegmentMaxRotation * 0.5f, SegmentMaxRotation * 0.5f), 0.25f);
                thisSeg.Center = aheadSeg.Center - (thisSeg.rotation - MathHelper.PiOver2).ToRotationVector2() * segmentDistance;

            }
        }

        private void ExactSegmentLogic(float squshMult)
        {
            float dist = 40f;
            int segmentPointInUse = 0;
            for (int i = 0; i < Segments.Count; i++)
            {
                var thisSeg = Segments[i];
                var aheadSeg = new BaseWormSegment(this);
                if (i != 0)
                    aheadSeg = Segments[i - 1];
                bool hasMoved = false;
                while (segmentPointInUse < segmentPoints.Count)
                {
                    if (segmentPointInUse == 0)
                    {
                        if (aheadSeg.Center.Distance(segmentPoints[0]) >= dist)
                        {
                            thisSeg.Center = aheadSeg.Center + aheadSeg.Center.DirectionTo(segmentPoints[0]) * dist;
                            Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                            Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                            hasMoved = true;
                            break;
                        }
                        else
                        {
                            segmentPointInUse++;
                        }
                    }
                    else
                    {
                        if (aheadSeg.Center.Distance(segmentPoints[segmentPointInUse]) >= dist)
                        {
                            thisSeg.Center = aheadSeg.Center + aheadSeg.Center.DirectionTo(segmentPoints[segmentPointInUse]) * dist;
                            Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                            Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                            hasMoved = true;
                            break;
                        }
                        else
                        {
                            segmentPointInUse++;
                        }
                    }
                }
                if (!hasMoved && segmentPointInUse >= segmentPoints.Count)
                {
                    thisSeg.Center = segmentPoints[segmentPoints.Count - 1];
                    Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                    Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                    hasMoved = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Spawns the hitboxes for the worm's segments.
        /// Won't spawn hitboxes if there's less than 5 avaliable NPC slots
        /// </summary>
        public virtual void SpawnHitboxes()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                for (int i = 0; i < Segments.Count; i++)
                {
                    if (Main.npc.Count(x => x.active) >= Main.maxNPCs - 5)
                        break;
                    if (Main.player.Any(x => x.active && Segments[i].Center.Distance(x.Center) < 1200))
                    {
                        if (Main.npc.Any(x => x.active && x.type == WormHitboxNpcType && x.ai[1] == i && x.ai[0] == NPC.whoAmI))
                        {
                            Main.npc.First(x => x.active && x.type == WormHitboxNpcType && x.ai[1] == i && x.ai[0] == NPC.whoAmI).ai[2] = 0;
                        }
                        else
                            NPC.NewNPC(NPC.GetSource_Misc("Hitbox"), (int)Segments[i].Center.X, (int)Segments[i].Center.Y + 100, WormHitboxNpcType, ai0: NPC.whoAmI, ai1: i);
                    }
                }
        }
        #endregion

        #region Defaults

        public override void SetStaticDefaults()
        {
            NPCID.Sets.MustAlwaysDraw[Type] = true;
        }
        public override void SetDefaults()
        {
            for (var i = 0; i < SegmentCount - 1; i++)
            {
                Segments.Add(new BaseWormSegment(this, 0));
            }
            Segments.Add(new BaseWormSegment(this, 1));
        }

        #endregion

        #region Draw

        public override void FindFrame(int frameHeight)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                NPC.rotation = MathF.Sin(Main.GlobalTimeWrappedHourly) * 0.2f + MathHelper.PiOver2;
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.IsABestiaryIconDummy)
            {
                drawColor = Color.White;
                SegmentRigidity = 0.1f;
                UpdateSegments();
            }
            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                DrawSegment(spriteBatch, screenPos, drawColor, Segments[i]);
            }
            spriteBatch.Draw(TextureAssets.Npc[Type].Value, NPC.Center - screenPos, null, drawColor * NPC.Opacity, NPC.rotation, TextureAssets.Npc[Type].Value.Size() / 2, NPC.scale, SpriteEffects.None, 1);
            if (GlowTextures.Count > 0 && GlowTextures[0] is not null)
                spriteBatch.Draw(GlowTextureAssets[0].Value, NPC.Center - screenPos, null, Color.White * NPC.Opacity, NPC.rotation, GlowTextureAssets[0].Size() / 2, NPC.scale, SpriteEffects.None, 1);
            return false;
        }

        public virtual void DrawSegment(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, BaseWormSegment segment)
        {
            var color = Lighting.GetColor(segment.Center.ToTileCoordinates());
            if (NPC.IsABestiaryIconDummy)
            {
                color = Color.White;
            }
            if (!SegmentTextureAssets.IndexInRange(segment.segmentType))
            {
                return;
            }
            var tex = SegmentTextureAssets[segment.segmentType].Value;
            spriteBatch.Draw(tex, segment.Center - screenPos, null, color * segment.Opacity, segment.rotation, tex.Size() / 2 + (SegmentTypeDrawOffsets[segment.segmentType]), NPC.scale, SpriteEffects.None, 1);
            if (GlowTextures is null || !GlowTextures.IndexInRange(segment.segmentType + 1) || GlowTextures[segment.segmentType + 1] is null)
            {
                return;
            }
            tex = GlowTextureAssets[segment.segmentType + 1].Value;
            spriteBatch.Draw(tex, segment.Center - screenPos, null, Color.White * segment.Opacity, segment.rotation, tex.Size() / 2 + (SegmentTypeDrawOffsets[segment.segmentType]), NPC.scale, SpriteEffects.None, 1);
        }
        #endregion
    }

    //The segments near a player spawn a hitbox NPC to allow damaging players and taking damage
    public abstract class BaseWormHitboxNPC : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 200;
            NPC.height = 200;
            NPC.lifeMax = 10000;
            NPC.knockBackResist = 0;
            NPC.noTileCollide = true;
            NPC.dontCountMe = true;
            NPC.aiStyle = -1;
        }

        public override void AI()
        {
            var headNPC = Main.npc[(int)NPC.ai[0]];
            if (!(headNPC.ModNPC is BaseWormNPC && headNPC.active))
            {
                NPC.active = false;
                return;
            }
            NPC.realLife = (int)NPC.ai[0];
            NPC.damage = headNPC.damage;
            NPC.lifeMax = headNPC.lifeMax;
            NPC.life = headNPC.life;
            NPC.Center = (headNPC.ModNPC as BaseWormNPC).Segments[(int)NPC.ai[1]].Center;
        }
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return false;
        }
    }

    /// <summary>
    /// Implementation to make ModProjectile worms much easier to make.
    /// Modified version of BaseWormNPC to function on a ModProjectile instead of ModNPC
    /// </summary>
    public abstract class BaseWormProjectile : ModProjectile
    {
        #region Abstracts / Commonly Overridden Fields
        /// <summary>
        /// The amount of segments of this worm. This does not include the head
        /// </summary>
        public abstract int SegmentCount { get; }
        /// <summary>
        /// A list of the offsets to the next segment in the worm from this segment
        /// DOES include the head
        /// </summary>
        public abstract List<float> SegmentTypePositionOffsets { get; }
        /// <summary>
        /// A list of all textures for the segments to draw with
        /// does NOT include the head
        /// </summary>
        public abstract List<string> SegmentTextures { get; }
        /// <summary>
        /// Offsets for drawing each segment
        /// </summary>
        public List<Vector2> SegmentTypeDrawOffsets = new List<Vector2>();

        /// <summary>
        /// A list of all glow textures for the segments to draw with
        /// DOES include the head
        /// </summary>
        public virtual List<string?> GlowTextures { get; }

        /// <summary>
        /// How far through the current animation this worm is
        /// </summary>
        public float AnimationFrame = 0;
        /// <summary>
        /// The active animation for the worm
        /// </summary>
        public WormAnimation ActiveAnimation = null;
        #endregion
        #region Segments
        /// <summary>
        /// The textures for each segment type of this worm. Works like getting a texture from TextureAssets
        /// </summary>
        public List<Asset<Texture2D>> SegmentTextureAssets
        {
            get
            {
                if (internalTexAssets.Count == 0)
                    for (var i = 0; i < SegmentTextures.Count; i++)
                    {
                        internalTexAssets.Add(ModContent.Request<Texture2D>(SegmentTextures[i]));
                        if (SegmentTypeDrawOffsets.Count <= i)
                        {
                            SegmentTypeDrawOffsets.Add(Vector2.Zero);
                        }
                    }
                return internalTexAssets;
            }
        }

        /// <summary>
        /// Internal list that stores the textureassets.
        /// Use SegmentTextureAssets to get the data stored here.
        /// </summary>
        private List<Asset<Texture2D>> internalTexAssets = new List<Asset<Texture2D>>();
        /// <summary>
        /// The textures for each glow type of this worm. Works like getting a texture from TextureAssets
        /// </summary>
        public List<Asset<Texture2D>> GlowTextureAssets
        {
            get
            {
                if (internalGlowAssets.Count == 0)
                    for (var i = 0; i < GlowTextures.Count; i++)
                    {
                        if (GlowTextures[i] is not null)
                            internalGlowAssets.Add(ModContent.Request<Texture2D>(GlowTextures[i]));
                        else internalGlowAssets.Add(ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj"));
                    }
                return internalGlowAssets;
            }
        }

        /// <summary>
        /// Internal list that stores the glow textureassets.
        /// Use SegmentTextureAssets to get the data stored here.
        /// </summary>
        private List<Asset<Texture2D>> internalGlowAssets = new List<Asset<Texture2D>>();

        public List<BaseWormSegment> Segments = new();

        /// <summary>
        /// Which type of segment following logic this worm should currently use
        /// </summary>
        public SegmentFollowLogic SegmentFollowType;

        /// <summary>
        /// How rigid the segment should be when using default segment logic
        /// </summary>
        public float SegmentRigidity = 0.2f;

        /// <summary>
        /// The max rotational offset from the direction of the previous segment
        /// </summary>
        public float SegmentMaxRotation = MathHelper.TwoPi;

        /// <summary>
        /// The points used by ExactSegmentLogic to exactly follow the head
        /// </summary>
        private List<Vector2> segmentPoints = new List<Vector2>();

        /// <summary>
        /// Updates the positions of the segments based on the value set in SegmentFollowType
        /// </summary>
        public void UpdateSegments()
        {
            Projectile.position += Projectile.velocity; //Update this segment's movement so that all other segments use this as the base. This is undone at the end of the method.
            if (ActiveAnimation is not null)
            {
                ActiveAnimation.ApplyAnimationFrame(Projectile, AnimationFrame);
                AnimationFrame++;
                if (AnimationFrame > ActiveAnimation.AnimationKeyframes.Keys.Max())
                {
                    AnimationFrame = 0;
                    ActiveAnimation = null;
                }
            }
            else
            {
                if (segmentPoints.Count < 1 || segmentPoints[0].Distance(Projectile.Center) > 8)
                {
                    segmentPoints.Insert(0, Projectile.Center);
                }
                while (segmentPoints.Count > 300)
                {
                    segmentPoints.RemoveAt(segmentPoints.Count - 1);
                }
                switch (SegmentFollowType)
                {
                    case (SegmentFollowLogic.Regular):
                        RegularSegmentLogic();
                        break;
                    case (SegmentFollowLogic.Exact):
                        ExactSegmentLogic();
                        break;
                }
            }
            Projectile.position -= Projectile.velocity;

        }

        private void RegularSegmentLogic()
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                float segmentDistance = SegmentTypePositionOffsets[0];
                var thisSeg = Segments[i];
                var aheadSeg = new BaseWormSegment(this);
                if (i != 0)
                {
                    aheadSeg = Segments[i - 1];
                    segmentDistance = SegmentTypePositionOffsets[Segments[i - 1].segmentType + 1];
                }
                segmentDistance *= Projectile.scale;
                Vector2 nexSegDir = aheadSeg.Center - thisSeg.Center;
                if (aheadSeg.rotation != thisSeg.rotation)
                {
                    nexSegDir = nexSegDir.RotatedBy(MathHelper.WrapAngle(aheadSeg.rotation - thisSeg.rotation) * SegmentRigidity);
                    nexSegDir = nexSegDir.MoveTowards((aheadSeg.rotation - thisSeg.rotation).ToRotationVector2(), 1f);
                }
                thisSeg.rotation = nexSegDir.ToRotation() + MathHelper.PiOver2;
                float angledif = MathHelper.WrapAngle(thisSeg.rotation - aheadSeg.rotation);
                thisSeg.rotation = thisSeg.rotation.AngleLerp(aheadSeg.rotation + MathHelper.Clamp(angledif, -SegmentMaxRotation * 0.5f, SegmentMaxRotation * 0.5f), 0.25f);
                thisSeg.Center = aheadSeg.Center - (thisSeg.rotation - MathHelper.PiOver2).ToRotationVector2() * segmentDistance;

            }
        }

        private void ExactSegmentLogic()
        {
            float dist = 40f;
            int segmentPointInUse = 0;
            for (int i = 0; i < Segments.Count; i++)
            {
                var thisSeg = Segments[i];
                var aheadSeg = new BaseWormSegment(this);
                if (i != 0)
                    aheadSeg = Segments[i - 1];
                bool hasMoved = false;
                while (segmentPointInUse < segmentPoints.Count)
                {
                    if (segmentPointInUse == 0)
                    {
                        if (aheadSeg.Center.Distance(segmentPoints[0]) >= dist)
                        {
                            thisSeg.Center = aheadSeg.Center + aheadSeg.Center.DirectionTo(segmentPoints[0]) * dist;
                            Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                            Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                            hasMoved = true;
                            break;
                        }
                        else
                        {
                            segmentPointInUse++;
                        }
                    }
                    else
                    {
                        if (aheadSeg.Center.Distance(segmentPoints[segmentPointInUse]) >= dist)
                        {
                            thisSeg.Center = aheadSeg.Center + aheadSeg.Center.DirectionTo(segmentPoints[segmentPointInUse]) * dist;
                            Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                            Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                            hasMoved = true;
                            break;
                        }
                        else
                        {
                            segmentPointInUse++;
                        }
                    }
                }
                if (!hasMoved && segmentPointInUse >= segmentPoints.Count)
                {
                    thisSeg.Center = segmentPoints[segmentPoints.Count - 1];
                    Segments[i].velocity = Segments[i].Center.DirectionTo(aheadSeg.Center);
                    Segments[i].rotation = Segments[i].velocity.ToRotation() + MathHelper.PiOver2;
                    hasMoved = true;
                    break;
                }
            }
        }
        #endregion

        #region Defaults
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600; //100 tiles offscreen
        }
        public override void SetDefaults()
        {
            for (var i = 0; i < SegmentCount - 1; i++)
            {
                Segments.Add(new BaseWormSegment(this, 0));
            }
            Segments.Add(new BaseWormSegment(this, 1));
        }
        #endregion

        #region Draw
        public override bool PreDraw(ref Color lightColor)
        {

            for (int i = Segments.Count - 1; i >= 0; i--)
            {
                DrawSegment(ref lightColor, Segments[i]);
            }
            Main.spriteBatch.Draw(TextureAssets.Projectile[Type].Value, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, TextureAssets.Projectile[Type].Value.Size() / 2, Projectile.scale, SpriteEffects.None, 1);

            if (GlowTextures.Count > 0 && GlowTextures[0] is not null)
                Main.spriteBatch.Draw(GlowTextureAssets[0].Value, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, GlowTextureAssets[0].Size() / 2, Projectile.scale, SpriteEffects.None, 1);
            return false;
        }

        public virtual void DrawSegment(ref Color lightColor, BaseWormSegment segment)
        {
            var color = Lighting.GetColor(segment.Center.ToTileCoordinates());
            if (!SegmentTextureAssets.IndexInRange(segment.segmentType))
            {
                return;
            }
            var tex = SegmentTextureAssets[segment.segmentType].Value;
            Main.spriteBatch.Draw(tex, segment.Center - Main.screenPosition, null, color * segment.Opacity, segment.rotation, tex.Size() / 2 + (SegmentTypeDrawOffsets[segment.segmentType]), Projectile.scale, SpriteEffects.None, 1);
            if (GlowTextures is null || !GlowTextures.IndexInRange(segment.segmentType + 1) || GlowTextures[segment.segmentType + 1] is null)
            {
                return;
            }
            tex = GlowTextureAssets[segment.segmentType + 1].Value;
            Main.spriteBatch.Draw(tex, segment.Center - Main.screenPosition, null, Color.White * segment.Opacity, segment.rotation, tex.Size() / 2 + (SegmentTypeDrawOffsets[segment.segmentType]), Projectile.scale, SpriteEffects.None, 1);
        }
        #endregion

        #region Netcode

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AnimationFrame);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AnimationFrame = reader.ReadSingle();
        }
        #endregion

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (var i = 0; i < Segments.Count; i++)
            {
                var prevSeg = new BaseWormSegment(this);
                if (i != 0)
                {
                    prevSeg = Segments[i - 1];
                }
                float cpoint = 0;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), prevSeg.Center, Segments[i].Center, 16, ref cpoint))
                {
                    return true;
                }
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
    }
}
