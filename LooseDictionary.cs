using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PegasusLib {
	public delegate bool KeyLoosener<T>(ref T key, T originalKey);
	public static class LooseDictionary {
		public static bool ParentType(ref Type key, Type originalKey) {
			key = key.BaseType;
			return key is not null;
		}
	}
	public class LooseDictionary<TKey, TValue>(KeyLoosener<TKey> loosener) : Dictionary<TKey, TValue> {
		public new TValue this[TKey key] {
			get {
				TKey originalKey = key;
				do {
					if (base.TryGetValue(key, out TValue value)) return value;
				} while (loosener(ref key, originalKey));
				throw new KeyNotFoundException($"The given key '{key}' was not present in the dictionary.");
			}
			set => base[key] = value;
		}
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value, out int levels) {
			TKey originalKey = key;
			levels = 0;
			do {
				if (base.TryGetValue(key, out value)) return true;
				levels++;
			} while (loosener(ref key, originalKey));

			value = default;
			return false;
		}
		public new bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out value, out _);
	}
}
