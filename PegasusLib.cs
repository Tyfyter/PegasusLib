using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ObjectData;

namespace PegasusLib {
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class PegasusLib : Mod {
		internal static List<IUnloadable> unloadables = [];
		public override void Unload() {
			foreach (IUnloadable unloadable in unloadables) {
				unloadable.Unload();
			}
			unloadables = null;
			DropRuleExt.Unload();
		}
		public static Color GetRarityColor(int rare, bool expert = false, bool master = false) {
			if (expert || rare == ItemRarityID.Expert) {
				return Main.DiscoColor;
			}
			if (master || rare == ItemRarityID.Master) {
				return new Color(255, (int)(Main.masterColor * 200), 0);
			}
			if (rare >= ItemRarityID.Count) {
				return RarityLoader.GetRarity(rare).RarityColor;
			}
			switch (rare) {
				case ItemRarityID.Quest:
				return Colors.RarityAmber;

				case ItemRarityID.Gray:
				return Colors.RarityTrash;

				case ItemRarityID.Blue:
				return Colors.RarityBlue;

				case ItemRarityID.Green:
				return Colors.RarityGreen;

				case ItemRarityID.Orange:
				return Colors.RarityOrange;

				case ItemRarityID.LightRed:
				return Colors.RarityRed;

				case ItemRarityID.Pink:
				return Colors.RarityPink;

				case ItemRarityID.LightPurple:
				return Colors.RarityPurple;

				case ItemRarityID.Lime:
				return Colors.RarityLime;

				case ItemRarityID.Yellow:
				return Colors.RarityYellow;

				case ItemRarityID.Cyan:
				return Colors.RarityCyan;

				case ItemRarityID.Red:
				return Colors.RarityDarkRed;

				case ItemRarityID.Purple:
				return Colors.RarityDarkPurple;
			}
			return Colors.RarityNormal;
		}
		/// <summary>
		/// Gets the top left tile of a multitile
		/// </summary>
		/// <param name="i"></param>
		/// <param name="j"></param>
		/// <param name="data"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		public static void GetMultiTileTopLeft(int i, int j, TileObjectData data, out int left, out int top) {
			Tile tile = Main.tile[i, j];
			int innerFrameY = tile.TileFrameY % data.CoordinateFullHeight;
			int frameI = (tile.TileFrameX % data.CoordinateFullWidth) / (data.CoordinateWidth + data.CoordinatePadding);
			int frameJ = 0;
			while (innerFrameY >= data.CoordinateHeights[frameJ] + data.CoordinatePadding) {
				innerFrameY -= data.CoordinateHeights[frameJ] + data.CoordinatePadding;
				frameJ++;
			}
			top = j - frameJ;
			left = i - frameI;
		}
	}
}
