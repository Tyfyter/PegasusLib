using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Terraria.ModLoader;

namespace PegasusLib.Reflection {
	[ReflectionParentType(typeof(KeybindLoader))]
	public class KeybindLoaderMethods : ReflectionLoader {
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
		static FastStaticFieldInfo<IDictionary<string, ModKeybind>> modKeybinds;
		internal static Func<ModKeybind, string> _get_FullName;
		public static IEnumerable<ModKeybind> Keybinds => modKeybinds.Value.Values;
		public static bool TryGet(string name, out ModKeybind keybind) => modKeybinds.Value.TryGetValue(name, out keybind);
		public override void OnLoad() {
			DynamicMethod getterMethod = new($"{nameof(ModKeybind)}.get_FullName", typeof(string), [typeof(ModKeybind)], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.EmitCall(OpCodes.Call, typeof(ModKeybind).GetProperty("FullName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod, null);
			gen.Emit(OpCodes.Ret);

			_get_FullName = getterMethod.CreateDelegate<Func<ModKeybind, string>>();
		}
	}
}
