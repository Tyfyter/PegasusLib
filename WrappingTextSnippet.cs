using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using PegasusLib.Reflection;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PegasusLib {
	public abstract class WrappingTextSnippet(string text, Color color, float scale = 1f) : TextSnippet(text, color, scale) {
		public WrappingTextSnippet() : this("", Color.White, 1) { }
		public static Vector2 BasePosition { get; internal set; }
		public static float MaxWidth { get; internal set; }
		public static Vector2 Origin { get; internal set; }
		public bool IsHovered { get; protected set; }
		public class PaddingSnippet(float width) : TextSnippet {
			public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1) {
				size = new(width, 28);
				return true;
			}
		}
	}
	public class WrappingTextSnippetSetup : ILoadable {
		public void Load(Mod mod) {
			try {
				On_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += On_ChatManager_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool;
				IL_ChatManager.DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool += IL_ChatManager_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool;
				MonoModHooks.Add(typeof(DynamicSpriteFont).GetMethod("InternalDraw", BindingFlags.NonPublic | BindingFlags.Instance), (orig_InternalDraw orig, DynamicSpriteFont self, string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth) => {
					Vector2 oldOrigin = WrappingTextSnippet.Origin;
					WrappingTextSnippet.Origin = origin;
					orig(self, text, spriteBatch, startPosition, color, rotation, origin, ref scale, spriteEffects, depth);
					WrappingTextSnippet.Origin = oldOrigin;
				});
			} catch (Exception exception) {
				PegasusLib.FeatureError(LibFeature.WrappingTextSnippet, exception);
#if DEBUG
				throw;
#endif
			}
		}
		delegate void orig_InternalDraw(DynamicSpriteFont self, string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth);
		delegate void hook_InternalDraw(orig_InternalDraw orig, DynamicSpriteFont self, string text, SpriteBatch spriteBatch, Vector2 startPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth);
		public static void SetWrappingData(Vector2 basePosition, float maxWidth) {
			WrappingTextSnippet.BasePosition = basePosition;
			WrappingTextSnippet.MaxWidth = maxWidth;
		}
		private static Vector2 On_ChatManager_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool(On_ChatManager.orig_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool orig, SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors) {
			SetWrappingData(position, maxWidth);
			return orig(spriteBatch, font, snippets, position, baseColor, rotation, origin, baseScale, out hoveredSnippet, maxWidth, ignoreColors);
		}
		private static void IL_ChatManager_DrawColorCodedString_SpriteBatch_DynamicSpriteFont_TextSnippetArray_Vector2_Color_float_Vector2_Vector2_refInt32_float_bool(ILContext il) {
			ILCursor c = new(il);
			c.GotoNext(MoveType.Before,
				static i => i.MatchCallvirt<TextSnippet>(nameof(TextSnippet.UniqueDraw))
			);
			MonoModMethods.SkipPrevArgument(c);//scale
			if (!c.Next.MatchLdarg(out int scale)) throw new Exception("scale not Ldarg");
			MonoModMethods.SkipPrevArgument(c);//color
			MonoModMethods.SkipPrevArgument(c);//position
			if (!c.Next.MatchLdloc(out int vector)) throw new Exception("vector not Ldloc");
			MonoModMethods.SkipPrevArgument(c);//spriteBatch
			MonoModMethods.SkipPrevArgument(c);//size
			MonoModMethods.SkipPrevArgument(c);//justCheckingString
			MonoModMethods.SkipPrevArgument(c);//this
			if (!c.Next.MatchLdloc(out int textSnippet)) throw new Exception("textSnippet not Ldloc");
			c.GotoNext(MoveType.After,
				i => i.MatchCall(typeof(Utils), nameof(Utils.Between))
			);
			c.EmitLdloc(textSnippet);
			c.EmitDelegate(static (bool value, TextSnippet textSnippet) => {
				if (textSnippet is WrappingTextSnippet snippet) return snippet.IsHovered;
				return value;
			});
			c.GotoNext(MoveType.After,
				i => i.MatchLdloca(out _),							//IL_00a8: ldloca.s 3
				i => i.MatchLdloc(out _),							//IL_00aa: ldloc.3
				i => i.MatchLdfld<Vector2>("X"),					//IL_00ab: ldfld float32[FNA]Microsoft.Xna.Framework.Vector2::X
				i => i.MatchLdloc(vector),							//IL_00b0: ldloc.2
				i => i.MatchLdfld<Vector2>("X"),					//IL_00b1: ldfld float32[FNA]Microsoft.Xna.Framework.Vector2::X
				i => i.MatchCall(typeof(Math), nameof(Math.Max)),	//IL_00b6: call float32[System.Runtime]System.Math::Max(float32, float32)
				i => i.MatchStfld<Vector2>("X")						//IL_00bb: stfld float32[FNA]Microsoft.Xna.Framework.Vector2::X
			);
			c.EmitLdloc(textSnippet);
			c.EmitLdloca(vector);
			c.EmitLdarg(3);
			c.EmitLdarg(il.Method.Parameters.Count - 2);
			c.EmitLdarg(1);
			c.EmitLdarg(scale);
			c.EmitDelegate(static (TextSnippet snippet, ref Vector2 vector, Vector2 startPosition, float maxWidth, DynamicSpriteFont font, Vector2 scale) => {
				if (maxWidth == -1 || snippet is not WrappingTextSnippet) return;
				float lineSpacing = font.LineSpacing * scale.Y;
				while (vector.X - startPosition.X > maxWidth) {
					vector.X -= maxWidth;
					vector.Y += lineSpacing;
				}
			});
		}

		public void Unload() {}
	}
}
