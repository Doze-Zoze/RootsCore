using System;
using Terraria;
using Terraria.ModLoader;

namespace RootsCore
{
    public class DrawLayerSystem : ModSystem
    {

        /// <summary>
        /// Which layer you'd like this projectile to draw on. The draw order is as follows<br/>
        /// Walls -> Tiles -> NPCs -> Projectiles -> Players -> Dusts -> Everything
        /// </summary>
        public enum DrawLayer
        {
            BeforeWalls,
            AfterWalls,
            AfterTiles,
            BeforeNPCs,
            AfterNPCs,
            BeforeProjectiles,
            AfterProjectiles,
            AfterPlayers,
            AfterDusts,
            AfterEverything
        }

        /// <summary>
        /// Action that is called whenever a draw layer should be drawn<br/>
        /// NOTE: Spritebatch must be begun and ended in methods appending to this action
        /// </summary>
        public static Action<DrawLayer> DrawToLayer;

        public override void Load()
        {
            On_Main.DrawBackgroundBlackFill += DrawToLayer_BeforeWalls;
            On_Main.DoDraw_Tiles_Solid += DrawToLayer_AfterWalls;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawToLayer_NPCs;
            On_Main.DrawProjectiles += DrawToLayer_Projectiles;
            On_Main.DrawPlayers_AfterProjectiles += DrawToLayer_AfterPlayers;
            On_Main.DrawDust += DrawToLayer_AfterDusts;
            On_Main.DrawInfernoRings += DrawToLayer_AfterEverything;
        }

        public override void Unload()
        {
            DrawToLayer = null;
        }
        private static void DrawToLayer_BeforeWalls(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
        {
            Main.spriteBatch.End();
            DrawToLayer?.Invoke(DrawLayer.BeforeWalls);
            Main.spriteBatch.Begin();
            orig(self);
        }
        private static void DrawToLayer_AfterWalls(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            DrawToLayer?.Invoke(DrawLayer.AfterWalls);
            orig(self);
        }
        private static void DrawToLayer_NPCs(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
        {
            DrawToLayer?.Invoke(DrawLayer.BeforeNPCs);
            orig(self);
            DrawToLayer?.Invoke(DrawLayer.AfterNPCs);
        }
        private static void DrawToLayer_Projectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            DrawToLayer?.Invoke(DrawLayer.BeforeProjectiles);
            orig(self);
            DrawToLayer?.Invoke(DrawLayer.AfterProjectiles);
        }
        private static void DrawToLayer_AfterPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
        {
            orig(self);
            DrawToLayer?.Invoke(DrawLayer.AfterPlayers);
        }
        private static void DrawToLayer_AfterDusts(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);
            DrawToLayer?.Invoke(DrawLayer.AfterDusts);
        }
        private static void DrawToLayer_AfterEverything(On_Main.orig_DrawInfernoRings orig, Main self)
        {
            orig(self);
            Main.spriteBatch.End();
            DrawToLayer?.Invoke(DrawLayer.AfterEverything);
            Main.spriteBatch.Begin();
        }
    }
}
