using Microsoft.Xna.Framework;
using Terraria;

namespace RootsCore.Extensions
{
    public static class PlayerExtensions
    {
        extension(Player player)
        {
            public float GetMeleeScale(bool addHeldItemScale = true)
            {
                float baseScale = 1;
                player.ApplyMeleeScale(ref baseScale);
                if (addHeldItemScale)
                    baseScale += (player.HeldItem.scale - 1);

                return baseScale;
            }

            //TODO - Multiplayer compatibility/netsyncing needed
            public Vector2 MouseWorld => Main.MouseWorld;
        }
    }
}
