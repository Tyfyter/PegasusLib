using Terraria;
using Terraria.ModLoader;

namespace PegasusLib {
	public static class ShopExt {
		public static NPCShop InsertAfter<T>(this NPCShop shop, int targetItem, params Condition[] condition) where T : ModItem =>
			shop.InsertAfter(targetItem, ModContent.ItemType<T>(), condition);
		public static NPCShop InsertBefore<T>(this NPCShop shop, int targetItem, params Condition[] condition) where T : ModItem =>
			shop.InsertBefore(targetItem, ModContent.ItemType<T>(), condition);
		public static NPCShop InsertAfter<TAfter, TNew>(this NPCShop shop, params Condition[] condition) where TAfter : ModItem where TNew : ModItem =>
			shop.InsertAfter(ModContent.ItemType<TAfter>(), ModContent.ItemType<TNew>(), condition);
		public static NPCShop InsertBefore<TBefore, TNew>(this NPCShop shop, params Condition[] condition) where TBefore : ModItem where TNew : ModItem =>
			shop.InsertBefore(ModContent.ItemType<TBefore>(), ModContent.ItemType<TNew>(), condition);
	}
}
