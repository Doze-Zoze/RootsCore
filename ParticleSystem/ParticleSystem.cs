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

        /// <remarks>
        /// DO NOT MODIFY DIRECTLY.<br/>
        /// use <see cref="SpawnParticle"/> and <see cref="KillParticle"/> to add or remove particles
        /// </remarks>
        /// <summary>
        /// The list of particles to be rendered and updated each frame
        /// </summary>
        public static readonly List<Particle> Particles = [];
        /// <summary>
        /// Adds a particle to <see cref="Particles"/> and <see cref="BatchedDrawList"/>
        /// </summary>
        /// <param name="particle"></param>
        public static void SpawnParticle(Particle particle)
        {
            Particles.Add(particle);
            if (!BatchedDrawList.ContainsKey(particle.DrawLayer))
                BatchedDrawList[particle.DrawLayer] = [];
            if (!BatchedDrawList[particle.DrawLayer].ContainsKey(particle.DrawBatch ?? ParticleDrawBatch.DrawRegular))
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular] = [];
            BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular].Add(particle);
            while (Particles.Count > RootsCoreConfig.Instance.MaximumParticleCount)
                KillParticle(0);
        }
        /// <summary>
        /// Removes a particle to <see cref="Particles"/> and <see cref="BatchedDrawList"/>
        /// </summary>
        /// <param name="particle"></param>
        public static void KillParticle(Particle particle)
        {
            Particles.Remove(particle);
            try
            {
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? ParticleDrawBatch.DrawRegular);
                }
            }
            catch
            {
                BatchedDrawList.Clear();
            }
        }
        /// <summary>
        /// Removes a particle from <see cref="Particles"/> and <see cref="BatchedDrawList"/>
        /// </summary>
        /// <param name="index">The index in <see cref="Particles"/> of the particle to remove</param>
        public static void KillParticle(int index)
        {
            var particle = Particles[index];
            Particles.RemoveAt(index);
            try
            {
                BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular].Remove(particle);
                if (BatchedDrawList[particle.DrawLayer][particle.DrawBatch ?? ParticleDrawBatch.DrawRegular].Count == 0)
                {
                    BatchedDrawList[particle.DrawLayer].Remove(particle.DrawBatch ?? ParticleDrawBatch.DrawRegular);
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

        internal static RenderTarget2D RentParticleTarget(bool pixelated)
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

        internal static void ReturnParticleTarget(RenderTarget2D target, bool pixelated)
        {
            var pool = pixelated ? _pixelatedRTPool : _nonPixelatedRTPool;
            pool.Push(target);
            _rentedRTs.Remove(target);
        }
        #endregion

        #region Drawing
        private static readonly Dictionary<Point, Color> _lightingCache = [];
        internal static readonly Dictionary<DrawLayerSystem.DrawLayer, Dictionary<ParticleDrawBatch, List<Particle>>> BatchedDrawList = [];
        public static void DrawParticles(DrawLayerSystem.DrawLayer layer)
        {
            if (!BatchedDrawList.ContainsKey(layer))
                return;

            //This draws all particles directly to screen, bypassing all rendertarget effects
            //This means no pixelization or shaders that apply to batches apply, reducing workload
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
            var rtbindings = graphicsDevice.GetRenderTargets(); //Stores current RTs to reapply later

            //Here, for all batches drawing on the current layer, we
            // - Rent batch render targets as needed
            // - Draw that batch's particles to the render target
            foreach (var batchKVP in layerBatch)
            {
                var batchData = batchKVP.Key;
                var batchParticles = batchKVP.Value;
                batchData.BatchTarget = RentParticleTarget(batchData.Pixelate);
                graphicsDevice.SetRenderTarget(batchData.BatchTarget);
                graphicsDevice.Clear(Color.Transparent);

                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, batchData.Pixelate ? halfSizeMatrix : Matrix.Identity);
                for (int i = 0; i < batchParticles.Count; i++)
                    DrawParticle(batchParticles[i]);
                Main.spriteBatch.End();
            }
            //We need to ensure that rendertargets that were in use before we switched the targets preserve their contents when reapplied
            //We then re-apply them
            foreach (var binding in rtbindings)
            {
                if (binding.RenderTarget is not RenderTarget2D rt)
                    continue;
                rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            }
            graphicsDevice.SetRenderTargets(rtbindings);
            
            //Now we draw each batch's RenderTarget back to the screen, then return that batch's target to the pool as it is no longer needed
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

                // - Begin isolated SpriteBatch so that shader effects can be applied safely
                // - Apply those through ApplyEffectsToDraw() and draw the batch target
                // - Reset the spritebatch so shaders don't apply again & return the batch target
                // Potential optimization: Loop all layers that need this code in order before resetting to base spritebatch to reduce spritebatch resets
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
        /// <summary>
        /// Applies lighting color at the given world postion to the the given color
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <returns>Input color adjusted for tile lighting</returns>
        public static Color ApplyLightingColorToColor(Vector2 point, Color color) => ApplyLightingColorToColor(point.ToTileCoordinates(), color);
        /// <summary>
        /// Applies lighting color at the given tile postion to the the given color
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <returns>Input color adjusted for tile lighting</returns>
        public static Color ApplyLightingColorToColor(Point point, Color color)
        {
            if (RootsCoreConfig.Instance.DisableParticleTileLighting)
                return color;
            Color lightColor = GetCachedLightingColor(point);
            return Color.FromNonPremultiplied(color.ToVector4() * lightColor.ToVector4());
        }
        /// <summary>
        /// Gets tile light at given world postion and caches that tile's color for the rest of the update<br/>
        /// Causes many calls to the same tile for dense effects to be much faster
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color"></param>
        /// <returns>Input color adjusted for tile lighting</returns>
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
