using System;
using System.Collections;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PegasusLib;
/// <summary>
/// <para/>Should only be used when a mod must behave in a manner which cannot be expected or accounted for in order for an exception to be thrown
/// <para/>Examples include calling <see cref="DamageClass.CountsAsClass"/> in <see cref="DamageClass.GetEffectInheritance"/>, or throwing an exception in <see cref="IItemDropRule.ReportDroprates(List{DropRateInfo}, DropRateInfoChainFeed)"/>
/// </summary>
[Serializable]
public class AttributedException(Exception inner, Mod mod, string message = null) : Exception(null, inner) {
	public override string Message => message is null ? InnerException.Message : $"{message}: {InnerException.Message}";
	public override IDictionary Data { get; } = new AttributingDictionary(inner.Data, mod);
	public override string HelpLink { get => InnerException.HelpLink; set => InnerException.HelpLink = value; }
	public override string Source { get => InnerException.Source; set => InnerException.Source = value; }
	readonly struct AttributingDictionary : IDictionary {
		private readonly IDictionary dictionary;
		public AttributingDictionary(IDictionary dictionary, Mod mod) {
			this.dictionary = dictionary;
			dictionary["mod"] = mod.Name;
		}
		public readonly object this[object key] {
			get => dictionary[key];
			set {
				if (key is string _key && _key == "mod") return;
				dictionary[key] = value;
			}
		}
		public bool IsFixedSize => dictionary.IsFixedSize;
		public bool IsReadOnly => dictionary.IsReadOnly;
		public ICollection Keys => dictionary.Keys;
		public ICollection Values => dictionary.Values;
		public int Count => dictionary.Count;
		public bool IsSynchronized => dictionary.IsSynchronized;
		public object SyncRoot => dictionary.SyncRoot;
		public void Add(object key, object value) => dictionary.Add(key, value);
		public void Clear() => dictionary.Clear();
		public bool Contains(object key) => dictionary.Contains(key);
		public void CopyTo(Array array, int index) => dictionary.CopyTo(array, index);
		public IDictionaryEnumerator GetEnumerator() => dictionary.GetEnumerator();
		public void Remove(object key) => dictionary.Remove(key);
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)dictionary).GetEnumerator();
	}
}
