using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Security.Principal;
using Terraria;
using Terraria.ModLoader;

namespace RootsCore.ParticleSystem
{
    public class ParticleSystem : ModSystem
    {
        #region Loading & Unloading
        public override void Load()
        {
            DrawLayerSystem.DrawToLayer += DrawParticles;
        }
        public override void Unload()
        {
            while (_nonPixelatedRTPool.Count > 0)
                _nonPixelatedRTPool.Pop().Dispose();

            while (_pixelatedRTPool.Count > 0)
                _pixelatedRTPool.Pop().Dispose();
            while (_rentedRTs.Count > 0)
            {
                _rentedRTs[0].Dispose();
                _rentedRTs.RemoveAt(0);
            }

        }
        #endregion

        #region Particle Pool Management

        public static readonly List<Particle> Particles = [];
        public static void SpawnParticle(Particle particle)
        {
            Particles.Add(particle);
            if (!BatchedDrawList.ContainsKey(particle.DrawLayer))
                BatchedDrawList[particle.DrawLayer] = [];
            if (!BatchedDrawList[particle.DrawLayer].ContainsKey(particle.DrawBatch ?? ParticleDrawBatch.DrawDefault))
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault] = [];
            BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault].Add(particle);
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
                KillParticle(0);
        }
        public static void KillParticle(Particle particle)
        {
            Particles.Remove(particle);
            try
            {
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? ParticleDrawBatch.DrawDefault);
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
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawDefault].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? ParticleDrawBatch.DrawDefault);
                }
            }
            catch
            {
                BatchedDrawList.Clear();
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
            _lightingCache.Clear();
        }
        #endregion

        #region Render Target Management

        private static readonly Stack<RenderTarget2D> _nonPixelatedRTPool = new();
        private static readonly Stack<RenderTarget2D> _pixelatedRTPool = new();
        private static readonly List<RenderTarget2D> _rentedRTs = [];

        public static RenderTarget2D RentParticleTarget(bool pixelated)
        {
            var pool = pixelated ? _pixelatedRTPool : _nonPixelatedRTPool;

            int width = pixelated ? Main.screenWidth / 2 : Main.screenWidth;
            int height = pixelated ? Main.screenHeight / 2 : Main.screenHeight;

            if (!pool.TryPop(out RenderTarget2D target))
                target = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            if (target.Width != width || target.Height != height)
            {
                if (!target.IsDisposed)
                    target.Dispose();
                target = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }
            _rentedRTs.Add(target);
            return target;
        }

        public static void ReturnParticleTarget(RenderTarget2D target, bool pixelated)
        {
            var pool = pixelated ? _pixelatedRTPool : _nonPixelatedRTPool;
            pool.Push(target);
            _rentedRTs.Remove(target);
        }
        #endregion

        #region Drawing
        private static readonly Dictionary<Point, Color> _lightingCache = [];
        public static readonly Dictionary<DrawLayerSystem.DrawLayer, Dictionary<ParticleDrawBatch, List<Particle>>> BatchedDrawList = [];
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
                batchData.BatchTarget = RentParticleTarget(batchData.Pixelate);
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
                    ReturnParticleTarget(batch.BatchTarget, batch.Pixelate);
                    batch.BatchTarget = null;
                    continue;
                }
                if (batch.ApplyEffectsToDraw is null)
                {
                    Main.spriteBatch.Draw(batch.BatchTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, batch.Pixelate ? 2 : 1, 0, 0);
                    ReturnParticleTarget(batch.BatchTarget, batch.Pixelate);
                    batch.BatchTarget = null;
                    continue;
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
                batch.ApplyEffectsToDraw();
                Main.spriteBatch.Draw(batch.BatchTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, batch.Pixelate ? 2 : 1, 0, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.Transform);
                ReturnParticleTarget(batch.BatchTarget, batch.Pixelate);
                batch.BatchTarget = null;
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
        #endregion

        #region Helper Functions
        public static Color ApplyLightingColorToColor(Vector2 point, Color color) => ApplyLightingColorToColor(point.ToTileCoordinates(), color);
        public static Color ApplyLightingColorToColor(Point point, Color color)
        {
            if (RootsCoreConfig.Instance.DisableParticleTileLighting)
                return color;
            Color lightColor = GetCachedLightingColor(point);
            return Color.FromNonPremultiplied(color.ToVector4() * lightColor.ToVector4());
        }
        public static Color GetCachedLightingColor(Point point)
        {
            if (_lightingCache.TryGetValue(point, out Color color))
                return color;
            color = Lighting.GetColor(point);
            _lightingCache[point] = color;
            return color;
        }
        #endregion
    }
}
