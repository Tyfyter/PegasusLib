using System.Collections.Concurrent;
using System.Linq;
using Terraria.Localization;

namespace PegasusLib {
	public class LanguageTree(LocalizedText value = default) : ConcurrentDictionary<string, LanguageTree> {
		public LocalizedText value = value;
		public string TextValue => value.Value;
		public LocalizedText[] Children => Values.Select(tree => tree.value).ToArray();
		public LanguageTree GetOrCreate(string key) => GetOrAdd(key, _ => new());
		public LanguageTree Find(string path, bool create = true) {
			int splitIndex = path.IndexOf('.');
			if (!create) {
				string step = splitIndex == -1 ? path : path[..splitIndex];
				if (!TryGetValue(step, out LanguageTree next)) return null;
				if (splitIndex == -1) return next;
				return next.Find(path[(splitIndex + 1)..], false);
			}
			if (splitIndex == -1) return GetOrCreate(path);
			return GetOrCreate(path[..splitIndex]).Find(path[(splitIndex + 1)..]);
		}
	}
}
