using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace PegasusLib.Graphics {
	public struct PolarVec2(float r, float theta) {
		public float R = r;
		public float Theta = theta;

		public static explicit operator Vector2(PolarVec2 pv) => new((float)(pv.R * Math.Cos(pv.Theta)), (float)(pv.R * Math.Sin(pv.Theta)));
		public static explicit operator PolarVec2(Vector2 vec) => new(vec.Length(), vec.ToRotation());
		public readonly PolarVec2 RotatedBy(float offset) => new(R, Theta + offset);
		public readonly PolarVec2 WithRotation(float theta) => new(R, theta);
		public readonly PolarVec2 WithLength(float length) => new(length, Theta);
		public static bool operator ==(PolarVec2 a, PolarVec2 b) => a.Theta == b.Theta && a.R == b.R;
		public static bool operator !=(PolarVec2 a, PolarVec2 b) => a.Theta != b.Theta || a.R != b.R;
		public static PolarVec2 operator *(PolarVec2 a, float scalar) => new(a.R * scalar, a.Theta);
		public static PolarVec2 operator *(float scalar, PolarVec2 a) => new(a.R * scalar, a.Theta);
		public static Vector2 operator *(PolarVec2 a, Vector2 vector) => ((Vector2)a) * vector;
		public static Vector2 operator *(Vector2 vector, PolarVec2 a) => ((Vector2)a) * vector;
		public static PolarVec2 operator /(PolarVec2 a, float scalar) => new(a.R / scalar, a.Theta);
		public override readonly bool Equals(object obj) => (obj is PolarVec2 other) && other == this;
		public override readonly int GetHashCode() {
			unchecked {
				return (R.GetHashCode() * 397) ^ Theta.GetHashCode();
			}
		}
		public override readonly string ToString() => $"{{r = {R}, θ = {Theta}}}";
		public static PolarVec2 Zero => new();
		public static PolarVec2 UnitRight => new(1, 0);
		public static PolarVec2 UnitUp => new(1, MathHelper.PiOver2);
		public static PolarVec2 UnitLeft => new(1, MathHelper.Pi);
		public static PolarVec2 UnitDown => new(1, -MathHelper.PiOver2);
	}
}
