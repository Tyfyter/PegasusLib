using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ObjectData;

namespace PegasusLib {
	public static class TileUtils {
		public static void SetTileData(this Tile tile, TileData data) {
			tile.Get<TileTypeData>() = data.TileTypeData;
			tile.Get<WallTypeData>() = data.WallTypeData;
			tile.Get<TileWallWireStateData>() = data.TileWallWireStateData;
			tile.Get<LiquidData>() = data.LiquidData;
			tile.Get<TileWallBrightnessInvisibilityData>() = data.TileWallBrightnessInvisibilityData;
		}
		public static void SetSlope(this TileWallWireStateData tile, SlopeType slopeType) {
			tile.Slope = slopeType;
		}
		delegate void _KillTile_GetItemDrops(int x, int y, Tile tileCache, out int dropItem, out int dropItemStack, out int secondaryItem, out int secondaryItemStack, bool includeLargeObjectDrops = false);
		static _KillTile_GetItemDrops KillTile_GetItemDrops;
		public static int GetTileDrop(this Tile tile, int x = 0, int y = 0) {
			int itemDrop = -1;
			if (tile.HasTile) {
				if (tile.TileType >= TileID.Count) {
					itemDrop = TileLoader.GetItemDropFromTypeAndStyle(tile.TileType, TileObjectData.GetTileStyle(tile));
				} else {
					if (KillTile_GetItemDrops is null) {
						KillTile_GetItemDrops = typeof(WorldGen).GetMethod("KillTile_GetItemDrops", BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate<_KillTile_GetItemDrops>();
						AssemblyLoadContext.GetLoadContext(typeof(TileUtils).Assembly).Unloading += (_) => KillTile_GetItemDrops = null;
					}
					KillTile_GetItemDrops(x, y, tile, out itemDrop, out _, out _, out _, false);
				}
			}
			return itemDrop;
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
		public static Point GetRelativeOriginCoordinates(TileObjectData objectData, Tile tile) {
			int frameX = tile.TileFrameX % objectData.CoordinateFullWidth;
			int frameWidth = objectData.CoordinateWidth + objectData.CoordinatePadding;
			int frameY = 0;
			if (objectData.Height != 1) {
				for (int y = tile.TileFrameY; y > 0; y -= objectData.CoordinateHeights[frameY] + objectData.CoordinatePadding) {
					frameY++;
					frameY %= objectData.Height;
					if (frameY == 0 && !objectData.StyleHorizontal) {
						objectData = TileObjectData.GetTileData(tile.TileType, frameY / objectData.Height);
						y -= objectData.CoordinatePadding;
					}
				}
			}
			return new Point(objectData.Origin.X - (frameX / frameWidth), objectData.Origin.Y - frameY);
		}

		public static Point GetFrameFromCoordinates(TileObjectData objectData, Point Coords) {
			int frameX = Coords.X * objectData.CoordinateFullWidth;
			int frameY = 0;
			for (int y = Coords.Y; y > 0; y--) {
				frameY += objectData.CoordinateHeights[frameY] + objectData.CoordinatePadding;
			}
			return new Point(frameX, frameY);
		}

		public static int GetStyleWidth(ushort type) {
			TileObjectData objectData = TileObjectData.GetTileData(type, 0);
			return objectData?.CoordinateFullWidth ?? 0;
		}
		public static void AggressivelyPlace(Point Coords, ushort type, int style) {
			TileObjectData objectData = TileObjectData.GetTileData(type, style);
			int frameX;
			int frameY = 0;
			Tile tile;
			for (int y = 0; y < objectData.Height; y++) {
				frameX = style * objectData.CoordinateFullWidth;
				for (int x = 0; x < objectData.Width; x++) {
					tile = Main.tile[Coords.X + x, Coords.Y + y];
					tile.ResetToType(type);
					tile.TileFrameX = (short)frameX;
					tile.TileFrameY = (short)frameY;
					frameX += objectData.CoordinateWidth + objectData.CoordinatePadding;
				}
				frameY += objectData.CoordinateHeights[y] + objectData.CoordinatePadding;
			}
		}
		public class TileData {
			public TileTypeData tileTypeData;
			public WallTypeData wallTypeData;
			public TileWallWireStateData tileWallWireStateData;
			public LiquidData liquidData;
			public TileWallBrightnessInvisibilityData tileWallBrightnessInvisibilityData;
			public ref TileTypeData TileTypeData => ref tileTypeData;
			public ref WallTypeData WallTypeData => ref wallTypeData;
			public ref TileWallWireStateData TileWallWireStateData => ref tileWallWireStateData;
			public ref LiquidData LiquidData => ref liquidData;
			public ref TileWallBrightnessInvisibilityData TileWallBrightnessInvisibilityData => ref tileWallBrightnessInvisibilityData;

			public static implicit operator TileData(Tile tile) {
				return new() {
					TileTypeData = tile.Get<TileTypeData>(),
					WallTypeData = tile.Get<WallTypeData>(),
					TileWallWireStateData = tile.Get<TileWallWireStateData>(),
					LiquidData = tile.Get<LiquidData>(),
					TileWallBrightnessInvisibilityData = tile.Get<TileWallBrightnessInvisibilityData>()
				};
			}
		}
	}
}