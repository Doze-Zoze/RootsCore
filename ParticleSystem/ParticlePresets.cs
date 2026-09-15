using Microsoft.Xna.Framework;
using System;

namespace RootsCore.ParticleSystem
{
    public class ParticlePreset
    {
        #region Presets
        public static ParticlePreset LinearShrink { get; } = new()
        {
            UpdateLogic = (particle) =>
                particle.Scale = new Vector2(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseScale
        };
        public static ParticlePreset LinearFade { get; } = new()
        {
            UpdateLogic = (particle) =>
                particle.Opacity = (1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity
        };
        public static ParticlePreset LinearShrinkAndFade { get; } = new()
        {
            UpdateLogic = (particle) =>
            {
                particle.Scale = new Vector2(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseScale;
                particle.Opacity = (1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity;
            }
        };
        public static ParticlePreset ExplodeAndFade { get; } = new()
        {
            UpdateLogic = (particle) =>
            {
                particle.Scale = new Vector2(RootsCoreUtils.Ease.OutExpo(particle.TimeAlive / (float)particle.MaxLifetime)) * particle.BaseScale;
                particle.Opacity = RootsCoreUtils.Ease.OutBack(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity;
            }
        };
        #endregion
        
        #region Preset Data
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
        #endregion
    }
}
