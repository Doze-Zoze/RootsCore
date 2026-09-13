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
    public class ParticlePresets {

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
            UpdateLogic = (particle) => {
                particle.Scale = new Vector2(1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseScale;
                particle.Opacity = (1 - particle.TimeAlive / (float)particle.MaxLifetime) * particle.BaseOpacity;
                }
        };
        public static ParticleBehaviorPreset ExplodeAndFade { get; } = new()
        {
            UpdateLogic = (particle) => {
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
            /// Defaults to <code>Color.White with { A = 0 }</code>
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
        }
        public static List<Particle> Particles = [];

        //TODO - Config for the size of the cap
        public const int MaxParticles = 1000;

        public static void SpawnParticle(Particle particle) {
            Particles.Add(particle);
            while (Particles.Count > MaxParticles)
            {
                Particles.RemoveAt(0);
            }
        }
        public override void Load()
        {
            DrawLayerSystem.DrawToLayer += DrawParticles;
        }

        public override void PostUpdateDusts()
        {
            while (Particles.Count > MaxParticles)
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
                if (particle.TimeAlive == 0) {
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
            Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,null,null,null,null,Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                if (layer != particle.DrawLayer) continue;

                if (particle.CustomDrawLogic is not null)
                {
                    particle.CustomDrawLogic.Invoke(particle);
                    continue;
                }

                Color drawColor = particle.Color;
                if (particle.UseTileLighting)
                {
                    Color lightColor = Lighting.GetColor(particle.Position.ToTileCoordinates());
                    drawColor = Color.FromNonPremultiplied(drawColor.ToVector4() * lightColor.ToVector4());
                }
                Main.EntitySpriteDraw(particle.Texture.Value, particle.Position - Main.screenPosition, particle.Frame, drawColor * particle.Opacity, particle.Rotation, particle.Origin, particle.Scale, default);
            }
            Main.spriteBatch.End();

        }
    }
}
