using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using static RootsCore.ParticleSystem.ParticleSystem;

namespace RootsCore.ParticleSystem
{

    public class ParticleDrawBatch(bool pixelate, Action applyEffectsToDraw = null)
    {

        public static ParticleDrawBatch DrawPixelated { get; } = new(true);
        public static ParticleDrawBatch DrawDefault { get; } = new(false);

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
        public Particle(Asset<Texture2D> texture, Vector2 position, int lifetime, ParticlePreset particleBehavior)
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
        /// Defaults to ParticleDrawBatch.DrawPixelated
        /// Set to null to disable pixelization, or set to a custom batch if you know what you're doing
        /// </summary>
        public ParticleDrawBatch DrawBatch = ParticleDrawBatch.DrawPixelated;
    }

}
