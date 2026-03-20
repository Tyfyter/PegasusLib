using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Collections;

namespace PegasusLib {
	[DebuggerDisplay("Count = {Count}, Total = {Total}")]
	public class FungibleSet<T> : IDictionary<T, int> {
		public readonly EqualityComparer<T> KeyComparer;
		public readonly bool AddOnSet = false;

		public Dictionary<T, int> Entries { get; private set; }
		public int this[T key] {
			get => Entries.TryGetValue(key, out int value) ? value : 0;
			set {
				if (value > 0) {
					Entries[key] = value;
				} else {
					Entries.Remove(key);
				}
			}
		}
		public FungibleSet() {
			Entries = new Dictionary<T, int>();
		}
		public FungibleSet(Dictionary<T, int> entries) {
			Entries = entries;
		}
		public FungibleSet(IEnumerable<KeyValuePair<T, int>> entries) {
			Entries = new Dictionary<T, int>();
			foreach (KeyValuePair<T, int> entry in entries) {
				this[entry.Key] += entry.Value;
			}
		}
		public int Add(T key, int value = 1) => this[key] += value;
		public int Remove(T key, int value = 1) => -(this[key] -= value);
		public int Total => Entries.Values.Sum();
		public ICollection<T> Keys => Entries.Keys;
		public ICollection<int> Values => Entries.Values;
		public int Count => Entries.Count;
		public bool IsReadOnly => ((ICollection<KeyValuePair<T, int>>)Entries).IsReadOnly;
		void ICollection<KeyValuePair<T, int>>.Add(KeyValuePair<T, int> item) => ((ICollection<KeyValuePair<T, int>>)Entries).Add(item);
		public void Clear() => Entries.Clear();
		bool ICollection<KeyValuePair<T, int>>.Contains(KeyValuePair<T, int> item) => Entries.Contains(item);
		bool IDictionary<T, int>.ContainsKey(T key) => Entries.ContainsKey(key);
		void ICollection<KeyValuePair<T, int>>.CopyTo(KeyValuePair<T, int>[] array, int arrayIndex) => ((ICollection<KeyValuePair<T, int>>)Entries).CopyTo(array, arrayIndex);
		public IEnumerator<KeyValuePair<T, int>> GetEnumerator() => Entries.GetEnumerator();
		bool IDictionary<T, int>.Remove(T key) => Entries.Remove(key);
		bool ICollection<KeyValuePair<T, int>>.Remove(KeyValuePair<T, int> item) => ((ICollection<KeyValuePair<T, int>>)Entries).Remove(item);
		bool IDictionary<T, int>.TryGetValue(T key, out int value) => Entries.TryGetValue(key, out value);
		IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();
		void IDictionary<T, int>.Add(T key, int value) => Entries.Add(key, value);
	}
}