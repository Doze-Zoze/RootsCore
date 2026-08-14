using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RootsCore
{
    public class IdSetManager : ModSystem
    {
        public override void ResizeArrays()
        {
            for (int i = 0; i < NpcSets.AiOverrides.Length; i++)
            {
                NpcSets.AiOverrides[i] = [];
            }
        }
    }


    [ReinitializeDuringResizeArrays]
    public static class ItemSets
    {
        public static bool[] DontConsumeManaOnSwing = ItemID.Sets.Factory
            .CreateNamedSet("DontConsumeManaOnSwing")
            .Description("Stops items with Item.mana set from consuming Mana on swing")
            .RegisterBoolSet(false);

        public static bool[] DontUseVanillaEquipEffects = ItemID.Sets.Factory
            .CreateNamedSet("DontUseVanillaEquipEffects")
            .Description("Stops items from applying their vanilla equipment effects")
            .RegisterBoolSet(false);

        public static bool[] DontUseVanillaSetBonus = ItemID.Sets.Factory
            .CreateNamedSet("DontUseVanillaSetBonus")
            .Description("Stops items from applying their vanilla set bonus")
            .RegisterBoolSet(false);

        public static bool[] ManaStarPickup = ItemID.Sets.Factory
            .CreateNamedSet("ManaStarPickup")
            .Description("Items that count as Mana Star pickups")
            .RegisterBoolSet(false, ItemID.Star, ItemID.ManaCloakStar, ItemID.SoulCake, ItemID.SugarPlum);

        public static Predicate<(Player, Item)>[] ShouldResetManaRegen = ItemID.Sets.Factory
            .CreateNamedSet("ShouldResetManaRegen")
            .Description("Predicate to decide when an item should reset mana regeneration")
            .RegisterCustomSet<Predicate<(Player, Item)>>(null);
    }

    [ReinitializeDuringResizeArrays]
    public static class ProjSets
    {
        public static bool[] ManaSpawnedProjectile = ProjectileID.Sets.Factory
            .CreateNamedSet("ManaSpawnedProjectile")
            .Description("Projectiles spanwed from mana-consuming attacks")
            .RegisterBoolSet(false);
    }

    [ReinitializeDuringResizeArrays]
    public static class NpcSets
    {
        public static List<(Predicate<NPC> predicate, Func<NPC, AIOverride> AiFunc)>[] AiOverrides = NPCID.Sets.Factory
            .CreateNamedSet("AiOverrides")
            .Description("Ai overrides to apply to a given NPC if the given predicate is true. Mods should not set the list directly, but instead append to it")
            .RegisterCustomSet<List<(Predicate<NPC>,Func<NPC, AIOverride>)>>(null);
    }
}
