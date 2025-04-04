using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace PegasusLib {
	public interface ICustomDrawItem {
		void DrawInHand(Texture2D itemTexture, ref PlayerDrawSet drawInfo, Vector2 itemCenter, Color lightColor, Vector2 drawOrigin);
		/// <summary>
		/// whether or not the item is drawn over the player's hand
		/// </summary>
		bool DrawOverHand(Player player) => false;
		/// <summary>
		/// whether or not the item is drawn in a layer further back than usual
		/// </summary>
		bool BackHand(Player player) => false;
	}
	public class Custom_Item_Layer : PlayerDrawLayer {
		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			return (drawInfo.drawPlayer.ItemAnimationActive || drawInfo.heldItem.holdStyle > 0)
				&& !drawInfo.drawPlayer.JustDroppedAnItem
				&& drawInfo.shadow == 0
				&& drawInfo.heldItem.ModItem is ICustomDrawItem;
		}
		public override Position GetDefaultPosition() {
			Multiple position = new() {
				{ new Between(PlayerDrawLayers.BladedGlove, PlayerDrawLayers.ProjectileOverArm), (drawInfo) => (drawInfo.drawPlayer.HeldItem?.ModItem is ICustomDrawItem customDraw && customDraw.DrawOverHand(drawInfo.drawPlayer)) },
				{ new Between(PlayerDrawLayers.Skin, PlayerDrawLayers.Leggings), (drawInfo) => (drawInfo.drawPlayer.HeldItem?.ModItem is ICustomDrawItem customDraw && customDraw.BackHand(drawInfo.drawPlayer)) },
				{ new Between(null, PlayerDrawLayers.ArmOverItem), (drawInfo) => (drawInfo.drawPlayer.HeldItem?.ModItem is ICustomDrawItem customDraw && !customDraw.DrawOverHand(drawInfo.drawPlayer) && !customDraw.BackHand(drawInfo.drawPlayer)) }
			};
			return position;
		}
		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player drawPlayer = drawInfo.drawPlayer;
			Item item = drawPlayer.HeldItem;
			Texture2D itemTexture = TextureAssets.Item[item.type].Value;
			ICustomDrawItem aItem = (ICustomDrawItem)item.ModItem;

			Vector2 itemCenter = new(itemTexture.Width / 2, itemTexture.Height / 2);
			Vector2 drawItemPos = Main.DrawPlayerItemPos(drawPlayer.gravDir, item.type);
			int drawXPos = (int)drawItemPos.X;
			itemCenter.Y = drawItemPos.Y;
			if (drawPlayer.mount?.Active == true) {
				itemCenter.Y -= drawPlayer.mount.PlayerOffset;
			}
			Vector2 drawOrigin = new(drawXPos, itemTexture.Height / 2);
			if (drawPlayer.direction == -1) {
				drawOrigin = new Vector2(itemTexture.Width + drawXPos, itemTexture.Height / 2);
			}
			drawOrigin.X -= drawPlayer.width / 2;
			Color lightColor = Lighting.GetColor((int)(drawInfo.Position.X + drawInfo.drawPlayer.width * 0.5) / 16, (int)((drawInfo.Position.Y + drawInfo.drawPlayer.height * 0.5) / 16.0)); ;//drawInfo.colorBodySkin.ToVector4() / drawPlayer.skinColor.ToVector4();
			aItem.DrawInHand(itemTexture, ref drawInfo, itemCenter, lightColor, drawOrigin);
		}
	}
	public class CustomDrawItemPlayer : ModPlayer {
		public override void HideDrawLayers(PlayerDrawSet drawInfo) {
			if (drawInfo.heldItem.ModItem is ICustomDrawItem) PlayerDrawLayers.HeldItem.Hide();
		}
	}
}
