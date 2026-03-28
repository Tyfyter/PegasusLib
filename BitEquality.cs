/*using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PegasusLib; 
public static class BitEquality<T> {
	public static new Func<T, T, bool> Equals { get; }
	static unsafe bool LargeEquality(T a, T b) {
		int size = Unsafe.SizeOf<T>();
		byte* x = (byte*)(void*)&a;
		byte* y = (byte*)(void*)&b;
		for (int i = 0; i < size; i++) {
			if (*(x + i) != *(y + i)) return false;
		}
		return true;
	}
	static BitEquality() {
		if (Unsafe.SizeOf<T>() > Unsafe.SizeOf<nint>()) goto boottoobig;
		try {
			Equals = PegasusLib.Compile<Func<T, T, bool>>("Equals",
				(OpCodes.Ldarg_0, null),
				(OpCodes.Ldarg_1, null),
				(OpCodes.Ceq, null),
				(OpCodes.Ret, null)
			);
			_ = Equals(default, default);
			return;
		} catch (Exception) { }
		boottoobig:
		Equals = LargeEquality;
	}
}
*/