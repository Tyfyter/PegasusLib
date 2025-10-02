using Microsoft.CodeAnalysis.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using static Mono.CompilerServices.SymbolWriter.CodeBlockEntry;
using static ReLogic.Graphics.DynamicSpriteFont;
using static System.Net.Mime.MediaTypeNames;

namespace PegasusLib {
	public record struct SnippetOption(string Name, [StringSyntax(StringSyntaxAttribute.Regex)] string Data, Action<string> Action) {
		public readonly string Pattern => Name + Data;
		/// <summary>
		/// Creates an option which parses a color as an 8, 6, 4, or 3 digit hex code
		/// </summary>
		public static SnippetOption CreateColorOption(string name, Action<Color> setter) {
			return new(name, "[\\da-fA-F]{3,8}", match => {
				int Parse(int index, int size) {
					int startIndex = (index * size);
					return Convert.ToInt32(match[startIndex..(startIndex + size)], 16);
				}
				switch (match.Length) {
					case 8:
					setter(new Color(Parse(0, 2), Parse(1, 2), Parse(2, 2), Parse(3, 2)));
					break;
					case 6:
					setter(new Color(Parse(0, 2), Parse(1, 2), Parse(2, 2)));
					break;
					case 4:
					setter(new Color(Parse(0, 1) * 16 - 1, Parse(1, 1) * 16 - 1, Parse(2, 1) * 16 - 1, Parse(3, 1) * 16 - 1));
					break;
					case 3:
					setter(new Color(Parse(0, 1) * 16 - 1, Parse(1, 1) * 16 - 1, Parse(2, 1) * 16 - 1));
					break;
					default:
					throw new FormatException($"Malformed color code {match}");
				}
			});
		}
		/// <summary>
		/// Creates an option which parses a floating point number
		/// </summary>
		public static SnippetOption CreateFloatOption(string name, Action<float> setter) {
			return new(name, "-?[\\d\\.]+", match => {
				setter(float.Parse(match));
			});
		}
		/// <summary>
		/// Creates an option which parses an integer
		/// </summary>
		public static SnippetOption CreateIntOption(string name, Action<int> setter) {
			return new(name, "-?[\\d]+", match => {
				setter(int.Parse(match));
			});
		}
		/// <summary>
		/// Creates an option which parses a floating point number
		/// </summary>
		public static SnippetOption CreateFloatsOption(string name, Range count, Action<float[]> setter) {
			static string P([StringSyntax(StringSyntaxAttribute.Regex)] params string[] parts) => string.Concat(parts);
			return new(name, P("(?:-?[\\d\\.]+,?)", RangeToRegex(count)), match => {
				setter(match.Split(',').Select(float.Parse).ToArray());
			});
		}
		/// <summary>
		/// Creates an option which parses an integer
		/// </summary>
		public static SnippetOption CreateIntsOption(string name, Range count, Action<int[]> setter) {
			static string P([StringSyntax(StringSyntaxAttribute.Regex)] params string[] parts) => string.Concat(parts);
			return new(name, P("(?:-?[\\d]+,?)", RangeToRegex(count)), match => {
				setter(match.Split(',').Select(int.Parse).ToArray());
			});
		}
		/// <summary>
		/// Creates an option which parses a Vector2
		/// </summary>
		public static SnippetOption CreateVector2Option(string name, Action<Vector2> setter) {
			return new(name, "(?:-?[\\d\\.]+,)-?[\\d\\.]+", match => {
				string[] args = match.Split(',');
				setter(new(float.Parse(args[0]), float.Parse(args[1])));
			});
		}
		/// <summary>
		/// Creates an option which parses a Rectangle
		/// </summary>
		public static SnippetOption CreateRectangleOption(string name, Action<Rectangle> setter) {
			return new(name, "(?:\\d+,){3}\\d+", match => {
				string[] args = match.Split(',');
				setter(new(int.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3])));
			});
		}
		/// <summary>
		/// Creates an option which parses a string terminated with any of the specified delimiters
		/// </summary>
		public static SnippetOption CreateStringOption(string name, Action<string> setter, params char[] delimiters) {
			string allDelimiters = $"{string.Join<char>("\\", ['/', ..delimiters])}";
			return new(name, $"[^{allDelimiters}]+[{allDelimiters}]?", match => {
				if (Regex.IsMatch(match, $"[{allDelimiters}]$")) match = match[..^1];
				setter(match);
			});
		}
		/// <summary>
		/// Creates an option which sets a flag
		/// </summary>
		public static SnippetOption CreateFlagOption(string name, Action setter) {
			return new(name, "", _ => {
				setter();
			});
		}
		public static string RangeToRegex(Range range) => $"{{{(range.Start.Equals(Index.Start) ? "" : range.Start.Value)},{(range.End.Equals(Index.End) ? "" : range.End.Value)}}}";
	}
	public static class SnippetHelper {
		public static void ParseOptions(this string optionsText, params SnippetOption[] options) {
			Regex regex = new($"(?:{string.Join("|", options.Select(so => $"({so.Pattern})"))})*");
			GroupCollection groups = regex.Match(optionsText).Groups;
			for (int i = 0; i < groups.Count - 1; i++) {
				string match = groups[i + 1].Value;
				if (match.Length <= 0) continue;
				options[i].Action(match[options[i].Name.Length..]);
			}
		}
	}
	public abstract class AdvancedTextSnippetHandler<TOptions> : ITagHandler, ILoadable, ILocalizedModType where TOptions : new() {
		public abstract IEnumerable<string> Names { get; }
		static readonly FastStaticFieldInfo<ConcurrentDictionary<string, ITagHandler>> _handlers = new(typeof(ChatManager), "_handlers");
		public Mod Mod { get; private set; }
		public string LocalizationCategory => "AdvancedTextSnippet";
		public string Name => GetType().Name;
		public string FullName => $"{Mod.Name}/{Name}";
		public void Load(Mod mod) {
			Mod = mod;
			foreach (string name in Names) {
				_handlers.Value[name.ToLower()] = this;
			}
			if (NetmodeActive.Server) return;
			if (ModLoader.TryGetMod("ChatPlus", out Mod chatPlus)) {
				foreach (string name in Names) {
					chatPlus.Call("RegisterTagProvider",
						name,
						GetSuggestionsWithName(name)
					);
				}
			}
			foreach (SnippetOption option in GetOptions()) {
				this.GetLocalization("Option_" + option.Name);
			}
		}
		protected TOptions options;
		string currentName;
		Func<string, IEnumerable<(string, UIElement)>> GetSuggestionsWithName(string name) {
			return Iterator;
			IEnumerable<(string, UIElement)> Iterator(string typed) {
				currentName = name;
				foreach ((string tag, UIElement view) in GetSuggestions(typed)) {
					yield return ($"[{currentName}{tag}", view);
				}
			}
		}
		public virtual IEnumerable<(string, UIElement)> GetSuggestions(string typed) => SuggestOptions(typed);
		public IEnumerable<(string, UIElement)> SuggestOptions(string typed) {
			if (typed.Contains(':')) yield break;
			if (!typed.StartsWith('/')) typed = "/" + typed;
			SnippetOption[] options = GetOptions().ToArray();
			Regex typingOptionRegex = new($"(?:{string.Join("|", options.TrySelect((SnippetOption option, out string result) => {
				result = $"{option.Name}";
				return !string.IsNullOrEmpty(option.Data);
			}))})$");
			if (typingOptionRegex.IsMatch(typed)) yield break;
			Regex regex = new($"(?:{string.Join("|", options.Select(so => $"({so.Pattern})"))})*");
			GroupCollection groups = regex.Match(typed[1..]).Groups;
			for (int i = 0; i < groups.Count - 1; i++) {
				string match = groups[i + 1].Value;
				if (match.Length > 0) continue;
				yield return (typed + options[i].Name, CreateText(this.GetLocalization("Option_" + options[i].Name)));
			}
		}
		static UIElement CreateText(LocalizedText text) {
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text.Value);
			return new UIText(text) {
				Width = new(size.X, 0),
				MinWidth = new(size.X, 0),
				Height = new(size.Y, 0),
				MinHeight = new(size.Y, 0)
			};
		}
		/// <summary>
		/// Set <see cref="options"/> in the <see cref="SnippetOption"/>s returned here
		/// </summary>
		public abstract IEnumerable<SnippetOption> GetOptions();
		public abstract TextSnippet Parse(string text, Color baseColor, TOptions options);
		public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
			this.options = new();
			SnippetHelper.ParseOptions(options,
				GetOptions().ToArray()
			);
			return Parse(text, baseColor, this.options);
		}
		public void Unload() { }
	}
	/*public class Waggle_Handler : AdvancedTextSnippetHandler<Waggle_Handler.Options> {
		public override IEnumerable<string> Names => ["waggle"];
		public class Wiggle_Snippet(string text, Options options, Color color = default, float scale = 1) : WrappingTextSnippet(text, color, scale) {
			public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1) {
				if (justCheckingString || spriteBatch is null) {
					size = default;
					return false;
				}
				size = FontAssets.MouseText.Value.MeasureString(Text) * scale * Vector2.UnitX;
				static SpriteCharacterData GetCharacterData(char character) {
					if (!FontAssets.MouseText.Value.SpriteCharacters.TryGetValue(character, out SpriteCharacterData value)) {
						return FontAssets.MouseText.Value.DefaultCharacterData;
					}
					return value;
				}
				Vector2 zero = default;
				foreach (char c in Text) {
					SpriteCharacterData characterData = GetCharacterData(c);
					Vector3 kerning = characterData.Kerning;
					Rectangle padding = characterData.Padding;
					zero.X += FontAssets.MouseText.Value.CharacterSpacing * scale;
					zero.X += kerning.X * scale;
					Vector2 pos = zero;
					pos.X += padding.X * scale;
					pos.Y += padding.Y * scale;
					pos.Y += (float)Math.Sin(zero.X / (options.WiggleWidth * scale) + Main.timeForVisualEffects * options.Speed) * options.WiggleScale;
					spriteBatch.Draw(characterData.Texture, pos + position, characterData.Glyph, color, 0, Origin, scale, default, 0);
					zero.X += (kerning.Y + kerning.Z) * scale;
				}
				return true;
			}
		}
		public record struct Options(float Speed = 1f / 60f, float WiggleWidth = 16, float WiggleScale = 2) {
			public Options() : this(1f / 60f) { }
		}
		public override TextSnippet Parse(string text, Color baseColor, Options options) {
			return new Wiggle_Snippet(text, options, baseColor, 1);
		}
		public override IEnumerable<SnippetOption> GetOptions() {
			yield return SnippetOption.CreateFloatOption("t", v => options.Speed = v);
			yield return SnippetOption.CreateFloatOption("x", v => options.WiggleWidth = v);
			yield return SnippetOption.CreateFloatOption("y", v => options.WiggleScale = v);
		}
	}*/
}
