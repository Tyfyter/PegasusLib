using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace PegasusLib {
	public static class MathUtils {
		public static bool LinearSmoothing(ref float smoothed, float target, float rate) {
			if (target != smoothed) {
				if (Math.Abs(target - smoothed) < rate) {
					smoothed = target;
				} else {
					if (target > smoothed) {
						smoothed += rate;
					} else if (target < smoothed) {
						smoothed -= rate;
					}
				}
			}
			return smoothed == target;
		}
		/// <summary>
		/// Multiplies a vector by a matrix
		/// </summary>
		/// <param name="x">the vector used to determine the x component of the output</param>
		/// <param name="y">the vector used to determine the y component of the output</param>
		public static Vector2 MatrixMult(this Vector2 value, Vector2 x, Vector2 y) {
			return new Vector2(Vector2.Dot(value, x), Vector2.Dot(value, y));
		}
		public static Vector2 Clamp(this Vector2 vector, Rectangle rect) {
			return Vector2.Clamp(vector, rect.TopLeft(), rect.BottomRight());
		}
		public static bool LinearSmoothing(ref Vector2 smoothed, Vector2 target, float rate) {
			if (target != smoothed) {
				Vector2 diff = target - smoothed;
				float dist = diff.Length();
				if (dist < rate) {
					smoothed = target;
				} else {
					smoothed += diff * rate / dist;
				}
			}
			return smoothed == target;
		}
	}
}
