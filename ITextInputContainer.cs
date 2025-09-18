using Microsoft.Xna.Framework.Input;
using System.Text;
using Terraria.GameInput;
using Terraria;
using ReLogic.OS;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System;
using MonoMod.Cil;

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
			string text = container.TextDisplay.ToString();
			if (focused && Main.timeForVisualEffects % 40 < 20) {
				spriteBatch.DrawString(
					font,
					"|",
					position + font.MeasureString(text[..container.CursorIndex]) * Vector2.UnitX * scale + offset * new Vector2(0.5f, 1),
					textColor,
					0,
					new(0, 0),
					scale,
					0,
				0);
			}
			spriteBatch.DrawString(
				font,
				text,
				position + offset,
				textColor,
				0,
				new(0, 0),
				scale,
				0,
			0);
		}
		public static bool PressingAlt(this KeyboardState state) => state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt);
		public static bool JustPressed(Keys key) => Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
	}
}
