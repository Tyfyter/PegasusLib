using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.ModLoader.Core;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PegasusLib.Reflection;
using System.Collections;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace PegasusLib {
	public static class PegasusExt {
		/// <typeparam name="T"></typeparam>
		/// <param name="self"></param>
		/// <param name="key"></param>
		/// <param name="fallback"></param>
		/// <returns>The value of <paramref name="key"/> in <paramref name="self"/>, or <paramref name="fallback"/> if <paramref name="self"/> does not contain <paramref name="key"/></returns>
		public static T SafeGet<T>(this TagCompound self, string key, T fallback = default) {
			return self.TryGet(key, out T output) ? output : fallback;
		}
		/// <summary>
		/// Gets the default texture path of this type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetDefaultTMLName(this Type type) => (type.Namespace + "." + type.Name).Replace('.', '/');
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns>Every flag <paramref name="value"/> matches</returns>
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
		public static ReverseEntityGlobalsEnumerator<TGlobal> EnumerateReverse<TGlobal>(this GlobalHookList<TGlobal> hookList,  IEntityWithGlobals<TGlobal> entity) where TGlobal : GlobalType<TGlobal> {
			return new ReverseEntityGlobalsEnumerator<TGlobal>(GlobalHookListMethods<TGlobal>.ForType(hookList, entity.Type), entity);
		}
		public static void InsertOrdered<T>(this IList<T> list, T item, IComparer<T> comparer = null) {
			comparer ??= Comparer<T>.Default;
			for (int i = 0; i < list.Count + 1; i++) {
				if (i == list.Count || comparer.Compare(list[i], item) > 0) {
					list.Insert(i, item);
					return;
				}
			}
		}
		public static IEnumerable<TResult> TryCast<TResult>(this IEnumerable source) {
			if (source is IEnumerable<TResult> typedSource) return typedSource;
			ArgumentNullException.ThrowIfNull(source);
			return TryCastIterator<TResult>(source);
		}

		private static IEnumerable<TResult> TryCastIterator<TResult>(IEnumerable source) {
			foreach (object obj in source) {
				if (obj is TResult result) yield return result;
			}
		}

		public static Projectile SpawnProjectile(this Projectile self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
			if (self.owner != Main.myPlayer) return null;
			return Projectile.NewProjectileDirect(
				spawnSource,
				position,
				velocity,
				type,
				damage,
				knockback,
				Main.myPlayer,
				ai0,
				ai1,
				ai2
			);
		}
		public static Projectile SpawnProjectile(this Player self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
			if (self.whoAmI != Main.myPlayer) return null;
			return Projectile.NewProjectileDirect(
				spawnSource,
				position,
				velocity,
				type,
				damage,
				knockback,
				Main.myPlayer,
				ai0,
				ai1,
				ai2
			);
		}
		public static Projectile SpawnProjectile(this NPC self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return null;
			return Projectile.NewProjectileDirect(
				spawnSource,
				position,
				velocity,
				type,
				damage,
				knockback,
				Main.myPlayer,
				ai0,
				ai1,
				ai2
			);
		}
	}
}
