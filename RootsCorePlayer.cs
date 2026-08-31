using Terraria.ModLoader;

namespace RootsCore
{
    public class RootsCorePlayer : ModPlayer
    {
        /// <summary>
        /// Used to detect what swing a weapon is doing.
        /// </summary>
        public int swingCounter = 0;
        public bool forceManaRegenStop = false;
        public override void ResetEffects()
        {
            if (Player.itemTime <= 1)
                forceManaRegenStop = false;
        }

    }
}
