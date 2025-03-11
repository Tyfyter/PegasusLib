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
		public static SnippetOption CreateFloatOption(string name, Action<float> setter) {
			return new(name, "[\\d\\.]+", match => {
				setter(int.Parse(match));
			});
		}
		public static SnippetOption CreateIntOption(string name, Action<int> setter) {
			return new(name, "[\\d]+", match => {
				setter(int.Parse(match));
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
