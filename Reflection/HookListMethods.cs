using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PegasusLib.Reflection {
	public static class GlobalHookListMethods<TGlobal> where TGlobal : GlobalType<TGlobal> {
		private static Func<int, TGlobal[]> _ForType = null;
		public static TGlobal[] ForType(GlobalHookList<TGlobal> hookList, int type) {
			if (_ForType is null) {
				MethodInfo info = typeof(GlobalHookList<TGlobal>).GetMethod("ForType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				_ForType = info.CreateDelegate<Func<int, TGlobal[]>>(hookList);
			}
			DelegateMethods._target.SetValue(_ForType, hookList);
			return _ForType(type);
		}
	}
}
