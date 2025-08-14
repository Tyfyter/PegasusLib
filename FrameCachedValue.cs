using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegasusLib {
	public class FrameCachedValue<T> {
		uint lastGameFrameCount = 0;
		readonly Func<T> GetValueFunc;
		T value;
		public FrameCachedValue(Func<T> getValueFunc) {
			GetValueFunc = getValueFunc;
			if (PegasusLib.unloading) return;
			try {
				value = GetValueFunc();
				lastGameFrameCount = PegasusLib.gameFrameCount;
			} catch (Exception) { }
		}
		public T GetValue() => Value;
		public T Value {
			get {
				if (lastGameFrameCount != PegasusLib.gameFrameCount) {
					lastGameFrameCount = PegasusLib.gameFrameCount;
					value = GetValueFunc();
				}
				return value;
			}
		}
		public static implicit operator FrameCachedValue<T>(Func<T> getValueFunc) => new(getValueFunc);
	}
}
