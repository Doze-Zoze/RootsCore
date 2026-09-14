using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace RootsCore
{
    public class RootsCoreConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        public static RootsCoreConfig Instance => ModContent.GetInstance<RootsCoreConfig>();

        [DefaultValue(2500)]
        public int MaximumParticleCount;
    }
}
