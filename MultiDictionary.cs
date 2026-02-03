using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PegasusLib {
	public class MultiDictionary<TKey, TValue>(IEqualityComparer<TKey> comparer = null) : IDictionary<TKey, IEnumerable<TValue>> {
		readonly Dictionary<TKey, List<TValue>> inner = new(comparer);

		public IEnumerable<TValue> this[TKey key] {
			get => inner[key];
			set => throw new InvalidOperationException("MultiDictionary does not support this.set");
		}

		public ICollection<TKey> Keys => inner.Keys;
		ICollection<IEnumerable<TValue>> IDictionary<TKey, IEnumerable<TValue>>.Values => throw new InvalidOperationException("MultiDictionary does not support this.set");
		public int Count => inner.Count;
		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).IsReadOnly;
		public void Add(TKey key, TValue value) {
			if (!inner.TryGetValue(key, out List<TValue> values)) inner[key] = values = [];
			values.Add(value);
		}
		public void Add(TKey key, IEnumerable<TValue> value) {
			if (!inner.TryGetValue(key, out List<TValue> values)) inner[key] = values = [];
			values.AddRange(value);
		}
		void ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>.Add(KeyValuePair<TKey, IEnumerable<TValue>> item) {
			((ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).Add(item);
		}
		public void Clear() => inner.Clear();
		bool ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>.Contains(KeyValuePair<TKey, IEnumerable<TValue>> item) {
			return ((ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).Contains(item);
		}
		public bool ContainsKey(TKey key) => inner.ContainsKey(key);
		void ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>.CopyTo(KeyValuePair<TKey, IEnumerable<TValue>>[] array, int arrayIndex) {
			((ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).CopyTo(array, arrayIndex);
		}

		IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>.GetEnumerator() {
			return ((IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).GetEnumerator();
		}
		public bool Remove(TKey key) => inner.Remove(key);
		bool ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>.Remove(KeyValuePair<TKey, IEnumerable<TValue>> item) {
			return ((ICollection<KeyValuePair<TKey, IEnumerable<TValue>>>)inner).Remove(item);
		}
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out IEnumerable<TValue> value) {
			if (inner.TryGetValue(key, out List<TValue> values)) {
				value = values;
				return true;
			}
			value = null;
			return false;
		}
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)inner).GetEnumerator();
	}
}
