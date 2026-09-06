using Terraria.ModLoader;

namespace RootsCore
{
    public class RootsCorePlayer : ModPlayer
    {
        /// <summary>
        /// Used to detect what swing a weapon is doing.
        /// </summary>
        public int SwingCounter;
        public bool ForceManaRegenStop;
        public override void ResetEffects()
        {
            if (Player.itemTime <= 1)
                ForceManaRegenStop = false;
        }

    }
}
