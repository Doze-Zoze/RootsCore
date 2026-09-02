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
            for (int i = 0; i < NpcSets.AIOverrides.Length; i++)
            {
                NpcSets.AIOverrides[i] = [];
            }
        }
    }

    [ReinitializeDuringResizeArrays]
    public static class ItemSets
    {
        public static readonly bool[] DontConsumeManaOnSwing = ItemID.Sets.Factory
            .CreateNamedSet("DontConsumeManaOnSwing")
            .Description("Stops items with Item.mana set from consuming Mana on swing")
            .RegisterBoolSet(false);

        public static readonly bool[] DontUseVanillaEquipEffects = ItemID.Sets.Factory
            .CreateNamedSet("DontUseVanillaEquipEffects")
            .Description("Stops items from applying their vanilla equipment effects")
            .RegisterBoolSet(false);

        public static readonly bool[] DontUseVanillaSetBonus = ItemID.Sets.Factory
            .CreateNamedSet("DontUseVanillaSetBonus")
            .Description("Stops items from applying their vanilla set bonus")
            .RegisterBoolSet(false);

        public static readonly bool[] ManaStarPickup = ItemID.Sets.Factory
            .CreateNamedSet("ManaStarPickup")
            .Description("Items that count as Mana Star pickups")
            .RegisterBoolSet(false, ItemID.Star, ItemID.ManaCloakStar, ItemID.SoulCake, ItemID.SugarPlum);

        public static readonly Predicate<(Player, Item)>[] ShouldResetManaRegen = ItemID.Sets.Factory
            .CreateNamedSet("ShouldResetManaRegen")
            .Description("Predicate to decide when an item should reset mana regeneration")
            .RegisterCustomSet<Predicate<(Player, Item)>>(null);
    }

    [ReinitializeDuringResizeArrays]
    public static class ProjSets
    {
        public static readonly bool[] ManaSpawnedProjectile = ProjectileID.Sets.Factory
            .CreateNamedSet("ManaSpawnedProjectile")
            .Description("Projectiles spawned from mana-consuming attacks")
            .RegisterBoolSet(false);
    }

    [ReinitializeDuringResizeArrays]
    public static class NpcSets
    {
        public static readonly List<(Predicate<NPC> predicate, Func<NPC, AIOverride> AIFunc)>[] AIOverrides = NPCID.Sets.Factory
            .CreateNamedSet("AIOverrides")
            .Description("AI overrides to apply to a given NPC if the given predicate is true. Mods should not set the list directly, but instead append to it")
            .RegisterCustomSet<List<(Predicate<NPC>,Func<NPC, AIOverride>)>>(null);
    }
}
