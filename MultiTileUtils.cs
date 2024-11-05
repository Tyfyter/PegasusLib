using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace PegasusLib {
	[Obsolete("Moved to TileUtils")]
	public static class MultiTileUtils {
        public static Point GetRelativeOriginCoordinates(TileObjectData objectData, Tile tile) {
            int frameX = tile.TileFrameX % objectData.CoordinateFullWidth;
            int frameWidth = objectData.CoordinateWidth + objectData.CoordinatePadding;
            int frameY = 0;
            if(objectData.Height != 1) {
                for(int y = tile.TileFrameY; y > 0; y -= objectData.CoordinateHeights[frameY] + objectData.CoordinatePadding) {
                    frameY++;
                    frameY %= objectData.Height;
                    if(frameY == 0 && !objectData.StyleHorizontal) {
                        objectData = TileObjectData.GetTileData(tile.TileType, frameY/objectData.Height);
                        y -= objectData.CoordinatePadding;
                    }
                }
            }
            return new Point(objectData.Origin.X - (frameX / frameWidth), objectData.Origin.Y - frameY);
        }

        public static Point GetFrameFromCoordinates(TileObjectData objectData, Point Coords) {
            int frameX = Coords.X * objectData.CoordinateFullWidth;
            int frameY = 0;
            for(int y = Coords.Y; y > 0; y--) {
                frameY+=objectData.CoordinateHeights[frameY] + objectData.CoordinatePadding;
            }
            return new Point(frameX, frameY);
        }

        public static int GetStyleWidth(ushort type) {
            TileObjectData objectData = TileObjectData.GetTileData(type, 0);
            return objectData?.CoordinateFullWidth??0;
        }
        public static void AggressivelyPlace(Point Coords, ushort type, int style) {
            TileObjectData objectData = TileObjectData.GetTileData(type, style);
            int frameX;
            int frameY = 0;
            Tile tile;
            for(int y = 0; y < objectData.Height; y++) {
                frameX = style * objectData.CoordinateFullWidth;
                for(int x = 0; x < objectData.Width; x++) {
                    tile = Main.tile[Coords.X + x, Coords.Y + y];
                    tile.ResetToType(type);
                    tile.TileFrameX = (short)frameX;
                    tile.TileFrameY = (short)frameY;
                    frameX+=objectData.CoordinateWidth + objectData.CoordinatePadding;
                }
                frameY+=objectData.CoordinateHeights[y] + objectData.CoordinatePadding;
            }
        }
    }
}