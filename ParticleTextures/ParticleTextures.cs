using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace RootsCore.ParticleTextures;
public class ParticleTextures
{
    public static class Opaque
    {
        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Circle => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/circle_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/circle_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/circle_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/circle_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/circle_05")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Dirt => field ??= 
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/dirt_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/dirt_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/dirt_03")
        ];

        /// <summary>Contains 2 textures</summary>
        public static Asset<Texture2D>[] Fire => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/fire_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/fire_02")
        ];

        /// <summary>Contains 6 textures</summary>
        public static Asset<Texture2D>[] Flame => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flame_06")
        ];
        public static Asset<Texture2D> Flare => field ??= ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/flare_01");

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Light => field ??= 
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/light_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/light_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/light_03")
        ];

        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Magic => field ??= 
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/magic_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/magic_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/magic_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/magic_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/magic_05")
        ];

        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Muzzle => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/muzzle_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/muzzle_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/muzzle_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/muzzle_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/muzzle_05")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Scorch => field ??= 
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/scorch_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/scorch_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/scorch_03")
         ];

        public static Asset<Texture2D> Scratch => field ??= ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/scratch_01");

        /// <summary>Contains 4 textures</summary>
        public static Asset<Texture2D>[] Slash => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/slash_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/slash_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/slash_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/slash_04")
        ];

        /// <summary>Contains 10 textures</summary>
        public static Asset<Texture2D>[] Smoke => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_07"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_08"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_09"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/smoke_10")
        ];

        /// <summary>Contains 7 textures</summary>
        public static Asset<Texture2D>[] Spark => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/spark_07")
        ];

        /// <summary>Contains 9 textures</summary>
        public static Asset<Texture2D>[] Star => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_07"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_08"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/star_09")
        ];

        /// <summary>Contains 2 textures</summary>
        public static Asset<Texture2D>[] Symbol => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/symbol_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/symbol_02")
        ];

        /// <summary>Contains 7 textures</summary>
        public static Asset<Texture2D>[] Trace => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/trace_07")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Twirl => field ??= 
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/twirl_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/twirl_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/twirl_03")
        ];

        /// <summary>Contains 4 textures</summary>
        public static Asset<Texture2D>[] Window => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/window_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/window_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/window_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Opaque/window_04")
        ];
    }

    public static class Transparent
    {
        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Circle => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/circle_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/circle_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/circle_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/circle_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/circle_05")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Dirt => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/dirt_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/dirt_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/dirt_03")
        ];

        /// <summary>Contains 2 textures</summary>
        public static Asset<Texture2D>[] Fire => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/fire_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/fire_02")
        ];

        /// <summary>Contains 6 textures</summary>
        public static Asset<Texture2D>[] Flame => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flame_06")
        ];
        public static Asset<Texture2D> Flare => field ??= ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/flare_01");

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Light => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/light_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/light_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/light_03")
        ];

        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Magic => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/magic_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/magic_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/magic_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/magic_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/magic_05")
        ];


        /// <summary>Contains 5 textures</summary>
        public static Asset<Texture2D>[] Muzzle => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/muzzle_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/muzzle_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/muzzle_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/muzzle_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/muzzle_05")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Scorch => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/scorch_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/scorch_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/scorch_03")
         ];

        public static Asset<Texture2D> Scratch => field ??= ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/scratch_01");

        /// <summary>Contains 4 textures</summary>
        public static Asset<Texture2D>[] Slash => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/slash_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/slash_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/slash_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/slash_04")
        ];

        /// <summary>Contains 10 textures</summary>
        public static Asset<Texture2D>[] Smoke => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_07"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_08"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_09"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/smoke_10")
        ];

        /// <summary>Contains 7 textures</summary>
        public static Asset<Texture2D>[] Spark => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/spark_07")
        ];

        /// <summary>Contains 9 textures</summary>
        public static Asset<Texture2D>[] Star => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_07"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_08"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/star_09")
        ];

        /// <summary>Contains 2 textures</summary>
        public static Asset<Texture2D>[] Symbol => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/symbol_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/symbol_02")
        ];

        /// <summary>Contains 7 textures</summary>
        public static Asset<Texture2D>[] Trace => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_04"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_05"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_06"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/trace_07")
        ];

        /// <summary>Contains 3 textures</summary>
        public static Asset<Texture2D>[] Twirl => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/twirl_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/twirl_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/twirl_03")
        ];

        /// <summary>Contains 4 textures</summary>
        public static Asset<Texture2D>[] Window => field ??=
        [
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/window_01"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/window_02"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/window_03"),
            ModContent.Request<Texture2D>("RootsCore/ParticleTextures/Transparent/window_04")
        ];
    }
}

