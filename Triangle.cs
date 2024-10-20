using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace PegasusLib {
	public readonly struct Triangle(Vector2 a, Vector2 b, Vector2 c) {
		public readonly Vector2 a = a;
		public readonly Vector2 b = b;
		public readonly Vector2 c = c;
		public readonly bool Intersects(Rectangle rect) {
			return Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), a, b) ||
			Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), b, c) ||
			Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), c, a) ||
			Contains(rect.TopLeft());
		}
		public readonly bool Contains(Vector2 point) {
			bool b0 = Vector2.Dot(new Vector2(point.X - a.X, point.Y - a.Y), new Vector2(a.Y - b.Y, b.X - a.X)) > 0;
			bool b1 = Vector2.Dot(new Vector2(point.X - b.X, point.Y - b.Y), new Vector2(b.Y - c.Y, c.X - b.X)) > 0;
			bool b2 = Vector2.Dot(new Vector2(point.X - c.X, point.Y - c.Y), new Vector2(c.Y - a.Y, a.X - c.X)) > 0;
			return (b0 == b1 && b1 == b2);
		}
		public readonly (Vector2 min, Vector2 max) GetBounds() {
			float minX = (int)Math.Min(Math.Min(a.X, b.X), c.X);
			float minY = (int)Math.Min(Math.Min(a.Y, b.Y), c.Y);
			float maxX = (int)Math.Max(Math.Max(a.X, b.X), c.X);
			float maxY = (int)Math.Max(Math.Max(a.Y, b.Y), c.Y);
			return (new Vector2(minX, minY), new Vector2(maxX, maxY));
		}
		public readonly bool HasNaNs() => a.HasNaNs() || b.HasNaNs() || c.HasNaNs();
	}
}
