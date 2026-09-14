using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public class ParticleBehaviorPreset
        {
            public Vector2? Velocity;
            public float? Rotation;
            public Vector2? Scale;
            public Action<Particle> UpdateLogic { get; set; } = null;
            public Action<Particle> CustomDrawLogic { get; set; } = null;
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

                Velocity = particleBehavior.Velocity ?? Velocity;
                Rotation = particleBehavior.Rotation ?? Rotation;
                Scale = particleBehavior.Scale ?? Scale;
                UpdateLogic = particleBehavior.UpdateLogic ?? UpdateLogic;
                CustomDrawLogic = particleBehavior.CustomDrawLogic ?? CustomDrawLogic;
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
            public bool Pixelate = true;
            public bool UseTileLighting = false;
            public Action<Particle> UpdateLogic { get; set; }
            public Action<Particle> CustomDrawLogic { get; set; }
            public DrawLayerSystem.DrawLayer DrawLayer = DrawLayerSystem.DrawLayer.AfterDusts;
        }
        public static readonly List<Particle> Particles = [];
        private static RenderTarget2D PixelizationTarget;

        public static void SpawnParticle(Particle particle)
        {
            Particles.Add(particle);
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
            {
                Particles.RemoveAt(0);
            }
        }
        public override void Load()
        {
            DrawLayerSystem.DrawToLayer += DrawParticles;
            Main.QueueMainThreadAction(() => new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth / 2, Main.screenHeight / 2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents));

        }

        public override void Unload()
        {
            if (!PixelizationTarget.IsDisposed)
                PixelizationTarget.Dispose();
            PixelizationTarget = null;
        }

        public override void PostUpdateDusts()
        {
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
            {
                Particles.RemoveAt(0);
            }

            for (var i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                if (particle.TimeAlive >= particle.MaxLifetime)
                {
                    Particles.Remove(particle);
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
        }

        public static void DrawParticles(DrawLayerSystem.DrawLayer layer)
        {
            if (Particles.All(x => x.DrawLayer != layer))
                return;
            EnsureRenderTargetSize();
            var graphicsDevice = Main.spriteBatch.GraphicsDevice;
            var translationMatrix = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0);
            var halfSizeMatrix = Matrix.CreateScale(0.5f, 0.5f, 1.0f);


            var rtbindings = graphicsDevice.GetRenderTargets();
            graphicsDevice.SetRenderTarget(PixelizationTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, halfSizeMatrix);
            for (int i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                if (layer != particle.DrawLayer || !particle.Pixelate) continue;
                DrawParticle(particle);
            }

            Main.spriteBatch.End();
            foreach (var binding in rtbindings)
            {
                if (binding.RenderTarget is not RenderTarget2D rt)
                    continue;

                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            }

            graphicsDevice.SetRenderTargets(rtbindings);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
            for (int i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                if (layer != particle.DrawLayer || particle.Pixelate) continue;
                DrawParticle(particle);
            }
            Main.spriteBatch.Draw(PixelizationTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2, 0, 0);
            Main.spriteBatch.End();


        }
        private static void DrawParticle(Particle particle)
        {
            if (particle.CustomDrawLogic is not null)
            {
                particle.CustomDrawLogic.Invoke(particle);
                return;
            }

            Color drawColor = particle.Color;
            if (particle.UseTileLighting)
            {
                Color lightColor = Lighting.GetColor(particle.Position.ToTileCoordinates());
                drawColor = Color.FromNonPremultiplied(drawColor.ToVector4() * lightColor.ToVector4());
            }
            Main.spriteBatch.Draw(particle.Texture.Value, particle.Position - Main.screenPosition, particle.Frame, drawColor * particle.Opacity, particle.Rotation, particle.Origin, particle.Scale, 0, 0);
        }

        public static void EnsureRenderTargetSize()
        {
            int width = Main.screenWidth / 2;
            int height = Main.screenHeight / 2;

            if (PixelizationTarget is null)
            {
                PixelizationTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                return;
            }

            if (PixelizationTarget.Width == width && PixelizationTarget.Height == height)
                return;

            if (!PixelizationTarget.IsDisposed)
                PixelizationTarget.Dispose();
            PixelizationTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
    }
}
