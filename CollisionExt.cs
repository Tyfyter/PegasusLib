using System;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework;

namespace PegasusLib {
	public static class CollisionExt {
		static Triangle[] tileTriangles;
		static Rectangle[] tileRectangles;
		public static void Load() {
			Vector2 topLeft = Vector2.Zero * 16;
			Vector2 topRight = Vector2.UnitX * 16;
			Vector2 bottomLeft = Vector2.UnitY * 16;
			Vector2 bottomRight = Vector2.One * 16;
			tileTriangles = [
				new Triangle(topLeft, bottomLeft, bottomRight),
				new Triangle(topRight, bottomLeft, bottomRight),
				new Triangle(topLeft, topRight, bottomLeft),
				new Triangle(topLeft, topRight, bottomRight)
			];
			tileRectangles = [
				new Rectangle(0, 0, 16, 16),
				new Rectangle(0, 8, 16, 8)
			];
		}
		public static void Unload() {
			tileTriangles = null;
			tileRectangles = null;
		}
		public static bool OverlapsAnyTiles(this Rectangle area, bool fallThrough = true) {
			Rectangle checkArea = area;
			Point topLeft = area.TopLeft().ToTileCoordinates();
			Point bottomRight = area.BottomRight().ToTileCoordinates();
			int minX = Utils.Clamp(topLeft.X, 0, Main.maxTilesX - 1);
			int minY = Utils.Clamp(topLeft.Y, 0, Main.maxTilesY - 1);
			int maxX = Utils.Clamp(bottomRight.X, 0, Main.maxTilesX - 1) - minX;
			int maxY = Utils.Clamp(bottomRight.Y, 0, Main.maxTilesY - 1) - minY;
			int cornerX = area.X - topLeft.X * 16;
			int cornerY = area.Y - topLeft.Y * 16;
			for (int i = 0; i <= maxX; i++) {
				for (int j = 0; j <= maxY; j++) {
					Tile tile = Main.tile[i + minX, j + minY];
					if (fallThrough && Main.tileSolidTop[tile.TileType]) continue;
					if (tile != null && tile.HasSolidTile()) {
						checkArea.X = i * -16 + cornerX;
						checkArea.Y = j * -16 + cornerY;
						if (tile.Slope != SlopeType.Solid) {
							if (tileTriangles[(int)tile.Slope - 1].Intersects(checkArea)) return true;
						} else {
							if (tileRectangles[(int)tile.BlockType].Intersects(checkArea)) return true;
						}
					}
				}
			}
			return false;
		}
		public static bool CanHitRay(Vector2 position, Vector2 target) {
			Vector2 diff = target - position;
			float length = diff.Length();
			return Raymarch(position, diff, length) == length;
		}
		/// <summary>
		/// Throws <see cref="ArgumentException"/> if <paramref name="direction"/> is zero
		/// </summary>
		/// <param name="position"></param>
		/// <param name="direction"></param>
		/// <param name="maxLength"></param>
		/// <returns>The distance traveled before a tile was reached, or <paramref name="maxLength"/> if the distance would exceed it</returns>
		/// <exception cref="ArgumentException"></exception>
		public static float Raymarch(Vector2 position, Vector2 direction, float maxLength = float.PositiveInfinity) {
			if (direction == Vector2.Zero) throw new ArgumentException($"{nameof(direction)} may not be zero");
			float length = 0;
			Point tilePos = position.ToTileCoordinates();
			Vector2 tileSubPos = (position - tilePos.ToWorldCoordinates(0, 0)) / 16;
			float angle = direction.ToRotation();
			double sin = Math.Sin(angle);
			double cos = Math.Cos(angle);
			double slope = cos == 0 ? Math.CopySign(double.PositiveInfinity, sin) : sin / cos;
			static void DoLoopyThing(float currentSubPos, out float newSubPos, int currentTilePos, out int newTilePos, double direction) {
				newTilePos = currentTilePos;
				if (currentSubPos == 0 && direction < 0) {
					newSubPos = 1;
					newTilePos--;
				} else if (currentSubPos == 1 && direction > 0) {
					newSubPos = 0;
					newTilePos++;
				} else {
					newSubPos = currentSubPos;
				}
			}
			if (RaycastStep(tileSubPos, sin, cos) == tileSubPos) {
				DoLoopyThing(tileSubPos.X, out tileSubPos.X, tilePos.X, out tilePos.X, cos);
				DoLoopyThing(tileSubPos.Y, out tileSubPos.Y, tilePos.Y, out tilePos.Y, sin);
			}
			while (length < maxLength) {
				Vector2 next = RaycastStep(tileSubPos, sin, cos);
				if (next == tileSubPos) break;
				Tile tile = Framing.GetTileSafely(tilePos);
				bool doBreak = !WorldGen.InWorld(tilePos.X, tilePos.Y);
				Vector2 diff = next - tileSubPos;
				float dist = diff.Length();

				if (tile.HasFullSolidTile()) {
					float flope = (float)slope;
					bool doSICalc = true;
					float tileSlope = 0;
					float tileIntercept = 0;
					switch (tile.BlockType) {
						case BlockType.Solid:
						doBreak = true;
						doSICalc = false;
						break;
						case BlockType.HalfBlock:
						if (next.Y > 0.5f) {
							doBreak = true;
							tileSlope = 0;
							tileIntercept = 0.5f;
						}
						break;
						case BlockType.SlopeDownLeft:
						if (next.X == 0 || next.Y == 1) {
							doBreak = true;
							tileSlope = 1;
							tileIntercept = 0;
						}
						break;
						case BlockType.SlopeDownRight:
						if (next.X == 1 || next.Y == 1) {
							doBreak = true;
							tileSlope = -1;
							tileIntercept = 1;
						}
						break;
						case BlockType.SlopeUpLeft:
						if (next.X == 0 || next.Y == 0) {
							doBreak = true;
							tileSlope = -1;
							tileIntercept = 1;
						}
						break;
						case BlockType.SlopeUpRight:
						if (next.X == 1 || next.Y == 0) {
							doBreak = true;
							tileSlope = 1;
							tileIntercept = 0;
						}
						break;
					}
					if (doSICalc) {
						//gets x position of intersection, y position can then be calculated by finding the y position at that x on either line
						float factor = ((tileSubPos.X * -flope + tileSubPos.Y) - tileIntercept) / (tileSlope - flope);
						Vector2 endPoint = new(
							factor,
							tileSlope * factor + tileIntercept
						);
						length += (float)(16 * endPoint.Distance(tileSubPos));
					}
				}
				if (doBreak) break;
				length += dist * 16;
				//Dust.NewDustPerfect(tilePos.ToWorldCoordinates(0, 0) + next * 16, 6, Vector2.Zero).noGravity = true;
				DoLoopyThing(next.X, out next.X, tilePos.X, out tilePos.X, cos);
				DoLoopyThing(next.Y, out next.Y, tilePos.Y, out tilePos.Y, sin);
				tile = Framing.GetTileSafely(tilePos);
				if (tile.HasFullSolidTile()) {
					switch (tile.BlockType) {
						case BlockType.Solid:
						doBreak = true;
						break;
						case BlockType.HalfBlock:
						if (next.Y > 0.5f) doBreak = true;
						break;
						case BlockType.SlopeDownLeft:
						if (next.X == 0 || next.Y == 1) doBreak = true;
						break;
						case BlockType.SlopeDownRight:
						if (next.X == 1 || next.Y == 1) doBreak = true;
						break;
						case BlockType.SlopeUpLeft:
						if (next.X == 0 || next.Y == 0) doBreak = true;
						break;
						case BlockType.SlopeUpRight:
						if (next.X == 1 || next.Y == 0) doBreak = true;
						break;
					}
				}
				if (!doBreak && (next.X == 0 || next.X == 1) && (next.Y == 0 || next.Y == 1)) {
					switch ((next.X, next.Y)) {
						case (0, 0):
						if (Framing.GetTileSafely(tilePos.X, tilePos.Y - 1).BlockType != BlockType.SlopeUpRight && Framing.GetTileSafely(tilePos.X - 1, tilePos.Y).BlockType is not BlockType.SlopeDownLeft or BlockType.HalfBlock) {
							doBreak = true;
						}
						break;
						case (1, 0):
						if (Framing.GetTileSafely(tilePos.X, tilePos.Y - 1).BlockType != BlockType.SlopeUpLeft && Framing.GetTileSafely(tilePos.X + 1, tilePos.Y).BlockType is not BlockType.SlopeDownRight or BlockType.HalfBlock) {
							doBreak = true;
						}
						break;
						case (0, 1):
						if (Framing.GetTileSafely(tilePos.X, tilePos.Y + 1).BlockType is not BlockType.SlopeDownRight or BlockType.HalfBlock && Framing.GetTileSafely(tilePos.X - 1, tilePos.Y).BlockType != BlockType.SlopeUpLeft) {
							doBreak = true;
						}
						break;
						case (1, 1):
						if (Framing.GetTileSafely(tilePos.X, tilePos.Y + 1).BlockType is not BlockType.SlopeDownLeft or BlockType.HalfBlock && Framing.GetTileSafely(tilePos.X + 1, tilePos.Y).BlockType != BlockType.SlopeUpRight) {
							doBreak = true;
						}
						break;
					}
				}
				if (doBreak) break;
				tileSubPos = next;
			}
			if (length > maxLength) return maxLength;
			return length;
		}
		static Vector2 RaycastStep(Vector2 pos, double sin, double cos) {
			if (cos == 0) return new(pos.X, sin > 0 ? 1 : 0);
			if (sin == 0) return new(cos > 0 ? 1 : 0, pos.Y);
			double slope = sin / cos;
			int xVlaue = cos > 0 ? 1 : 0;
			double yIntercept = pos.Y - slope * (pos.X - xVlaue);
			if (yIntercept >= 0 && yIntercept <= 1) return new Vector2(xVlaue, (float)yIntercept);
			int yVlaue = sin > 0 ? 1 : 0;
			double xIntercept = (pos.Y - yVlaue) / -slope + pos.X;
			return new Vector2((float)xIntercept, yVlaue);
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="a"></param>
		/// <returns>The intersection point between a line segment connecting <paramref name="a"/> to the center of <paramref name="rect"/> and <paramref name="rect"/></returns>
		public static Vector2 GetCenterProjectedPoint(Rectangle rect, Vector2 a) {
			Vector2 b = rect.Center();
			float s = (a.Y - b.Y) / (a.X - b.X);
			float v = s * rect.Width / 2;
			if (-rect.Height / 2 <= v && v <= rect.Height / 2) {
				if (a.X > b.X) {
					return new(rect.Right, b.Y + v);
				} else {
					return new(rect.Left, b.Y - v);
				}
			} else {
				v = (rect.Height / 2) / s;
				if (a.Y > b.Y) {
					return new(b.X + v, rect.Bottom);
				} else {
					return new(b.X - v, rect.Top);
				}
			}
		}
		/// <summary>
		/// checks if a convex polygon defined by a set of line segments intersects a rectangle
		/// </summary>
		/// <param name="lines"></param>
		/// <param name="hitbox"></param>
		/// <returns></returns>
		public static bool PolygonIntersectsRect((Vector2 start, Vector2 end)[] lines, Rectangle hitbox) {
			int intersections = 0;
			Vector2 rectPos = hitbox.TopLeft();
			Vector2 rectSize = hitbox.Size();
			bool hasSize = hitbox.Width != 0 || hitbox.Height != 0;
			for (int i = 0; i < lines.Length; i++) {
				Vector2 a = lines[i].start;
				Vector2 b = lines[i].end;
				if (hasSize && Collision.CheckAABBvLineCollision2(rectPos, rectSize, a, b)) return true;
				float t = ((a.X - rectPos.X) * (rectPos.Y) - (a.Y - rectPos.Y) * (rectPos.X))
						/ ((a.X - b.X) * (rectPos.Y) - (a.Y - b.Y) * (rectPos.X));
				if (t < 0 || t > 1) continue;

				float u = ((a.X - b.X) * (a.Y - rectPos.Y) - (a.Y - b.Y) * (a.X - rectPos.X))
						/ ((a.X - b.X) * (rectPos.Y) - (a.Y - b.Y) * (rectPos.X));
				if (u > 0 && u < 1) intersections++;
			}
			return intersections % 2 == 1;
		}
		public static bool HasSolidTile(this Tile tile) {
			return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];
		}
		public static bool HasFullSolidTile(this Tile tile) {
			return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
		}
	}
}
