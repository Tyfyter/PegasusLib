using System;

namespace PegasusLib; 
public readonly ref struct ScopedOverride<T> : IDisposable {
	private readonly ref T variable;
	private readonly T original;
	public ScopedOverride(FastStaticFieldInfo<T> variable, T value) : this(ref variable.Value, value) { }
	public ScopedOverride(ref T variable, T value) {
		this.variable = ref variable;
		original = variable;
		variable = value;
	}
	public void Dispose() => variable = original;
}
