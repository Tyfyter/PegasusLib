using Hjson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PegasusLib {
	public static class TextUtils {
		public static LanguageTree LanguageTree { get; private set; }
		public static LanguageTree FallbackLanguageTree { get; private set; }
		static Task taskWaiter;
		internal static void LoadTranslations() {
			CreateSelfLocalization ??= CreateLocalizationCreator();
			LanguageTree = [];
			bool generateFallback = FallbackLanguageTree is null;
			if (generateFallback) FallbackLanguageTree = [];
			List<Task> tasks = [];
			foreach (Mod mod in ModLoader.Mods) {
				tasks.Add(Task.Run(() => {
					foreach (string name in mod.GetFileNames() ?? []) {
						if (Path.GetExtension(name) != ".hjson") continue;
						if (!LocalizationLoader.TryGetCultureAndPrefixFromPath(name, out GameCulture fileCulture, out string prefix)) continue;
						if (fileCulture == Language.ActiveCulture) ProcessLocalizationFile(false, mod, name, prefix);
						if (generateFallback && fileCulture == GameCulture.DefaultCulture) ProcessLocalizationFile(true, mod, name, prefix);
					}
				}));
			}
			taskWaiter = Task.WhenAll(tasks);
		}
		/// <summary>
		///IL_0010: ldarg.0
		///IL_0011: ldarg.0
		///IL_0012: newobj instance void Terraria.Localization.LocalizedText::.ctor(string, string)
		///IL_0017: ret
		/// </summary>
		internal static Func<string, LocalizedText> CreateSelfLocalization;
		static Func<string, LocalizedText> CreateLocalizationCreator() => PegasusLib.Compile<Func<string, LocalizedText>>("LocalizationCreator",
			(OpCodes.Ldarg_0, null),
			(OpCodes.Ldarg_0, null),
			(OpCodes.Newobj, typeof(LocalizedText).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string), typeof(string)])),
			(OpCodes.Ret, null)
		);
		static void ProcessLocalizationFile(bool isFallback, Mod mod, string name, string prefix) {
			LanguageTree languageTree = isFallback ? FallbackLanguageTree : LanguageTree;
			using Stream stream = mod.GetFileStream(name);
			using StreamReader streamReader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
			string translationFileContents = streamReader.ReadToEnd();
			string jsonString;
			try {
				jsonString = HjsonValue.Parse(translationFileContents).ToString();
			} catch (Exception ex2) {
				string additionalContext = "";
				if (ex2 is ArgumentException) {
					Match match = Regex.Match(ex2.Message, "At line (\\d+),");
					if (match != null && match.Success && int.TryParse(match.Groups[1].Value, out var line)) {
						string[] lines = translationFileContents.Replace("\r", "").Replace("\t", "    ").Split('\n');
						int num = Math.Max(0, line - 4);
						int end = Math.Min(lines.Length, line + 3);
						StringBuilder linesOutput = new();
						for (int i = num; i < end; i++) {
							if (line - 1 == i) {
								linesOutput.Append($"\n{i + 1}[c/ff0000:>" + lines[i] + "]");
							} else {
								linesOutput.Append($"\n{i + 1}:" + lines[i]);
							}
						}
						additionalContext = "\nContext:" + linesOutput.ToString();
					}
				}
				throw new Exception($"The localization file \"{name}\" is malformed and failed to load:{additionalContext} ", ex2);
			}
			foreach (JToken t in JObject.Parse(jsonString).SelectTokens("$..*")) {
				if (!t.HasValues && !(t is JObject { Count: 0 })) {
					string path2 = "";
					JToken current = t;
					for (JToken parent = t.Parent; parent != null; parent = parent.Parent) {
						string text = ((parent is JProperty property) ? (property.Name + ((path2 == string.Empty) ? string.Empty : ("." + path2))) : ((parent is not JArray array) ? path2 : (array.IndexOf(current) + ((path2 == string.Empty) ? string.Empty : ("." + path2)))));
						path2 = text;
						current = parent;
					}
					path2 = path2.Replace(".$parentVal", "");
					if (!string.IsNullOrWhiteSpace(prefix)) path2 = prefix + "." + path2;
					languageTree.FindOrCreate(path2).value = Language.GetText(path2);
				}
			}
		}
		internal static void Unload() {
			LanguageTree = null;
			FallbackLanguageTree = null;
			taskWaiter = null;
		}
		public static string Format(string baseKey, IEnumerable<object> args) => Format(baseKey, args.ToArray());
		public static string Format(string baseKey, params object[] args) {
			taskWaiter.Wait();
			LanguageTree tree = LanguageTree.Find(baseKey);
			string format = null;
			if (tree.TryGetValue(args.Length.ToString(), out LanguageTree child)) {
				format = child.TextValue;
			} else {
				Regex modChecker = new("(-\\d+)?%(\\d+)");
				foreach (KeyValuePair<string, LanguageTree> item in tree) {
					Match modMatch = modChecker.Match(item.Key);
					if (modMatch.Length != item.Key.Length) continue;
					_ = int.TryParse(modMatch.Groups[1].Value, out int modOffset);
					if (int.TryParse(modMatch.Groups[2].Value, out int modulo)) {
						if ((args.Length + modOffset) % modulo == 0) {
							format = item.Value.TextValue;
							break;
						}
					}
				}
				if (format is null) {
					if (tree.TryGetValue("Any", out child)) {
						format = child.TextValue;
					} else {
						format = "";
					}
				}
			}
			Regex loop = new("\\[([^]]*)\\]");
			Regex substitution = new("{n}");
			if (loop.Match(format) is Match { Success: true } match) {
				int totalReplacements = substitution.Matches(format).Count;
				int perLoop = substitution.Matches(match.Groups[1].Value).Count;
				int neededLoopedSubs = (args.Length - (totalReplacements - perLoop));
				if (neededLoopedSubs % perLoop != 0) {
					return Language.GetTextValue("Mods.PegasusLib.PluralityFormatting.NoValidFormat", args.Length, baseKey);
				}
				format = match.Replace(format, string.Join("", Enumerable.Repeat(match.Groups[1].Value, neededLoopedSubs / perLoop)));
			}
			return substitution.Matches(format).Replace(format, i => args[i].ToString());
		}
		public static string Replace(this MatchCollection matches, string source, Func<int, string> replacement) {
			for (int i = matches.Count - 1; i >= 0; i--) {
				source = matches[i].Replace(source, replacement(i));
			}
			return source;
		}
		public static string Replace(this Match match, string source, string replacement) {
			return string.Concat(source.AsSpan(0, match.Index), replacement, source.AsSpan(match.Index + match.Length));
		}
	}
	internal class PluralityFormattingSystem : ModSystem {
		public override void OnLocalizationsLoaded() {
			TextUtils.LoadTranslations();
		}
	}
}
