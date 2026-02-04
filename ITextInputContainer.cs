using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using ReLogic.Graphics;
using ReLogic.OS;
using System;
using System.Text;
using Terraria;
using Terraria.GameInput;
using Terraria.UI.Chat;

namespace PegasusLib {
	public interface ITextInputContainer {
		public int CursorIndex { get; set; }
		public StringBuilder Text { get; }
		public string TextDisplay => Text.ToString();
		public void Submit() { }
		public void Reset() { }
	}
	public static class TextInputContainerExtensions {
		internal static void Load() {
			try {
				IL_Main.GetInputText += IL_Main_GetInputText;
			} catch (Exception e) {
				PegasusLib.FeatureError(LibFeature.ITextInputContainer, e);
			}
		}
		static void IL_Main_GetInputText(ILContext il) {
			ILCursor cur = new(il);
			ILLabel skipSpecial = cur.DefineLabel();
			cur.GotoNext(MoveType.AfterLabel,
				i => i.MatchLdsflda<Main>(nameof(Main.inputText)),//IL_006e: ldsflda valuetype [FNA]Microsoft.Xna.Framework.Input.KeyboardState Terraria.Main::inputText
				i => i.MatchLdcI4((int)Keys.Z),//IL_0073: ldc.i4 165
				i => i.MatchCall<KeyboardState>(nameof(KeyboardState.IsKeyDown))//IL_0078: call instance bool [FNA]Microsoft.Xna.Framework.Input.KeyboardState::IsKeyDown(valuetype [FNA]Microsoft.Xna.Framework.Input.Keys)
			);
			cur.EmitLdsfld(typeof(Main).GetField(nameof(Main.CurrentInputTextTakerOverride)));
			cur.EmitIsinst(typeof(ITextInputContainer));
			cur.EmitBrtrue(skipSpecial);
			cur.GotoNext(MoveType.After,
				i => i.MatchCall<Main>("PasteTextIn"),//IL_0147: call string Terraria.Main::PasteTextIn(bool, string)
				i => i.MatchStloc(out _)//IL_014c: stloc.1
			);
			cur.MarkLabel(skipSpecial);
		}
		public static void Copy(this ITextInputContainer container, bool cut = false) {
			Platform.Get<IClipboard>().Value = container.Text.ToString();
			if (cut) container.Clear();
		}
		public static void Paste(this ITextInputContainer container) {
			string clipboard = Platform.Get<IClipboard>().Value;
			container.Text.Insert(container.CursorIndex, clipboard);
			container.CursorIndex += clipboard.Length;
		}
		public static void Clear(this ITextInputContainer container) {
			container.Text.Clear();
			container.CursorIndex = 0;
		}
		public static void ProcessInput(this ITextInputContainer container, out bool typed) {
			typed = false;
			Main.CurrentInputTextTakerOverride = container;
			Main.chatRelease = false;
			PlayerInput.WritingText = true;
			Main.instance.HandleIME();
			string input = Main.GetInputText(" ", allowMultiLine: true);
			if (Main.inputText.PressingControl() || Main.inputText.PressingAlt()) {
				if (JustPressed(Keys.Z)) container.Clear();
				else if (JustPressed(Keys.X)) container.Copy(cut: true);
				else if (JustPressed(Keys.C)) container.Copy();
				else if (JustPressed(Keys.V)) container.Paste();
				else if (JustPressed(Keys.Left)) {
					if (container.CursorIndex <= 0) return;
					container.CursorIndex--;
					while (container.CursorIndex > 0 && container.Text[container.CursorIndex - 1] != ' ') {
						container.CursorIndex--;
					}
				} else if (JustPressed(Keys.Right)) {
					if (container.CursorIndex >= container.Text.Length) return;
					container.CursorIndex++;
					while (container.CursorIndex < container.Text.Length && container.Text[container.CursorIndex] != ' ') {
						container.CursorIndex++;
					}
				} else if (JustPressed(Keys.Back)) {
					if (container.CursorIndex <= 0) return;
					int length = 1;
					container.CursorIndex--;
					while (container.CursorIndex > 0 && container.Text[container.CursorIndex - 1] != ' ') {
						container.CursorIndex--;
						length++;
					}
					container.Text.Remove(container.CursorIndex, length);
				}
				typed = true;
				return;
			}
			if (Main.inputText.PressingShift()) {
				if (JustPressed(Keys.Delete)) {
					container.Copy(cut: true);
					typed = true;
					return;
				} else if (JustPressed(Keys.Insert)) {
					container.Paste();
					typed = true;
					return;
				}
			}
			if (JustPressed(Keys.Left)) {
				if (container.CursorIndex > 0) {
					container.CursorIndex--;
					typed = true;
				}
			} else if (JustPressed(Keys.Right)) {
				if (container.CursorIndex < container.Text.Length) {
					container.CursorIndex++;
					typed = true;
				}
			}

			if (input.Length == 0 && container.CursorIndex > 0) {
				container.Text.Remove(--container.CursorIndex, 1);
				typed = true;
			} else if (input.Length == 2) {
				container.Text.Insert(container.CursorIndex++, input[1]);
				typed = true;
			} else if (JustPressed(Keys.Delete)) {
				if (container.CursorIndex < container.Text.Length) {
					container.Text.Remove(container.CursorIndex, 1);
					typed = true;
				}
			}
			if (JustPressed(Keys.Enter)) {
				container.Submit();
			} else if (Main.inputTextEscape) {
				container.Reset();
			}
		}
		public static void DrawInputContainerText(this ITextInputContainer container, SpriteBatch spriteBatch, Vector2 position, DynamicSpriteFont font, Color textColor, bool focused, float scale = 1f, Vector2 offset = default) {
			container.DrawInputContainerText(spriteBatch, position, font, BasicStringDrawer(textColor), focused, scale, offset);
		}
		public static void DrawInputContainerText(this ITextInputContainer container, SpriteBatch spriteBatch, Vector2 position, DynamicSpriteFont font, StringDrawer drawer, bool focused, float scale = 1f, Vector2 offset = default, int blinkRate = 20) {
			DrawInputContainerText(container.TextDisplay.ToString(), container.CursorIndex, spriteBatch, position, font, drawer, focused, scale, offset, blinkRate);
		}
		public static void DrawInputContainerText(string text, int cursorIndex, SpriteBatch spriteBatch, Vector2 position, DynamicSpriteFont font, StringDrawer drawer, bool focused, float scale = 1f, Vector2 offset = default, int blinkRate = 20) {
			if (focused) {
				DrawInputTextCursor(
					PegasusConfig.Instance.preferredTextCursor,
					text, 
					cursorIndex, 
					spriteBatch, 
					position, 
					font, 
					drawer, 
					scale,
					offset, 
					blinkRate
				);
			}
			drawer(spriteBatch,
				font,
				text,
				position + offset,
				new(scale)
			);
		}
		public static void DrawInputTextCursor(CursorType cursor, string text, int cursorIndex, SpriteBatch spriteBatch, Vector2 position, DynamicSpriteFont font, StringDrawer drawer, float scale = 1f, Vector2 offset = default, int blinkRate = 20) {
			if (Main.timeForVisualEffects % (blinkRate * 2) < blinkRate) {
				string cursorChar = "";
				Vector2 cursorOffset = default;
				Vector2 cursorScale = new(scale);
				switch (cursor) {
					case CursorType.Line:
					cursorChar = "|";
					break;

					case CursorType.Caret: {
						static float Tallness(char c, bool before) {
							switch (c) {
								case 'g':
								return 1f;

								case 'j':
								case 'y':
								return 0.75f;

								case 'p':
								if (!before) return 1;
								break;
								case 'q':
								if (before) return 1;
								break;
							}
							return 0;
						}
						cursorChar = "^";
						cursorOffset = new(1, 16);
						cursorOffset.Y += Math.Max(cursorIndex > 0 ? Tallness(text[cursorIndex - 1], true) : 0, cursorIndex < text.Length ? Tallness(text[cursorIndex], false) : 0)
							* 6;
						break;
					}

					case CursorType.Caron: {
						static float Tallness(char c, bool before) {
							switch (c) {
								case 'f':
								case 'l':
								return 0.75f;

								case 'i':
								case 'j':
								return 0.5f;

								case 't':
								return 0.5f;

								case 'b':
								case 'h':
								if (!before) return 1;
								break;
								case 'd':
								if (before) return 1;
								break;
							}
							return 0;
						}
						cursorChar = "^";
						cursorOffset = new(1, 8);
						cursorOffset.Y -= Math.Max(cursorIndex > 0 ? Tallness(text[cursorIndex - 1], true) : 0, cursorIndex < text.Length ? Tallness(text[cursorIndex], false) : 0)
							* 6;
						cursorScale.Y *= -1;
						break;
					}
					case CursorType.Underscore:
					cursorChar = "_";
					cursorOffset = new Vector2(offset.X * 0.5f, 0) / scale;
					cursorOffset.X += 1;
					cursorOffset.Y -= 3;
					if (cursorIndex < text.Length) {
						cursorScale.X = font.MeasureString(text[cursorIndex].ToString()).X / font.MeasureString(cursorChar).X;
					} else {
						cursorScale.X = 0.5f;
					}
					break;
				}
				drawer(spriteBatch,
					font,
					cursorChar,
					position + font.MeasureString(text[..cursorIndex]) * Vector2.UnitX * scale + offset * new Vector2(0.5f, 1) + cursorOffset * scale,
					cursorScale
				);
			}
		}
		public enum CursorType {
			Line,
			Caret,
			Caron,
			Underscore,
		}
		public delegate void StringDrawer(SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Vector2 scale);
		public static StringDrawer BasicStringDrawer(Color textColor) => (spriteBatch, font, text, position, scale)  => {
			spriteBatch.DrawString(
				font,
				text,
				position,
				textColor,
				0,
				new(0, 0),
				scale,
				0,
			0);
		};
		public static StringDrawer ShadowedStringDrawer(Color textColor, float spread = 2) => (spriteBatch, font, text, position, scale) => {
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch,
				font,
				text,
				position,
				textColor,
				0,
				new(0, 0),
				scale,
				spread: spread
			);
		};
		public static StringDrawer ShadowedStringDrawer(Color textColor, Color shadowColor, float spread = 2, bool hoverSnippets = false) => (spriteBatch, font, text, position, scale) => {
			TextSnippet[] snippets = ChatManager.ParseMessage(text, textColor).ToArray();
			ChatManager.ConvertNormalSnippets(snippets);
			ChatManager.DrawColorCodedStringShadow(spriteBatch, font, snippets, position, shadowColor, 0, Vector2.Zero, scale, -1, spread);
			ChatManager.DrawColorCodedString(spriteBatch, font, snippets, position, Color.White, 0, Vector2.Zero, scale, out int hoveredSnippet, -1);
			if (hoverSnippets && hoveredSnippet != -1 && snippets[hoveredSnippet].CheckForHover) {
				snippets[hoveredSnippet].OnHover();
				if (Main.mouseLeft && Main.mouseLeftRelease) snippets[hoveredSnippet].OnClick();
			}
		};
		public static bool PressingAlt(this KeyboardState state) => state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt);
		public static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
	}
}
