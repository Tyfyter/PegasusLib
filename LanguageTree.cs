using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria.Localization;

namespace PegasusLib {
	[DebuggerDisplay("\\{[{value}, Count = {Count}]\\}")]
	public class LanguageTree(LocalizedText value = default) : ConcurrentDictionary<string, LanguageTree> {
		public LocalizedText value = value;
		public string TextValue => value.Value;
		public LocalizedText[] Children => Values.Select(tree => tree.value).ToArray();
		public LocalizedText[] Descendants => this.GetDescendants().Select(tree => tree.value).ToArray();
		string KeyPrefix => value is null ? "" : $"{value.Key}.";
		public LanguageTree GetOrCreate(string key) => GetOrAdd(key, _ => new(TextUtils.CreateSelfLocalization(KeyPrefix + key)));
		public LanguageTree Find(string path, bool doFallback = true) {
			int splitIndex = path.IndexOf('.');
			bool isEnd = splitIndex == -1;
			string searchPath = isEnd ? path : path[..splitIndex];
			if (TryGetValue(searchPath, out LanguageTree next)) {
				if (splitIndex == -1) return next;
				return next.Find(path[(splitIndex + 1)..], doFallback);
			} else if (doFallback) {
				return TextUtils.FallbackLanguageTree.Find(KeyPrefix + path, false);
			}
			return null;
		}
		public LanguageTree FindOrCreate(string path) {
			int splitIndex = path.IndexOf('.');
			if (splitIndex == -1) return GetOrCreate(path);
			return GetOrCreate(path[..splitIndex]).FindOrCreate(path[(splitIndex + 1)..]);
		}
		public string Format(params string[] args) => value.Format(args);
		public LocalizedText WithFormatArgs(params string[] args) => value.WithFormatArgs(args);
		public override string ToString() => value.ToString();
	}
}
