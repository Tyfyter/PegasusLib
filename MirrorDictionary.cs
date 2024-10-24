using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PegasusLib {
	public class MirrorDictionary<T> : IDictionary<T, T> {
		public T this[T key] {
			get {
				return key;
			}
			set { }
		}

		public ICollection<T> Keys { get; }
		public ICollection<T> Values { get; }
		public int Count { get; }
		public bool IsReadOnly => true;
		public void Add(T key, T value) {
			throw new NotImplementedException();
		}

		public void Add(KeyValuePair<T, T> item) {
			throw new NotImplementedException();
		}

		public void Clear() {
			throw new NotImplementedException();
		}

		public bool Contains(KeyValuePair<T, T> item) => item.Value.Equals(item.Key);
		public bool ContainsKey(T key) => true;

		public void CopyTo(KeyValuePair<T, T>[] array, int arrayIndex) {
			throw new NotImplementedException();
		}
		public IEnumerator<KeyValuePair<T, T>> GetEnumerator() {
			throw new NotImplementedException();
		}
		public bool Remove(T key) {
			throw new NotImplementedException();
		}
		public bool Remove(KeyValuePair<T, T> item) {
			throw new NotImplementedException();
		}
		public bool TryGetValue(T key, [MaybeNullWhen(false)] out T value) {
			value = key;
			return true;
		}
		IEnumerator IEnumerable.GetEnumerator() {
			throw new NotImplementedException();
		}
	}
}
