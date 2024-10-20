using System;

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
	}
}
