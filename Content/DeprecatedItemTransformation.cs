using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PegasusLib.Content;
public class DeprecatedItemTransformation : ILoadable {
	static readonly Dictionary<string, string> conversions = [];
	public static void Add(string old, string @new) => conversions.Add(old, @new);
	public static void Add(string old, int newVanilla) {
		if (newVanilla < 0 || newVanilla >= ItemID.Count) throw new ArgumentException($"newVanilla must be a vanilla item ID, {newVanilla} is not a valid vanilla item ID", nameof(newVanilla));
		conversions.Add(old, ItemID.Search.GetName(newVanilla));
	}
	void ILoadable.Load(Mod mod) {
		try {
			MonoModHooks.Add(((orig_Load)ItemIO.Load).Method, LoadItem);
		} catch (Exception e) {
			PegasusLib.FeatureError(LibFeature.DeprecatedItemTransformation, e);
		}
	}
	static void LoadItem(orig_Load orig, Item item, TagCompound tag) {
		if (tag.TryGet("mod", out string mod) && tag.TryGet("name", out string name) && conversions.TryGetValue($"{mod}/{name}", out string conversion)) {
			string[] parts = conversion.Split('/');
			switch (parts.Length) {
				case 1:
				tag["mod"] = "Terraria";
				tag["id"] = int.TryParse(conversion, out int id) ? id : ItemID.Search.GetId(conversion);
				break;
				case 2:
				tag["mod"] = parts[0];
				tag["name"] = parts[1];
				break;
			}
		}
		orig(item, tag);
	}
	delegate void hook_Load(orig_Load orig, Item item, TagCompound tag);
	delegate void orig_Load(Item item, TagCompound tag);
	void ILoadable.Unload() { }
}