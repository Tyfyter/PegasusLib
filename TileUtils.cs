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