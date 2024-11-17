using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace PegasusLib;
public static class GeometryUtils {
	public static double AngleDif(double alpha, double beta) {
		double TwoPi = (Math.PI * 2);
		double phi = Math.Abs(beta - alpha) % TwoPi;       // This is either the distance or 360 - distance
		double dir = ((phi > Math.PI) ^ (alpha > beta)) ? -1 : 1;
		double distance = phi > Math.PI ? TwoPi - phi : phi;
		return distance * dir;
	}
	public static float AngleDif(float alpha, float beta, out int dir) {
		float phi = Math.Abs(beta - alpha) % MathHelper.TwoPi;       // This is either the distance or 360 - distance
		dir = ((phi > MathHelper.Pi) ^ (alpha > beta)) ? -1 : 1;
		float distance = phi > MathHelper.Pi ? MathHelper.TwoPi - phi : phi;
		return distance;
	}
	public static Vector2 Vec2FromPolar(float r, float theta) {
		return new Vector2((float)(r * Math.Cos(theta)), (float)(r * Math.Sin(theta)));
	}
	public static float? AngleToTarget(Vector2 diff, float speed, float grav = 0.04f, bool high = false) {
		float v2 = speed * speed;
		float v4 = speed * speed * speed * speed;
		grav = -grav;
		float sqr = v4 - grav * (grav * diff.X * diff.X + 2 * diff.Y * v2);
		if (sqr >= 0) {
			sqr = MathF.Sqrt(sqr);
			float offset = diff.X > 0 ? 0 : MathHelper.Pi;
			return (high ? MathF.Atan((v2 + sqr) / (grav * diff.X)) : MathF.Atan((v2 - sqr) / (grav * diff.X))) + offset;
		}
		return null;
	}
	public static bool AngularSmoothing(ref float smoothed, float target, float rate) {
		if (target != smoothed) {
			float diff = AngleDif(smoothed, target, out int dir);
			if (Math.Abs(diff) < rate) {
				smoothed = target;
			} else {
				smoothed += rate * dir;
			}
		}
		return smoothed == target;
	}
	public static bool AngularSmoothing(ref float smoothed, float target, float rate, bool snap) {
		if (target != smoothed) {
			float diff = AngleDif(smoothed, target, out int dir);
			diff = Math.Abs(diff);
			if (diff < rate || (snap && diff > MathHelper.Pi - rate)) {
				smoothed = target;
			} else {
				smoothed -= rate * dir;
			}
		}
		return smoothed == target;
	}
	public static Vector2 WithMaxLength(this Vector2 vector, float length) {
		float pLength = vector.LengthSquared();
		return pLength > length * length ? Vector2.Normalize(vector) * length : vector;
	}
	public static bool IsWithin(this Vector2 a, Vector2 b, float range) => a.DistanceSQ(b) < range * range;
	public static bool GetIntersectionPoints(Vector2 start, Vector2 end, Rectangle bounds, out List<Vector2> intersections) {
		return GetIntersectionPoints(start, end, bounds.TopLeft(), bounds.BottomRight(), out intersections);
	}
	public static bool GetIntersectionPoints(Vector2 start, Vector2 end, Vector2 topLeft, Vector2 bottomRight, out List<Vector2> intersections) {
		intersections = [];
		Vector2 intersection;
		if (Intersects(start, end, topLeft, new Vector2(bottomRight.X, topLeft.Y), out intersection)) intersections.Add(intersection);
		if (Intersects(start, end, new Vector2(bottomRight.X, topLeft.Y), bottomRight, out intersection)) intersections.Add(intersection);
		if (Intersects(start, end, bottomRight, new Vector2(topLeft.X, bottomRight.Y), out intersection)) intersections.Add(intersection);
		if (Intersects(start, end, new Vector2(topLeft.X, bottomRight.Y), topLeft, out intersection)) intersections.Add(intersection);
		return intersections.Count != 0;
	}
	public static bool Intersects(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
		intersection = a1;
		Vector2 b = a2 - a1;
		Vector2 d = b2 - b1;
		float bDotDPerp = b.X * d.Y - b.Y * d.X;

		// if b dot d == 0, it means the lines are parallel so have infinite intersection points
		if (bDotDPerp == 0) return true;

		Vector2 c = b1 - a1;
		float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
		if (t < 0 || t > 1) {
			return false;
		}

		float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
		if (u < 0 || u > 1) {
			return false;
		}
		intersection = a1 + t * b;
		return true;
	}
}