using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace RootsCore.Extensions
{
    public static class Extensions
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

        public static Vector2 MultiLerp(this Vector2[] Vectors, float progress)
        {
            if (progress >= 1)
                return Vectors[^1];
            float scaled = progress * (Vectors.Length - 1);
            return Vector2.Lerp(Vectors[(int)scaled], Vectors[(int)scaled + 1], scaled % 1);
        }
        public static Vector2 MultiLerp(this List<Vector2> Vectors, float progress)
        {
            if (progress >= 1)
                return Vectors[^1];
            float scaled = progress * (Vectors.Count - 1);
            return Vector2.Lerp(Vectors[(int)scaled], Vectors[(int)scaled + 1], scaled % 1);
        }
    }
}
