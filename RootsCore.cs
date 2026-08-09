using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace RootsCore
{
	public partial class RootsCore : Mod
	{
        public override void Load()
        {
            LoadEdits();

            //Hooks to ModNPC. This is used by AI Override system to overried ModNPC hooks.
            Hooks.Add(new Hook(typeof(ModNPC).GetMethod("ModifyHitByItem", BindingFlags.Public | BindingFlags.Instance), ModifyHitByItem));
            Hooks.Add(new Hook(typeof(ModNPC).GetMethod("ModifyHitByProjectile", BindingFlags.Public | BindingFlags.Instance), ModifyHitByProjectile));
            Hooks.Add(new Hook(typeof(ModNPC).GetMethod("PreDraw", BindingFlags.Public | BindingFlags.Instance), PreDraw));
            Hooks.Add(new Hook(typeof(ModNPC).GetMethod("PostDraw", BindingFlags.Public | BindingFlags.Instance), PostDraw));
        }
    }
}
