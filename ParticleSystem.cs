using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static RootsCore.ParticleSystem;

namespace RootsCore
{
    public class ParticlePresets
    {

        public static ParticleBehaviorPreset LinearShrink { get; } = new()
        {
            UpdateLogic = (particle) =>
                particle.Scale = new Vector2(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseScale
        };
        public static ParticleBehaviorPreset LinearFade { get; } = new()
        {
            UpdateLogic = (particle) =>
                particle.Opacity = (1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity
        };
        public static ParticleBehaviorPreset LinearShrinkAndFade { get; } = new()
        {
            UpdateLogic = (particle) =>
            {
                particle.Scale = new Vector2(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseScale;
                particle.Opacity = (1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity;
            }
        };
        public static ParticleBehaviorPreset ExplodeAndFade { get; } = new()
        {
            UpdateLogic = (particle) =>
            {
                particle.Scale = new Vector2(RootsCoreUtils.Ease.OutExpo(particle.TimeAlive / (float)particle.MaxLifetime)) * particle.BaseScale;
                particle.Opacity = RootsCoreUtils.Ease.OutBack(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity;
            }
        };
    }

    public class ParticleSystem : ModSystem
    {
        #region Classes
        public class ParticleDrawBatch(bool pixelate, Action applyEffectsToDraw = null)
        {
            public bool Pixelate = pixelate;

            /// <summary>
            /// Runs this action in an isolated spritebatch before drawing the rendertarget
            /// Use to apply shaders to the rendertarget's drawing
            /// </summary>
            public Action ApplyEffectsToDraw = applyEffectsToDraw;

            /// <summary>
            /// Provide a custom draw action instead of the default drawing of the render target<br/>
            /// This can be used to apply effects to the render target<br/>
            /// Prevents ApplyEffectsToDraw from being called when set
            /// </summary>
            public Action CustomDrawAction;
            public RenderTarget2D BatchTarget = null; //TODO - render target pooling
            public void Unload()
            {
                Main.QueueMainThreadAction(() =>
                {
                    if (!BatchTarget.IsDisposed)
                        BatchTarget.Dispose();
                    BatchTarget = null;
                });
            }

            public void VerifyRenderTarget()
            {
                BatchesWithLoadedTargets.Add(this);
                int width = Pixelate ? Main.screenWidth / 2 : Main.screenWidth;
                int height = Pixelate ? Main.screenHeight / 2 : Main.screenHeight;

                if (BatchTarget is null)
                {
                    BatchTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    return;
                }

                if (BatchTarget.Width == width && BatchTarget.Height == height)
                    return;

                if (!BatchTarget.IsDisposed)
                    BatchTarget.Dispose();
                BatchTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }
        public class ParticleBehaviorPreset
        {
            public Vector2? Velocity;
            public float? Rotation;
            public Vector2? Scale;
            public Action<Particle> UpdateLogic { get; set; } = null;
            public Action<Particle> CustomDrawLogic { get; set; } = null;
            public Color? Color;
            public float? Opacity;
            public int? MaxLifetime;
            public bool? UseTileLighting;
            public DrawLayerSystem.DrawLayer? DrawLayer;
            public ParticleDrawBatch DrawBatch;

            public void ApplyToParticle(Particle particle)
            {
                particle.Velocity = Velocity ?? particle.Velocity;
                particle.Rotation = Rotation ?? particle.Rotation;
                particle.Scale = Scale ?? particle.Scale;
                particle.UpdateLogic = UpdateLogic ?? particle.UpdateLogic;
                particle.CustomDrawLogic = CustomDrawLogic ?? particle.CustomDrawLogic;
                particle.Color = Color ?? particle.Color;
                particle.Opacity = Opacity ?? particle.Opacity;
                particle.MaxLifetime = MaxLifetime ?? particle.MaxLifetime;
                particle.UseTileLighting = UseTileLighting ?? particle.UseTileLighting;
                particle.DrawLayer = DrawLayer ?? particle.DrawLayer;
                particle.DrawBatch = DrawBatch ?? particle.DrawBatch;
            }
        }
        public class Particle
        {
            #region Constructors
            public Particle(Asset<Texture2D> texture, Vector2 position, int lifetime)
            {
                Texture = texture;
                Frame = texture.Frame();
                Origin = texture.Size() * 0.5f;
                Position = position;
                MaxLifetime = lifetime;
            }
            public Particle(Asset<Texture2D> texture, Vector2 position, int lifetime, ParticleBehaviorPreset particleBehavior)
            {
                Texture = texture;
                Frame = texture.Frame();
                Origin = texture.Size() * 0.5f;
                Position = position;
                MaxLifetime = lifetime;

                particleBehavior.ApplyToParticle(this);
            }
            #endregion

            public Vector2 Position;
            public Vector2 Velocity = Vector2.Zero;
            public float Rotation;
            /// <summary>
            /// Defaults to <c>Color.White with { A = 0 }</c><br/>
            /// This draws it as an additive white
            /// </summary>
            public Color Color = Color.White with { A = 0 };
            public float Opacity = 1f;
            public Asset<Texture2D> Texture { get; set; }
            public Rectangle Frame { get; set; }
            public Vector2 Origin { get; set; }
            public Vector2 Scale = new(1);
            public Vector2 BaseScale = new(1);
            public float BaseOpacity = 1f;
            public int MaxLifetime;
            public int TimeAlive;
            public bool UseTileLighting = false;
            public Action<Particle> UpdateLogic { get; set; }
            public Action<Particle> CustomDrawLogic { get; set; }
            public DrawLayerSystem.DrawLayer DrawLayer = DrawLayerSystem.DrawLayer.AfterDusts;

            /// <summary>
            /// Defaults to ParticleSystem.DrawPixelated
            /// Set to null to disable pixelization, or set to a custom batch if you know what you're doing
            /// </summary>
            public ParticleDrawBatch DrawBatch = DrawPixelated;
        }
        #endregion
        public static ParticleDrawBatch DrawPixelated { get; } = new(true);
        public static ParticleDrawBatch DrawDefault { get; } = new(false);
        public static readonly List<Particle> Particles = [];
        public static readonly List<ParticleDrawBatch> BatchesWithLoadedTargets = [];
        public static readonly Dictionary<DrawLayerSystem.DrawLayer, Dictionary<ParticleDrawBatch, List<Particle>>> BatchedDrawList = [];
        public static void SpawnParticle(Particle particle)
        {
            Particles.Add(particle);
            if (!BatchedDrawList.ContainsKey(particle.DrawLayer))
                BatchedDrawList[particle.DrawLayer] = [];
            if (!BatchedDrawList[particle.DrawLayer].ContainsKey(particle.DrawBatch ?? DrawDefault))
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault] = [];
            BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault].Add(particle);
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
                KillParticle(0);
        }
        public static void KillParticle(Particle particle)
        {
            Particles.Remove(particle);
            try
            {
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? DrawDefault);
                }
            }
            catch
            {
                BatchedDrawList.Clear();
            }
        }
        public static void KillParticle(int index)
        {
            var particle = Particles[0];
            Particles.RemoveAt(0);
            try
            {
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? DrawDefault].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? DrawDefault);
                }
            }
            catch
            {
                BatchedDrawList.Clear();
            }
        }
        public override void Load()
        {
            DrawLayerSystem.DrawToLayer += DrawParticles;
            Main.QueueMainThreadAction(() => new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents));
        }
        public override void Unload()
        {
            for (int i = 0; i < BatchesWithLoadedTargets.Count; i++)
            {
                BatchesWithLoadedTargets[i].Unload();
                BatchesWithLoadedTargets.RemoveAt(i);
                i--;
            }
        }
        public override void PostUpdateDusts()
        {
            if (Particles.Count == 0)
            {
                BatchedDrawList.Clear();
                return;
            }
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
                KillParticle(0);

            for (var i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                if (particle.TimeAlive >= particle.MaxLifetime)
                {
                    KillParticle(particle);
                    i--;
                    continue;
                }
                if (particle.TimeAlive == 0)
                {
                    particle.BaseScale = particle.Scale;
                    particle.BaseOpacity = particle.Opacity;
                }
                particle.UpdateLogic?.Invoke(particle);
                particle.Position += particle.Velocity;
                particle.TimeAlive++;
            }
            lightingCache.Clear();
        }
        public static void DrawParticles(DrawLayerSystem.DrawLayer layer)
        {
            if (!BatchedDrawList.ContainsKey(layer))
                return;

            if (RootsCoreConfig.Instance.DisableParticleSpecialEffects)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
                for (int i = 0; i < Particles.Count; i++)
                    if (Particles[i].DrawLayer == layer)
                        DrawParticle(Particles[i]);
                Main.spriteBatch.End();
                return;
            }

            var layerBatch = BatchedDrawList[layer];
            var graphicsDevice = Main.spriteBatch.GraphicsDevice;
            var halfSizeMatrix = Matrix.CreateScale(0.5f, 0.5f, 1.0f);
            var rtbindings = graphicsDevice.GetRenderTargets();

            foreach (var batchKVP in layerBatch)
            {
                var batchData = batchKVP.Key;
                var batchParticles = batchKVP.Value;
                batchData.VerifyRenderTarget();
                graphicsDevice.SetRenderTarget(batchData.BatchTarget);
                graphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, batchData.Pixelate ? halfSizeMatrix : Matrix.Identity);
                for (int i = 0; i < batchParticles.Count; i++)
                    DrawParticle(batchParticles[i]);
                Main.spriteBatch.End();
            }
            foreach (var binding in rtbindings)
            {
                if (binding.RenderTarget is not RenderTarget2D rt)
                    continue;
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            }

            graphicsDevice.SetRenderTargets(rtbindings);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
            foreach (var batchKVP in layerBatch)
            {
                var batch = batchKVP.Key;
                if (batch.CustomDrawAction is not null)
                {
                    batch.CustomDrawAction();
                    continue;
                }
                if (batch.ApplyEffectsToDraw is null)
                {
                    Main.spriteBatch.Draw(batch.BatchTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, batch.Pixelate ? 2 : 1, 0, 0);
                    continue;
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
                batch.ApplyEffectsToDraw();
                Main.spriteBatch.Draw(batch.BatchTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, batch.Pixelate ? 2 : 1, 0, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
            }
            Main.spriteBatch.End();
        }
        private static void DrawParticle(Particle particle)
        {
            if (particle.CustomDrawLogic is not null)
            {
                particle.CustomDrawLogic(particle);
                return;
            }
            Color drawColor = particle.Color;
            if (particle.UseTileLighting)
            {
                drawColor = ApplyLightingColorToColor(particle.Position.ToTileCoordinates(), drawColor);
            }
            Main.spriteBatch.Draw(particle.Texture.Value, particle.Position - Main.screenPosition, particle.Frame, drawColor * particle.Opacity, particle.Rotation, particle.Origin, particle.Scale, 0, 0);
        }
        public static Color ApplyLightingColorToColor(Vector2 point, Color color) => ApplyLightingColorToColor(point.ToTileCoordinates(), color);
        public static Color ApplyLightingColorToColor(Point point, Color color)
        {
            if (RootsCoreConfig.Instance.DisableParticleTileLighting)
                return color;
            Color lightColor = GetCachedLightingColor(point);
            return Color.FromNonPremultiplied(color.ToVector4() * lightColor.ToVector4());
        }
        private static Dictionary<Point, Color> lightingCache = [];
        public static Color GetCachedLightingColor(Point point)
        {
            if (lightingCache.TryGetValue(point, out Color color))
                return color;
            color = Lighting.GetColor(point);
            lightingCache[point] = color;
            return color;
        }
    }
}
