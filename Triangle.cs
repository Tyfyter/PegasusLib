using System;
using System.Diagnostics;
using Terraria;

namespace PegasusLib; 
public struct Triangle(Vector2 a, Vector2 b, Vector2 c) {
	public Vector2 a = a;
	public Vector2 b = b;
	public Vector2 c = c;
	public Vector2 this[int i] {
		readonly get {
			switch ((i % 3 + 3) % 3) {
				case 0: return a;
				case 1: return b;
				case 2: return c;
			}
			throw new UnreachableException();
		}
		set {
			switch ((i % 3 + 3) % 3) {
				case 0:
				a = value;
				break;

				case 1:
				b = value;
				break;

				case 2:
				c = value;
				break;
			}
			throw new UnreachableException();
		}
	}
	public readonly float Area => float.Abs(Winding) * 0.5f;
	public readonly bool IsDegenerate => Area <= float.Epsilon;
	public readonly float Winding => a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
	public readonly bool Intersects(Rectangle rect) {
		if (Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), a, b)) return true;
		if (Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), b, c)) return true;
		if (Collision.CheckAABBvLineCollision2(rect.TopLeft(), rect.Size(), c, a)) return true;
		return Contains(rect.TopLeft());
	}
	public readonly bool Intersects(Triangle other) {
		Triangle self = this;
		// Triangles must be expressed counterclockwise
		if (self.Winding < 0) (self.b, self.c) = (self.c, self.b);
		if (other.Winding < 0) (other.b, other.c) = (other.c, other.b);

		// for each edge E of t1
		for (int i = 0; i < 3; i++) {
			int j = (i + 1) % 3;
			// Check all points of t2 lay on the external side of edge E.
			// If they do, the triangles do not overlap.
			if (new Triangle(self[i], self[j], other[0]).Winding <= float.Epsilon &&
				new Triangle(self[i], self[j], other[1]).Winding <= float.Epsilon &&
				new Triangle(self[i], self[j], other[2]).Winding <= float.Epsilon) {
				return false;
			}
		}

		// for each edge E of t2
		for (int i = 0; i < 3; i++) {
			int j = (i + 1) % 3;
			// Check all points of t1 lay on the external side of edge E.
			// If they do, the triangles do not overlap.
			if (new Triangle(other[i], other[j], self[0]).Winding <= float.Epsilon &&
				new Triangle(other[i], other[j], self[1]).Winding <= float.Epsilon &&
				new Triangle(other[i], other[j], self[2]).Winding <= float.Epsilon) {
				return false;
			}
		}

		return true;
	}
	public readonly bool Contains(Vector2 point) {
		if (IsDegenerate) return false;
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
