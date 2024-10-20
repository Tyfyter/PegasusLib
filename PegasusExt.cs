using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace PegasusLib {
	public static class PegasusExt {
		public static T SafeGet<T>(this TagCompound self, string key, T fallback = default) {
			return self.TryGet(key, out T output) ? output : fallback;
		}
		public static string GetDefaultTMLName(this Type type) => (type.Namespace + "." + type.Name).Replace('.', '/');
		public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum {
			T[] possibleFlags = Enum.GetValues<T>();
			for (int i = 0; i < possibleFlags.Length; i++) {
				if (possibleFlags[i].Equals(default(T))) continue;
				if (value.HasFlag(possibleFlags[i])) yield return possibleFlags[i];
			}
		}
		/// <summary>
		/// Checks if a recipe matches the provided pattern
		/// </summary>
		/// <param name="recipe">the recipe being checked</param>
		/// <param name="result">the result item, set to null to ignore the recipe's result, set count to null to ignore the amount of items the recipe crafts</param>
		/// <param name="tiles">the required tiles, set to null to ignore the tiles the recipe requires</param>
		/// <param name="ingredients">the ingredients, set to null to ignore the recipe's ingredients, set count to null to ignore the amount of the ingredient used</param>
		/// <returns></returns>
		public static bool Matches(this Recipe recipe, (int id, int? count)? result, int[] tiles, params (int id, int? count)[] ingredients) {
			static bool ItemMatches(Item item, (int id, int? count) pattern) {
				if (item.type == pattern.id) {
					return !pattern.count.HasValue || item.stack == pattern.count;
				}
				return false;
			}
			if (result.HasValue && !ItemMatches(recipe.createItem, result.Value)) return false;
			if (ingredients is not null) {
				if (recipe.requiredItem.Count == ingredients.Length) {
					for (int i = 0; i < ingredients.Length; i++) {
						(int id, int? count) ingredient = ingredients[i];
						if (!recipe.requiredItem.Any(req => ItemMatches(req, ingredient))) return false;
					}
				} else {
					return false;
				}
			}
			if (tiles is not null) {
				if (recipe.requiredTile.Count == tiles.Length) {
					for (int i = 0; i < ingredients.Length; i++) {
						if (!recipe.requiredTile.Contains(tiles[i])) return false;
					}
				} else {
					return false;
				}
			}
			return true;
		}
	}
}
