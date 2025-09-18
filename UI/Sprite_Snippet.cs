using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PegasusLib.UI;
public class Sprite_Snippet_Handler : ITagHandler {
	public class Sprite_Snippet : TextSnippet {
		readonly Asset<Texture2D> image;
		Options options;
		public Sprite_Snippet(string text, Options options) : base(text, options.Color ?? Color.White, (options.Scale.X + options.Scale.Y) * 0.5f) {
			this.options = options;
			if (ModContent.RequestIfExists(text, out image)) {
				Text = "";
			} else {
				Scale = 1;
			}
		}
		public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1) {
			size = default;
			if (image is null) return false;
			image.Wait();
			size = (options.Frame?.Size() ?? image.Size()) * options.Scale * scale;
			if (options.Color.HasValue) color = color.MultiplyRGBA(options.Color.Value);
			spriteBatch?.Draw(image.Value, position, options.Frame, color, 0, Vector2.Zero, options.Scale * scale, SpriteEffects.None, 0);
			return true;
		}
	}
	public record struct Options(Vector2 Scale, Color? Color = null, Rectangle? Frame = null);
	public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
		Options settings = new(Vector2.One);
		SnippetHelper.ParseOptions(options,
			SnippetOption.CreateFloatsOption("sc", 1..2, value => {
				switch (value.Length) {
					case 1:
					settings.Scale = new(value[0]);
					break;

					case 2:
					settings.Scale = new(value[0], value[1]);
					break;
				}
			}),
			SnippetOption.CreateRectangleOption("fr", value => settings.Frame = value),
			SnippetOption.CreateColorOption("c", value => settings.Color = value)
		);
		return new Sprite_Snippet(text, settings);
	}
}
