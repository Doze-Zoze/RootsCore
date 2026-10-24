using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System.Reflection;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace RootsCore
{
    public partial class RootsCore : Mod
    {
        public override void Load()
        {
            LoadEdits();

            //Hooks to ModNPC. This is used by AI Override system to override ModNPC hooks.
            _hooks.Add(new Hook(typeof(ModNPC).GetMethod("ModifyHitByItem", BindingFlags.Public | BindingFlags.Instance), ModifyHitByItem));
            _hooks.Add(new Hook(typeof(ModNPC).GetMethod("ModifyHitByProjectile", BindingFlags.Public | BindingFlags.Instance), ModifyHitByProjectile));
            _hooks.Add(new Hook(typeof(ModNPC).GetMethod("PreDraw", BindingFlags.Public | BindingFlags.Instance), PreDraw));
            _hooks.Add(new Hook(typeof(ModNPC).GetMethod("PostDraw", BindingFlags.Public | BindingFlags.Instance), PostDraw));
        }

        private static Asset<Effect> QuantizeShader;
        public override void PostSetupContent()
        {
            QuantizeShader ??= ModContent.GetInstance<RootsCore>().Assets.Request<Effect>("Shaders/QuantizeShader", AssetRequestMode.ImmediateLoad);
            GameShaders.Misc["RootsCore:QuantizeShader"] = new(QuantizeShader, "QuantizePass");
        }
    }
}
