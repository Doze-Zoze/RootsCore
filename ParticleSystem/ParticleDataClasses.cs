using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using static RootsCore.ParticleSystem.ParticleSystem;

namespace RootsCore.ParticleSystem
{
    /// <summary>
    /// Defines a batch for particles to draw together in a single RenderTarget, allowing applying shaders to multiple particles at once
    /// </summary>
    /// <param name="pixelate"></param>
    /// <param name="applyEffectsToDraw"></param>
    public class ParticleDrawBatch(bool pixelate, Action applyEffectsToDraw = null)
    {
        /// <summary>
        /// Default draw batch for particles, pixelates to Terraria's pixel grid
        /// </summary>
        public static ParticleDrawBatch DrawPixelated { get; } = new(true);

        /// <summary>
        /// Draw batch for drawing particles without pixelization effects
        /// </summary>
        public static ParticleDrawBatch DrawRegular { get; } = new(false);

        /// <summary>
        /// Causes this batch to be pixelated to match Terraria's pixel grid<br/>
        /// This is done by drawing at half size then upscaling w/ PointClamp samplerstate
        /// </summary>
        public bool Pixelate = pixelate;

        /// <summary>
        /// Runs this action in an isolated spritebatch before drawing the rendertarget<br/>
        /// Use to apply shaders to the rendertarget's drawing<br/>
        /// Does not run when <see cref="CustomDrawAction"/> is defined
        /// </summary>
        public Action ApplyEffectsToDraw = applyEffectsToDraw;

        /// <summary>
        /// Provide a custom draw action instead of the default drawing of the render target<br/>
        /// This can be used to apply effects to the render target<br/>
        /// Prevents <see cref="ApplyEffectsToDraw"/> from being called when set
        /// </summary>
        public Action CustomDrawAction; 

        /// <summary>
        /// The render target for this batch.<br/>
        /// This is <see cref="null"/> except during drawing, as the targets are returned to the pool when not in use
        /// </summary>
        public RenderTarget2D BatchTarget { get; internal set; } = null;
    }

    /// <summary>
    /// An individual Particle. Generally, particles should be created via constructors and customized via object initializers.<br/>
    /// Particles should then be added or deleted with <see cref="SpawnParticle"/> and <see cref="KillParticle"/> <br/>
    /// Particle references can also be stored and edited dynamically in other code, such as a projectile moving a particle to it's own position every frame
    /// </summary>
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

        /// <summary>
        /// Position of the particle in world coordinates<br/>
        /// By default, this is the center unless <see cref="Frame"/> is specified
        /// </summary>
        public Vector2 Position;
        /// <summary>
        /// Particle's velocity, in pixels per frame
        /// </summary>
        public Vector2 Velocity = Vector2.Zero;
        /// <summary>
        /// Particle's rotation, in radians
        /// </summary>
        public float Rotation;
        /// <summary>
        /// The color the particle should draw with<br/>
        /// Defaults to <c>Color.White with { A = 0 }</c><br/>
        /// This draws it as an additive white
        /// </summary>
        public Color Color = Color.White with { A = 0 };
        /// <summary>
        /// Particle's opacity, ranging from 1 (opaque) to 0 (transparent)<br/>
        /// <see cref="Color"/> is multiplied by this value when drawing
        /// </summary>
        public float Opacity = 1f;
        /// <summary>
        /// The texture the particle draws with
        /// </summary>
        public Asset<Texture2D> Texture { get; set; }
        /// <summary>
        /// The source rectangle of the texture that the particle should currently draw<br/>
        /// Animated particles should use <see cref="UpdateLogic"/> to change this for their animation
        /// </summary>
        public Rectangle Frame { get; set; }
        /// <summary>
        /// Where the "center" of the drawn texture is<br/>
        /// Defaults to half of the width and height of the texture
        /// </summary>
        public Vector2 Origin { get; set; }
        /// <summary>
        /// Scale multiplier applied to the texture
        /// </summary>
        public Vector2 Scale = new(1);
        /// <summary>
        /// The base scale of the particle when spawned<br/>
        /// This is calculated automatically and should rarely be set
        /// </summary>
        public Vector2 BaseScale = new(1);
        /// <summary>
        /// The base opacity of the particle when spawned<br/>
        /// This is calculated automatically and should rarely be set
        /// </summary>
        public float BaseOpacity = 1f;
        /// <summary>
        /// The maximum lifetime of the particle before it removes itself from the particle list
        /// </summary>
        public int MaxLifetime;
        /// <summary>
        /// How many frames this particle has been alive for
        /// </summary>
        public int TimeAlive;
        /// <summary>
        /// Whether or not this particle should dynamically adjust to tile lighting
        /// </summary>
        /// <remarks>
        /// This should be avoided when not needed as it can be performance intensive.<br/>
        /// Consider spawning the projectile with the lighting color already applied for short lifetime particles.<br/>
        /// This can be done using <see cref="ParticleSystem.ApplyLightingColorToColor"/>
        /// </remarks>
        public bool UseTileLighting = false;
        /// <summary>
        /// Custom update logic that is run every frame for the particle
        /// </summary>
        public Action<Particle> UpdateLogic { get; set; }
        /// <summary>
        /// Custom draw logic to be run instead of default draw logic for the particle.
        /// </summary>
        public Action<Particle> CustomDrawLogic { get; set; }
        /// <summary>
        /// What layer the particle should draw at<br/>
        /// Defaults to <see cref="DrawLayerSystem.DrawLayer.AfterDusts"/>
        /// </summary>
        public DrawLayerSystem.DrawLayer DrawLayer = DrawLayerSystem.DrawLayer.AfterDusts;

        /// <summary>
        /// Which batch the particle should draw in. Can be used for applying effects to entire batches of particles<br/>
        /// Defaults to <see cref="ParticleDrawBatch.DrawPixelated"/><br/>
        /// Set to <see cref="null"/> or <see cref="ParticleDrawBatch.DrawRegular"/> to disable pixelization
        /// </summary>
        public ParticleDrawBatch DrawBatch = ParticleDrawBatch.DrawPixelated;
    }

}
