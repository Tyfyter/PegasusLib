using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework;

namespace PegasusLib {
	public static class Drawing {
		public static void DrawTileGlow(Texture2D glowTexture, Color glowColor, int i, int j, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];
			if (!TileDrawing.IsVisible(tile)) return;
			Vector2 offset = new(Main.offScreenRange, Main.offScreenRange);
			if (Main.drawToScreen) {
				offset = Vector2.Zero;
			}
			int posYFactor = -2;
			int flatY = 0;
			int kScaleY = 2;
			int flatX = 14;
			int kScaleX = -2;
			Vector2 position = new Vector2(i * 16f, j * 16f) + offset - Main.screenPosition;
			switch (tile.BlockType) {
				case BlockType.Solid:
				spriteBatch.Draw(glowTexture, position, new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16), glowColor, 0f, default, 1f, SpriteEffects.None, 0f);
				break;
				case BlockType.HalfBlock:
				spriteBatch.Draw(glowTexture, position + new Vector2(0, 8), new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 4), glowColor, 0f, default, 1f, SpriteEffects.None, 0f);
				spriteBatch.Draw(glowTexture, position + new Vector2(0, 12), new Rectangle(144, 66, 16, 4), glowColor, 0f, default, 1f, SpriteEffects.None, 0f);
				break;
				case BlockType.SlopeDownLeft://1
				posYFactor = 0;
				kScaleY = 0;
				flatX = 0;
				kScaleX = 2;
				goto case BlockType.SlopeUpRight;
				case BlockType.SlopeDownRight://2
				posYFactor = 0;
				kScaleY = 0;
				flatX = 14;
				kScaleX = -2;
				goto case BlockType.SlopeUpRight;
				case BlockType.SlopeUpLeft://3
				flatX = 0;
				kScaleX = 2;
				goto case BlockType.SlopeUpRight;

				case BlockType.SlopeUpRight://4
				for (int k = 0; k < 8; k++) {
					Main.spriteBatch.Draw(
						glowTexture,
						position + new Vector2(flatX + kScaleX * k, k * 2 + posYFactor * k),
						new Rectangle(tile.TileFrameX + flatX + kScaleX * k, tile.TileFrameY + flatY + kScaleY * k, 2, 16 - 2 * k),
						glowColor,
						0f,
						Vector2.Zero,
						1f,
						0,
						0f
					);
				}
				break;
			}
		}
	}
}
