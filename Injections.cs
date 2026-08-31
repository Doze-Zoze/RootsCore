using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RootsCore
{
    public partial class RootsCore : Mod
    {
        void LoadEdits()
        {
            On_Player.ItemCheck_PayMana += AllowItemUsageWithImproperMana;
            On_Player.ItemCheck_ApplyManaRegenDelay += DisableManaRegenDelayWhenOutOfMana;
            On_Player.ApplyEquipFunctional += DisableVanillaEquipmentBuffs;
            On_Player.GrantArmorBenefits += DisableArmorPieceBuffs;
            On_Player.UpdateArmorSets += DisableArmorSetBonus;

            // see ArenaBoxSystem.cs for methods
            On_Projectile.AI_007_GrapplingHooks += AllowHooksToGrabArenabox;
            On_Collision.SolidCollision_Vector2_int_int += ArenaCollision_Vector2_int_int;
            On_Collision.SolidCollision_Vector2_int_int_bool += ArenaCollision_Vector2_int_int_bool;
            On_Collision.TileCollision += ArenaCollision_TileCollision;

        }


        private static List<Hook> Hooks = new();
        public override void Unload()
        {
            for (int i = 0; i < Hooks.Count; i++)
            {
                Hooks[i]?.Dispose();
                Hooks[i] = null;
            }
            Hooks = new();
        }



        private delegate bool orig_ModNPC_PreDraw(ModNPC self, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);
        private delegate void orig_ModNPC_PostDraw(ModNPC self, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor);
        private bool PreDraw(orig_ModNPC_PreDraw orig, ModNPC self, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (AIOverrideSystem.Get(self.Type)?.CurrentAiOverride?.Replace_Draw ?? false)
                return false;
            return orig(self, spriteBatch, screenPos, drawColor);
        }
        private void PostDraw(orig_ModNPC_PostDraw orig, ModNPC self, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (AIOverrideSystem.Get(self.Type)?.CurrentAiOverride?.Replace_Draw ?? false)
                return;
            orig(self, spriteBatch, screenPos, drawColor);
        }

        private delegate void orig_ModifyHitByProjectile(ModNPC self, Projectile projectile, ref NPC.HitModifiers modifiers);
        private void ModifyHitByProjectile(orig_ModifyHitByProjectile orig, ModNPC self, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            var ai = AIOverrideSystem.Get(self.NPC).CurrentAiOverride;
            //ai?.ModifyHitByProjectile(projectile, ref modifiers);
            if (ai?.Replace_ModifyHitByProjectile ?? false)
                return;
            orig(self, projectile, ref modifiers);
        }

        private delegate void orig_ModifyHitByItem(ModNPC self, Player player, Item item, ref NPC.HitModifiers modifiers);
        private void ModifyHitByItem(orig_ModifyHitByItem orig, ModNPC self, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            var ai = AIOverrideSystem.Get(self.NPC).CurrentAiOverride;
            //ai?.ModifyHitByItem(player,item, ref modifiers);
            if (ai?.Replace_ModifyHitByItem ?? false)
                return;
            orig(self, player, item, ref modifiers);
        }

        private void DisableArmorSetBonus(On_Player.orig_UpdateArmorSets orig, Player self, int i)
        {
            if (ItemSets.DontUseVanillaSetBonus[self.armor[0].type] || ItemSets.DontUseVanillaSetBonus[self.armor[2].type] || ItemSets.DontUseVanillaSetBonus[self.armor[2].type])
            {
                ItemLoader.UpdateArmorSet(self, self.armor[0], self.armor[1], self.armor[2]);
                return;
            }
            orig(self, i);
        }

        private void DisableArmorPieceBuffs(On_Player.orig_GrantArmorBenefits orig, Player self, Item armorPiece)
        {
            if (ItemSets.DontUseVanillaEquipEffects[armorPiece.type])
            {
                int type = armorPiece.type;
                self.RefreshInfoAccsFromItemType(armorPiece);
                self.RefreshMechanicalAccsFromItemType(type);

                self.statDefense += armorPiece.defense;
                self.lifeRegen += armorPiece.lifeRegen;
                if (armorPiece.shieldSlot > 0)
                    self.hasRaisableShield = true;

                if (armorPiece.type == ItemID.AmberRobe || (armorPiece.type >= ItemID.AmethystRobe && armorPiece.type <= ItemID.DiamondRobe))
                    self.hasGemRobe = true;

                ItemLoader.UpdateEquip(armorPiece, self);
                return;
            }
            orig(self, armorPiece);
        }

        private void DisableVanillaEquipmentBuffs(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
        {
            if (ItemSets.DontUseVanillaEquipEffects[currentItem.type])
            {
                ItemLoader.UpdateAccessory(currentItem, self, hideVisual);
                return;
            }
            orig(self, currentItem, hideVisual);
            return;
        }
        private void DisableManaRegenDelayWhenOutOfMana(On_Player.orig_ItemCheck_ApplyManaRegenDelay orig, Player self, Item sItem)
        {
            if (self.GetModPlayer<RootsCorePlayer>().forceManaRegenStop || (ItemSets.ShouldResetManaRegen[sItem.type]?.Invoke((self, sItem)) ?? self.statMana >= (int)(sItem.mana * self.manaCost) ))
            {
                orig(self, sItem);
                self.GetModPlayer<RootsCorePlayer>().forceManaRegenStop = true;
            }
        }
        private bool AllowItemUsageWithImproperMana(On_Player.orig_ItemCheck_PayMana orig, Player self, Item sItem, bool canUse)
        {
            if (ItemSets.DontConsumeManaOnSwing[sItem.type])
                return canUse;
            return orig(self, sItem, canUse);
        }
    }
}