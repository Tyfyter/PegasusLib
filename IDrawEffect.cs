using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader.Core;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.UI;
using MonoMod.Cil;
using PegasusLib.Reflection;
using Terraria.GameContent;

namespace PegasusLib {
	public interface IDrawNPCEffect {
		internal static GlobalHookList<GlobalNPC> HookPrepareToDrawNPC = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(g => ((IDrawNPCEffect)g).PrepareToDrawNPC));
		internal static GlobalHookList<GlobalNPC> HookFinishDrawingNPC = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(g => ((IDrawNPCEffect)g).FinishDrawingNPC));
		void PrepareToDrawNPC(NPC npc);
		void FinishDrawingNPC(NPC npc);
		bool OnlyWrapModTypeDraw => true;
		internal static void On_Main_DrawNPCDirect(On_Main.orig_DrawNPCDirect orig, Main self, SpriteBatch mySpriteBatch, NPC rCurrentNPC, bool behindTiles, Vector2 screenPos) {
			foreach (GlobalNPC c in HookPrepareToDrawNPC.Enumerate(rCurrentNPC).GetEnumerator()) {
				if (c is IDrawNPCEffect current && !current.OnlyWrapModTypeDraw) current.PrepareToDrawNPC(rCurrentNPC);
			}
			orig(self, mySpriteBatch, rCurrentNPC, behindTiles, screenPos);
			foreach (GlobalNPC c in HookFinishDrawingNPC.EnumerateReverse(rCurrentNPC).GetEnumerator()) {
				if (c is IDrawNPCEffect current && !current.OnlyWrapModTypeDraw) current.FinishDrawingNPC(rCurrentNPC);
			}
		}
		internal static void AddIteratePreDraw(ILContext il) {
			try {
				ILCursor c = new(il);
				ILLabel label = default;
				c.GotoNext(MoveType.Before,
					il => il.MatchLdloc(out _),
					il => il.MatchBrfalse(out label),
					il => il.MatchLdarg0(),
					il => il.MatchCallOrCallvirt<NPC>("get_" + nameof(NPC.ModNPC)),
					il => il.MatchBrfalse(out ILLabel _label) && _label.Target == label.Target
				);
				c.EmitLdarg0();
				c.EmitDelegate((NPC npc) => {
					foreach (GlobalNPC c in HookPrepareToDrawNPC.Enumerate(npc).GetEnumerator()) {
						if (c is IDrawNPCEffect current && current.OnlyWrapModTypeDraw) current.PrepareToDrawNPC(npc);
					}
				});
			} catch (Exception exception) {
				PegasusLib.FeatureError(LibFeature.IDrawNPCEffect, exception);
#if DEBUG
				throw;
#endif
			}
		}
		internal static void AddIteratePostDraw(ILContext il) {
			try {
				ILCursor c = new(il);
				c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<ModNPC>(nameof(ModNPC.PostDraw)));
				c.MoveAfterLabels();
				c.EmitLdarg0();
				c.EmitDelegate((NPC npc) => {
					foreach (GlobalNPC c in HookFinishDrawingNPC.Enumerate(npc).GetEnumerator()) {
						if (c is IDrawNPCEffect current && current.OnlyWrapModTypeDraw) current.FinishDrawingNPC(npc);
					}
				});
			} catch (Exception exception) {
				PegasusLib.FeatureError(LibFeature.IDrawNPCEffect, exception);
#if DEBUG
				throw;
#endif
			}
		}
	}
	public interface IDrawProjectileEffect {
		internal static GlobalHookList<GlobalProjectile> HookPrepareToDrawProjectile = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(g => ((IDrawProjectileEffect)g).PrepareToDrawProjectile));
		internal static GlobalHookList<GlobalProjectile> HookFinishDrawingProjectile = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(g => ((IDrawProjectileEffect)g).FinishDrawingProjectile));
		void PrepareToDrawProjectile(Projectile projectile);
		void FinishDrawingProjectile(Projectile projectile);
		internal static void On_Main_DrawProj_Inner(On_Main.orig_DrawProj_Inner orig, Main self, Projectile proj) {
			if (proj.whoAmI == 0) {

			}
			foreach (GlobalProjectile c in HookPrepareToDrawProjectile.Enumerate(proj).GetEnumerator()) {
				if (c is IDrawProjectileEffect current) current.PrepareToDrawProjectile(proj);
			}
			orig(self, proj);
			foreach (GlobalProjectile c in HookFinishDrawingProjectile.EnumerateReverse(proj).GetEnumerator()) {
				if (c is IDrawProjectileEffect current) current.FinishDrawingProjectile(proj);
			}
		}
	}
	public interface IDrawItemInWorldEffect {
		internal static GlobalHookList<GlobalItem> HookPrepareToDrawItemInWorld = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInWorldEffect)g).PrepareToDrawItemInWorld));
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInWorld = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInWorldEffect)g).FinishDrawingItemInWorld));
		void PrepareToDrawItemInWorld(Item item);
		void FinishDrawingItemInWorld(Item item);
		internal static void On_Main_DrawItem(On_Main.orig_DrawItem orig, Main self, Item item, int whoami) {
			foreach (GlobalItem c in HookPrepareToDrawItemInWorld.Enumerate(item).GetEnumerator()) {
				if (c is IDrawItemInWorldEffect current) current.PrepareToDrawItemInWorld(item);
			}
			orig(self, item, whoami);
			foreach (GlobalItem c in HookFinishDrawingItemInWorld.EnumerateReverse(item).GetEnumerator()) {
				if (c is IDrawItemInWorldEffect current) current.FinishDrawingItemInWorld(item);
			}
		}
	}
	public interface IDrawItemInInventoryEffect {
		internal static GlobalHookList<GlobalItem> HookPrepareToDrawItemInInventory = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInInventoryEffect)g).PrepareToDrawItemInInventory));
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInInventory = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInInventoryEffect)g).FinishDrawingItemInInventory));
		void PrepareToDrawItemInInventory(Item item, int context);
		void FinishDrawingItemInInventory(Item item, int context);
		internal static float On_ItemSlot_DrawItemIcon(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
			foreach (GlobalItem c in HookPrepareToDrawItemInInventory.Enumerate(item).GetEnumerator()) {
				if (c is IDrawItemInInventoryEffect current) current.PrepareToDrawItemInInventory(item, context);
			}
			float ret = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
			foreach (GlobalItem c in HookFinishDrawingItemInInventory.EnumerateReverse(item).GetEnumerator()) {
				if (c is IDrawItemInInventoryEffect current) current.FinishDrawingItemInInventory(item, context);
			}
			return ret;
		}
	}
}
