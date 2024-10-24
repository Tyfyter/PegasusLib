using System;
using System.Collections.Generic;

namespace PegasusLib {
	public class BidirectionalDictionary<TKey, TValue> {
		Dictionary<TKey, TValue> primary = new();
		Dictionary<TValue, TKey> reverse = new();
		public TValue this[TKey key] {
			get => GetValue(key);
			set {
				RemoveKey(key);
				Add(key, value);
			}
		}
		public void SetValue(TValue value, TKey key) {
			RemoveValue(value);
			Add(key, value);
		}
		/// <exception cref="ArgumentException">An element with the same key or value already exists</exception>
		public void Add(TKey key, TValue value) {
			if (ContainsKey(key)) throw new ArgumentException($"An element with the key \"{key}\" already exists", nameof(key));
			if (ContainsValue(value)) throw new ArgumentException($"An element with the value \"{value}\" already exists", nameof(value));
			primary.Add(key, value);
			reverse.Add(value, key);
		}
		public bool ContainsKey(TKey key) => primary.ContainsKey(key);
		public bool ContainsValue(TValue value) => reverse.ContainsKey(value);
		public TValue GetValue(TKey key) => primary[key];
		public TKey GetKey(TValue value) => reverse[value];
		public bool TryGetValue(TKey key, out TValue value) => primary.TryGetValue(key, out value);
		public bool TryGetKey(TValue value, out TKey key) => reverse.TryGetValue(value, out key);
		public bool RemoveKey(TKey key) {
			if (TryGetValue(key, out TValue value)) {
				primary.Remove(key);
				reverse.Remove(value);
				return true;
			}
			return false;
		}
		public bool RemoveValue(TValue value) {
			if (TryGetKey(value, out TKey key)) {
				reverse.Remove(value);
				primary.Remove(key);
				return true;
			}
			return false;
		}
	}

}
