using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib.Content;
public static class SearchExtraInventories {
	public static IEnumerable<Item[]> ExtraInventories(this Player player) {
		for (int i = 0; i < AddExtraInventoryForSearches.getters.Count; i++) yield return AddExtraInventoryForSearches.getters[i](player);
	}
	static bool HasItem(this Item[] collection, Predicate<Item> item) {
		for (int i = 0; i < collection.Length; i++) {
			if ((collection[i]?.stack ?? 0) > 0 && item(collection[i])) return true;
		}
		return false;
	}
	public static bool HasItem(this Player player, Predicate<Item> item) {
		if (player.inventory.HasItem(item)) return true;
		foreach (Item[] inventory in player.ExtraInventories()) {
			if (inventory.HasItem(item)) return true;
		}
		return false;
	}
	public static bool HasItemInAnyInventory(this Player player, Predicate<Item> item) {
		if (player.HasItem(item)) return true;
		if (player.armor.HasItem(item)) return true;
		if (player.dye.HasItem(item)) return true;
		if (player.miscEquips.HasItem(item)) return true;
		if (player.miscDyes.HasItem(item)) return true;
		if (player.bank.item.HasItem(item)) return true;
		if (player.bank2.item.HasItem(item)) return true;
		if (player.bank3.item.HasItem(item)) return true;
		if (player.bank4.item.HasItem(item)) return true;
		return false;
	}
	public static bool HasItemInAnyInventory(this Player player, bool[] itemSet) {
		if (player.HasItem(itemSet)) return true;
		if (HasItem(player.armor, itemSet)) return true;
		if (HasItem(player.dye, itemSet)) return true;
		if (HasItem(player.miscEquips, itemSet)) return true;
		if (HasItem(player.miscDyes, itemSet)) return true;
		if (HasItem(player.bank.item, itemSet)) return true;
		if (HasItem(player.bank2.item, itemSet)) return true;
		if (HasItem(player.bank3.item, itemSet)) return true;
		if (HasItem(player.bank4.item, itemSet)) return true;
		return false;
		static bool HasItem(Item[] collection, bool[] itemSet) {
			for (int i = 0; i < collection.Length; i++) {
				if ((collection[i]?.stack ?? 0) > 0 && itemSet[collection[i].type]) return true;
			}
			return false;
		}
	}
}

public class AddExtraInventoryForSearches : AutoModCall {
	internal static List<Func<Player, Item[]>> getters = [];
	public override void Load() {
		On_Player.HasItem_int += _On_Player_HasItem_int;
		On_Player.HasItem_int_ItemArray += _On_Player_HasItem_int_ItemArray;
		On_Player.HasItem_BooleanArray += _On_Player_HasItem_BooleanArray;
		On_Player.ConsumeItem += _On_Player_ConsumeItem;
	}
	static bool _On_Player_HasItem_int(On_Player.orig_HasItem_int orig, Player self, int type) {
		if (orig(self, type)) return true;
		foreach (Item item in self.ExtraInventories().SelectMany(x => x)) {
			if (item.type == type && item.stack > 0) return true;
		}
		return false;
	}
	static bool _On_Player_HasItem_int_ItemArray(On_Player.orig_HasItem_int_ItemArray orig, Player self, int type, Item[] collection) {
		if (orig(self, type, collection)) return true;
		if (collection == self.inventory) {
			foreach (Item[] inventory in self.ExtraInventories()) {
				if (orig(self, type, inventory)) return true;
			}
		}
		return false;
	}
	static bool _On_Player_HasItem_BooleanArray(On_Player.orig_HasItem_BooleanArray orig, Player self, bool[] itemSet) {
		if (orig(self, itemSet)) return true;
		foreach (Item item in self.ExtraInventories().SelectMany(x => x)) {
			if (itemSet[item.type] && item.stack > 0) return true;
		}
		return false;
	}
	static bool _On_Player_ConsumeItem(On_Player.orig_ConsumeItem orig, Player self, int type, bool reverseOrder, bool includeVoidBag) {
		if (orig(self, type, reverseOrder, includeVoidBag)) return true;
		foreach (Item[] inventory in self.ExtraInventories()) {
			int index = self.FindItem(type, inventory);
			if (index == -1) continue;
			Item item = inventory[index];
			if (ItemLoader.ConsumeItem(item, self) && --item.stack <= 0) item.TurnToAir();
			return true;
		}
		return false;
	}
	public static void Call(Func<Player, Item[]> getter) => getters.Add(getter);
}