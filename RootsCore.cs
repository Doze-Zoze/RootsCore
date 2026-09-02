using MonoMod.RuntimeDetour;
using System.Reflection;
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
    }
}
