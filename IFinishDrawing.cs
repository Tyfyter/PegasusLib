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

namespace PegasusLib {
	public interface IDrawNPCEffect {
		internal static GlobalHookList<GlobalNPC> HookPrepareToDrawNPC = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(g => ((IDrawNPCEffect)g).PrepareToDrawNPC));
		internal static GlobalHookList<GlobalNPC> HookFinishDrawingNPC = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(g => ((IDrawNPCEffect)g).FinishDrawingNPC));
		void PrepareToDrawNPC(NPC npc);
		void FinishDrawingNPC(NPC npc);
		internal static void On_Main_DrawNPCDirect(On_Main.orig_DrawNPCDirect orig, Main self, SpriteBatch mySpriteBatch, NPC rCurrentNPC, bool behindTiles, Vector2 screenPos) {
			ReverseEntityGlobalsEnumerator<GlobalNPC> enumerator = HookPrepareToDrawNPC.EnumerateReverse(rCurrentNPC).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawNPCEffect)enumerator.Current).FinishDrawingNPC(rCurrentNPC);
			}
			orig(self, mySpriteBatch, rCurrentNPC, behindTiles, screenPos);
			enumerator = HookFinishDrawingNPC.EnumerateReverse(rCurrentNPC).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawNPCEffect)enumerator.Current).FinishDrawingNPC(rCurrentNPC);
			}
		}
	}
	public interface IDrawProjectileEffect {
		internal static GlobalHookList<GlobalProjectile> HookPrepareToDrawProjectile = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(g => ((IDrawProjectileEffect)g).PrepareToDrawProjectile));
		internal static GlobalHookList<GlobalProjectile> HookFinishDrawingProjectile = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(g => ((IDrawProjectileEffect)g).FinishDrawingProjectile));
		void PrepareToDrawProjectile(Projectile projectile);
		void FinishDrawingProjectile(Projectile projectile);
		internal static void On_Main_DrawProj_Inner(On_Main.orig_DrawProj_Inner orig, Main self, Projectile proj) {
			ReverseEntityGlobalsEnumerator<GlobalProjectile> enumerator = HookPrepareToDrawProjectile.EnumerateReverse(proj).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawProjectileEffect)enumerator.Current).PrepareToDrawProjectile(proj);
			}
			orig(self, proj);
			enumerator = HookFinishDrawingProjectile.EnumerateReverse(proj).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawProjectileEffect)enumerator.Current).FinishDrawingProjectile(proj);
			}
		}
	}
	public interface IDrawItemInWorldEffect {
		internal static GlobalHookList<GlobalItem> HookPrepareToDrawItemInWorld = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInWorldEffect)g).PrepareToDrawItemInWorld));
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInWorld = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInWorldEffect)g).FinishDrawingItemInWorld));
		void PrepareToDrawItemInWorld(Item item);
		void FinishDrawingItemInWorld(Item item);
		internal static void On_Main_DrawItem(On_Main.orig_DrawItem orig, Main self, Item item, int whoami) {
			ReverseEntityGlobalsEnumerator<GlobalItem> enumerator = HookPrepareToDrawItemInWorld.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawItemInWorldEffect)enumerator.Current).FinishDrawingItemInWorld(item);
			}
			orig(self, item, whoami);
			enumerator = HookFinishDrawingItemInWorld.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawItemInWorldEffect)enumerator.Current).FinishDrawingItemInWorld(item);
			}
		}
	}
	public interface IDrawItemInInventoryEffect {
		internal static GlobalHookList<GlobalItem> HookPrepareToDrawItemInInventory = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInInventoryEffect)g).PrepareToDrawItemInInventory));
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInInventory = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IDrawItemInInventoryEffect)g).FinishDrawingItemInInventory));
		void PrepareToDrawItemInInventory(Item item, int context);
		void FinishDrawingItemInInventory(Item item, int context);
		internal static float On_ItemSlot_DrawItemIcon(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
			PrepareToDrawItemInInventory(item, context);
			float ret = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
			FinishDrawingItemInInventory(item, context);
			return ret;
		}
		/// <param name="worthless">exists solely to change the method signature</param>
		public static void PrepareToDrawItemInInventory(Item item, int context, int worthless = 0) {
			ReverseEntityGlobalsEnumerator<GlobalItem> enumerator = HookPrepareToDrawItemInInventory.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawItemInInventoryEffect)enumerator.Current).PrepareToDrawItemInInventory(item, context);
			}
		}
		/// <param name="worthless">exists solely to change the method signature</param>
		public static void FinishDrawingItemInInventory(Item item, int context, int worthless = 0) {
			ReverseEntityGlobalsEnumerator<GlobalItem> enumerator = HookFinishDrawingItemInInventory.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IDrawItemInInventoryEffect)enumerator.Current).FinishDrawingItemInInventory(item, context);
			}
		}
	}
}
