using PegasusLib.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.IO;

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
		public static ReverseEntityGlobalsEnumerator<TGlobal> EnumerateReverse<TGlobal>(this GlobalHookList<TGlobal> hookList, IEntityWithGlobals<TGlobal> entity) where TGlobal : GlobalType<TGlobal> {
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
		public delegate bool TryGetter<TSource, TResult>(TSource source, out TResult result);
		public static IEnumerable<TResult> TrySelect<TSource, TResult>(this IEnumerable<TSource> source, TryGetter<TSource, TResult> tryGetter) {
			foreach (TSource item in source) {
				if (tryGetter(item, out TResult result)) yield return result;
			}
		}
		public static Projectile SpawnProjectile(this ModPlayer self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) =>
			SpawnProjectile(self.Player, spawnSource, position, velocity, type, damage, knockback, ai0, ai1, ai2);
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
		public static Projectile SpawnProjectile(this ModProjectile self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) =>
			SpawnProjectile(self.Projectile, spawnSource, position, velocity, type, damage, knockback, ai0, ai1, ai2);
		public static Projectile SpawnProjectile(this Projectile self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
			if (self.owner != Main.myPlayer) return null;
			spawnSource ??= self.GetSource_FromAI();
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
		public static Projectile SpawnProjectile(this ModNPC self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) =>
			SpawnProjectile(self.NPC, spawnSource, position, velocity, type, damage, knockback, ai0, ai1, ai2);
		public static Projectile SpawnProjectile(this NPC self, IEntitySource spawnSource, Vector2 position, Vector2 velocity, int type, int damage, float knockback, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return null;
			spawnSource ??= self.GetSource_FromAI();
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
		public static NPC SpawnNPC(this ModNPC self, IEntitySource spawnSource, int x, int y, int type, int start = 0, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float ai3 = 0f, int target = 255) =>
			SpawnNPC(self.NPC, spawnSource, x, y, type, start, ai0, ai1, ai2, ai3, target);
		public static NPC SpawnNPC(this NPC self, IEntitySource spawnSource, int x, int y, int type, int start = 0, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float ai3 = 0f, int target = 255) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return null;
			spawnSource ??= self.GetSource_FromAI();
			return NPC.NewNPCDirect(spawnSource, x, y, type, start, ai0, ai1, ai2, ai3, target);
		}
		public static bool TrySet<T>(ref this T value, T newValue) where T : struct => PegasusLib.TrySet(ref value, newValue);
		public static bool Cooldown(ref this float value, float to = 0, float rate = 1) => value.Cooldown<float>(to, rate);
		public static bool Cooldown(ref this int value, int to = 0, int rate = 1) => value.Cooldown<int>(to, rate);
		public static bool Cooldown<N>(ref this N value, N to, N rate) where N : struct, INumber<N> {
			if (value > to) {
				value -= rate;
				if (value <= to) {
					value = to;
					return true;
				}
			}
			return false;
		}
		public static bool Warmup(ref this float value, float to, float rate = 1) => value.Warmup<float>(to, rate);
		public static bool Warmup(ref this int value, int to, int rate = 1) => value.Warmup<int>(to, rate);
		public static bool Warmup<N>(ref this N value, N to, N rate) where N : struct, INumber<N> {
			if (value < to) {
				value += rate;
				if (value >= to) {
					value = to;
					return true;
				}
			}
			return false;
		}
		public static bool CycleDown(ref this float value, float from, float rate = 1) => value.CycleDown<float>(from, rate);
		public static bool CycleDown(ref this int value, int from, int rate = 1) => value.CycleDown<int>(from, rate);
		public static bool CycleDown<N>(ref this N value, N from, N rate) where N : struct, INumber<N> {
			value -= rate;
			if (value >= N.Zero) {
				value += from;
				return true;
			}
			return false;
		}
		public static bool CycleUp(ref this float value, float to, float rate = 1) => value.CycleUp<float>(to, rate);
		public static bool CycleUp(ref this int value, int to, int rate = 1) => value.CycleUp<int>(to, rate);
		public static bool CycleUp<N>(ref this N value, N to, N rate) where N : struct, INumber<N> {
			value += rate;
			if (value >= to) {
				value -= to;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Pushes the contents of <paramref name="pushIn"/> into the beginning <paramref name="array"/><br/>
		/// Elements at the end of <paramref name="array"/> are removed to make room
		/// </summary>
		public static void Roll<T>(this T[] array, params T[] pushIn) {
			if (pushIn.Length == 0) return;
			for (int i = array.Length - 1; i >= 0; i--) {
				int index = i - pushIn.Length;
				array[i] = array.IndexInRange(index) ? array[index] : pushIn[i];
			}
		}
		public static T GetIfInRange<T>(this T[] array, int index, T fallback = default) {
			if (!array.IndexInRange(index)) return fallback;
			return array[index];
		}
		public static T GetIfInRange<T>(this T[,] array, int i, int j, T fallback = default) {
			if (!array.IndexInRange(i, j)) return fallback;
			return array[i, j];
		}
		public static bool IndexInRange<T>(this T[,] array, int i, int j) {
			return i >= 0
				&& j >= 0
				&& i < array.GetLength(0)
				&& j < array.GetLength(1);
		}
		public static T GetIfInRange<T>(this List<T> array, int index, T fallback = default) {
			if (!array.IndexInRange(index)) return fallback;
			return array[index];
		}
		public static T GetIfInRange<T>(this IReadOnlyList<T> array, int index, T fallback = default) {
			if (index < 0 || index >= array.Count) return fallback;
			return array[index];
		}
		public static IEnumerable<T> Select<T>(this Range range, Func<int, T> func) {
			if (range.Start.IsFromEnd || range.End.IsFromEnd) throw new ArgumentException("Func select does not support indexing from end", nameof(range));
			for (int i = range.Start.Value; i < range.End.Value; i++) yield return func(i);
		}
		/// <summary>
		/// Returns false if the exception was successfully attributed to the responsible mod, <see langword="throw"/> if this returns true
		/// </summary>
		public static bool AttributeTo(this Exception exception, Mod mod) {
			if (!PegasusLib.canAttributeErrors) return true;
			PegasusLib.attributedErrors.Add(mod, exception);
			return false;
		}
	}
}
