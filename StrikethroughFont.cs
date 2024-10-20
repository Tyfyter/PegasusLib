using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Reflection;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PegasusLib {
	public static class StrikethroughFont {
		internal class FontInfoLoader : ILoadable {
			public void Load(Mod mod) { }
			public void Unload() {
				strikethroughFont = null;
			}
		}
		static FieldInfo _spriteCharacters;
		static FieldInfo _SpriteCharacters => _spriteCharacters ??= typeof(DynamicSpriteFont).GetField("_spriteCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo _defaultCharacterData;
		static FieldInfo _DefaultCharacterData => _defaultCharacterData ??= typeof(DynamicSpriteFont).GetField("_defaultCharacterData", BindingFlags.NonPublic | BindingFlags.Instance);
		static DynamicSpriteFont strikethroughFont;
		public static DynamicSpriteFont Font {
			get {
				if (PlayerInput.Triggers.JustPressed.Down) strikethroughFont = null;
				if (strikethroughFont is null) {
					if (FontAssets.MouseText.IsLoaded) {
						Texture2D strikeTexture = ModContent.Request<Texture2D>("PegasusLib/Textures/Strikethrough_Font", AssetRequestMode.ImmediateLoad).Value;
						DynamicSpriteFont baseFont = FontAssets.MouseText.Value;
						strikethroughFont = new DynamicSpriteFont(baseFont.CharacterSpacing, baseFont.LineSpacing, baseFont.DefaultCharacter);
						Type dict = _SpriteCharacters.FieldType;
						_SpriteCharacters.SetValue(
							strikethroughFont,
							dict.GetConstructor([typeof(IDictionary<,>).MakeGenericType(dict.GenericTypeArguments)])
							.Invoke([_SpriteCharacters.GetValue(baseFont)])
						);
						object enumerator = dict.GetMethod(nameof(Dictionary<int, int>.GetEnumerator)).Invoke(_SpriteCharacters.GetValue(baseFont), []);
						Type enumType = enumerator.GetType();
						MethodInfo moveNext = enumType.GetMethod(nameof(Dictionary<int, int>.Enumerator.MoveNext));
						PropertyInfo current = enumType.GetProperty(nameof(Dictionary<int, int>.Enumerator.Current));
						PropertyInfo key = typeof(KeyValuePair<,>).MakeGenericType(dict.GenericTypeArguments).GetProperty(nameof(KeyValuePair<int, int>.Key));
						PropertyInfo prop = dict.GetProperty("Item");
						object sfFont = _SpriteCharacters.GetValue(strikethroughFont);

						Type spriteCharacterData = dict.GenericTypeArguments[1];
						ConstructorInfo ctor = spriteCharacterData.GetConstructors()[0];
						FieldInfo glyphField = spriteCharacterData.GetField("Glyph");
						FieldInfo paddingField = spriteCharacterData.GetField("Padding");
						FieldInfo kerningField = spriteCharacterData.GetField("Kerning");
						while ((bool)moveNext.Invoke(enumerator, [])) {
							object[] index = [key.GetValue(current.GetValue(enumerator))];
							object value = prop.GetValue(sfFont, index);
							Rectangle glyph = (Rectangle)glyphField.GetValue(value);
							Rectangle padding = (Rectangle)paddingField.GetValue(value);
							Vector3 kerning = (Vector3)kerningField.GetValue(value);
							padding.X = -4;
							padding.Y = 0;
							padding.Height = 0;
							glyph.X = 0;
							glyph.Y = -8;// 2 - glyph.Height / 2;
							glyph.Width += (int)(kerning.Y + kerning.Z + 4f);
							glyph.Height = 16;
							prop.SetValue(sfFont, ctor.Invoke([
								strikeTexture,
								glyph,
								padding,
								kerning,
							]), index);
						}
						_DefaultCharacterData.SetValue(strikethroughFont, _DefaultCharacterData.GetValue(baseFont));
					} else {
						return FontAssets.MouseText.Value;
					}
				}
				return strikethroughFont;
			}
		}
	}
}
