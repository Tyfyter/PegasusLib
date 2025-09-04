using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
			return new(name, "[\\d\\.]+", match => {
				setter(float.Parse(match));
			});
		}
		/// <summary>
		/// Creates an option which parses an integer
		/// </summary>
		public static SnippetOption CreateIntOption(string name, Action<int> setter) {
			return new(name, "[\\d]+", match => {
				setter(int.Parse(match));
			});
		}
		/// <summary>
		/// Creates an option which parses a Vector2
		/// </summary>
		public static SnippetOption CreateVector2Option(string name, Action<Vector2> setter) {
			return new(name, "(?:[\\d\\.]+,)[\\d\\.]+", match => {
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
	}
	public static class SnippetHelper {
		public static void ParseOptions(this string optionsText, params SnippetOption[] options) {
			Regex regex = new($"(?:{string.Join("|", options.Select(so => $"({so.Pattern})"))})+");
			GroupCollection groups = regex.Match(optionsText).Groups;
			for (int i = 0; i < groups.Count - 1; i++) {
				string match = groups[i + 1].Value;
				if (match.Length <= 0) continue;
				options[i].Action(match[options[i].Name.Length..]);
			}
		}
	}
}
