using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RootsCore
{
    public abstract class AIOverride(NPC npc)
    {
        public NPC NPC = npc;

        /// <inheritdoc cref="GlobalNPC.PreAI(NPC)" />
        public virtual bool PreAI() => true;

        /// <inheritdoc cref="GlobalNPC.AI(NPC)" />
        public virtual void AI() { }

        /// <inheritdoc cref="GlobalNPC.SendExtraAI(NPC, BitWriter, BinaryWriter)" />
        public virtual void SendExtraAI(BitWriter bitWriter, BinaryWriter binaryWriter) { }

        /// <inheritdoc cref="GlobalNPC.ReceiveExtraAI(NPC, BitReader, BinaryReader)" />
        public virtual void ReceiveExtraAI(BitReader bitReader, BinaryReader binaryReader) { }
        public virtual void SetDefaults() { }

        /// <inheritdoc cref="GlobalNPC.OnSpawn(NPC, IEntitySource)" />
        public virtual void OnSpawn(IEntitySource source) { }

        /// <inheritdoc cref="GlobalNPC.OnKill(NPC)" />
        public virtual void OnKill() { }

        /// <inheritdoc cref="GlobalNPC.OnHitPlayer(NPC, Player, Player.HurtInfo)" />
        public virtual void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) { }
        /// <inheritdoc cref="GlobalNPC.OnHitByProjectile(NPC, Projectile, NPC.HitInfo, int)" />
        public virtual void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) { }

        /// <summary>
        /// Whether this AI override should replace ModNPC.ModifyHitByProjectile. If false, both will be called, if true, only this one will be called.<br/>
        /// Defaults to false.
        /// Does nothing on vanilla NPCs
        /// </summary>
        public virtual bool Replace_ModifyHitByProjectile => false;
        /// <inheritdoc cref="GlobalNPC.ModifyHitByProjectile(NPC, Projectile, ref NPC.HitModifiers)" />
        public virtual void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) { }

        /// <inheritdoc cref="GlobalNPC.OnHitByItem(NPC, Player, Item, NPC.HitInfo, int)" />
        public virtual void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone) { }
        /// <summary>
        /// Whether this AI override should replace ModNPC.ModifyHitByItem. If false, both will be called, if true, only this one will be called.<br/>
        /// Defaults to false.
        /// Does nothing on vanilla NPCs
        /// </summary>
        public virtual bool Replace_ModifyHitByItem => false;
        /// <inheritdoc cref="GlobalNPC.ModifyHitByItem(NPC, Player, Item, ref NPC.HitModifiers)" />
        public virtual void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers) { }
        /// <inheritdoc cref="GlobalNPC.PreDraw(NPC, SpriteBatch, Vector2, Color)" />

        /// <summary>
        /// Whether this AI override should replace ModNPC.PreDraw / PostDraw. If false, both will be called, if true, only this one will be called.<br/>
        /// Defaults to false.
        /// Does nothing on vanilla NPCs
        /// </summary>
        public virtual bool Replace_Draw => false;
        public virtual bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => true;
        /// <inheritdoc cref="GlobalNPC.BossHeadSlot(NPC, ref int)"/>
        public virtual void BossHeadSlot(ref int index) { }

    }
    public partial class AIOverrideSystem : GlobalNPC
    {

        public static bool TryGet<T>(NPC npc, out T ai) where T : AIOverride
        {
            ai = null;
            if (!npc.TryGetGlobalNPC<AIOverrideSystem>(out var sys))
                return false;
            if (sys.CurrentAiOverride is not T current)
                return false;
            ai = current;
            return true;
        }
        public static bool TryGet<T>(int npc, out T ai) where T : AIOverride
        {
            ai = null;
            if (!Main.npc.IndexInRange(npc) || !Main.npc[npc].TryGetGlobalNPC<AIOverrideSystem>(out var sys))
                return false;
            if (sys.CurrentAiOverride is not T current)
                return false;
            ai = current;
            return true;
        }
        public static T Get<T>(NPC npc) where T : AIOverride => Get(npc).CurrentAiOverride as T;
        public static T Get<T>(int npc) where T : AIOverride => Get(npc).CurrentAiOverride as T;
        public static AIOverrideSystem Get(NPC npc) => npc.GetGlobalNPC<AIOverrideSystem>();
        public static AIOverrideSystem Get(int npc) => Main.npc.IndexInRange(npc) ? Main.npc[npc].GetGlobalNPC<AIOverrideSystem>() : null;
        public override bool InstancePerEntity => true;
        public AIOverride CurrentAiOverride = null;
        
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);
            if (CurrentAiOverride is null && NpcSets.AIOverrides[npc.type].Count > 0)
            {
                var (predicate, aiFunc) = NpcSets.AIOverrides[npc.type].FirstOrDefault(x => x.predicate(npc),(null,null));
                if (aiFunc is not null)
                    CurrentAiOverride = aiFunc.Invoke(npc);
            }
            CurrentAiOverride?.SetDefaults();
        }
        public override bool PreAI(NPC npc)
        {
            if (CurrentAiOverride is null)
                return true;

            if (CurrentAiOverride.PreAI())
            {
                CurrentAiOverride.AI();
            }
            return false;
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) =>
            CurrentAiOverride?.SendExtraAI(bitWriter, binaryWriter);
        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) =>
            CurrentAiOverride?.ReceiveExtraAI(bitReader, binaryReader);
        public override void OnSpawn(NPC npc, IEntitySource source) =>
            CurrentAiOverride?.OnSpawn(source);
        public override void OnKill(NPC npc) =>
            CurrentAiOverride?.OnKill();
        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo) =>
            CurrentAiOverride?.OnHitPlayer(target, hurtInfo);
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) =>
            CurrentAiOverride?.OnHitByProjectile(projectile, hit, damageDone);
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) =>
            CurrentAiOverride?.ModifyHitByProjectile(projectile, ref modifiers);
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone) =>
            CurrentAiOverride?.OnHitByItem(player, item, hit, damageDone);
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) =>
            CurrentAiOverride?.ModifyHitByItem(player, item, ref modifiers);
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) =>
            CurrentAiOverride?.PreDraw(spriteBatch, screenPos, drawColor) ?? base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        public override void BossHeadSlot(NPC npc, ref int index) =>
            CurrentAiOverride?.BossHeadSlot(ref index);
    }
}
