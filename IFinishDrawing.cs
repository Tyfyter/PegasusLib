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
	public interface IFinishDrawingNPC {
		internal static GlobalHookList<GlobalNPC> HookFinishDrawingNPC = NPCLoader.AddModHook(GlobalHookList<GlobalNPC>.Create(g => ((IFinishDrawingNPC)g).FinishDrawingNPC));
		void FinishDrawingNPC(NPC npc);
		internal static void On_Main_DrawNPCDirect(On_Main.orig_DrawNPCDirect orig, Main self, SpriteBatch mySpriteBatch, NPC rCurrentNPC, bool behindTiles, Vector2 screenPos) {
			orig(self, mySpriteBatch, rCurrentNPC, behindTiles, screenPos);
			ReverseEntityGlobalsEnumerator<GlobalNPC> enumerator = HookFinishDrawingNPC.EnumerateReverse(rCurrentNPC).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IFinishDrawingNPC)enumerator.Current).FinishDrawingNPC(rCurrentNPC);
			}
		}
	}
	public interface IFinishDrawingProjectile {
		internal static GlobalHookList<GlobalProjectile> HookFinishDrawingProjectile = ProjectileLoader.AddModHook(GlobalHookList<GlobalProjectile>.Create(g => ((IFinishDrawingProjectile)g).FinishDrawingProjectile));
		void FinishDrawingProjectile(Projectile projectile);
		internal static void On_Main_DrawProj_Inner(On_Main.orig_DrawProj_Inner orig, Main self, Projectile proj) {
			orig(self, proj);
			ReverseEntityGlobalsEnumerator<GlobalProjectile> enumerator = HookFinishDrawingProjectile.EnumerateReverse(proj).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IFinishDrawingProjectile)enumerator.Current).FinishDrawingProjectile(proj);
			}
		}
	}
	public interface IFinishDrawingItemInWorld {
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInWorld = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IFinishDrawingItemInWorld)g).FinishDrawingItemInWorld));
		void FinishDrawingItemInWorld(Item item);
		internal static void On_Main_DrawItem(On_Main.orig_DrawItem orig, Main self, Item item, int whoami) {
			orig(self, item, whoami);
			ReverseEntityGlobalsEnumerator<GlobalItem> enumerator = HookFinishDrawingItemInWorld.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IFinishDrawingItemInWorld)enumerator.Current).FinishDrawingItemInWorld(item);
			}
		}
	}
	public interface IFinishDrawingItemInInventory {
		internal static GlobalHookList<GlobalItem> HookFinishDrawingItemInInventory = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(g => ((IFinishDrawingItemInInventory)g).FinishDrawingItemInInventory));
		void FinishDrawingItemInInventory(Item item);
		internal static float On_ItemSlot_DrawItemIcon(On_ItemSlot.orig_DrawItemIcon orig, Item item, int context, SpriteBatch spriteBatch, Vector2 screenPositionForItemCenter, float scale, float sizeLimit, Color environmentColor) {
			float ret = orig(item, context, spriteBatch, screenPositionForItemCenter, scale, sizeLimit, environmentColor);
			FinishDrawingItemInInventory(item);
			return ret;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <param name="worthless">exists solely to change the method signature</param>
		public static void FinishDrawingItemInInventory(Item item, int worthless = 0) {
			ReverseEntityGlobalsEnumerator<GlobalItem> enumerator = HookFinishDrawingItemInInventory.EnumerateReverse(item).GetEnumerator();
			while (enumerator.MoveNext()) {
				((IFinishDrawingItemInInventory)enumerator.Current).FinishDrawingItemInInventory(item);
			}
		}
	}
}
