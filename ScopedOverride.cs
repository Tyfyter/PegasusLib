using System;

namespace PegasusLib; 
public readonly ref struct ScopedOverride<T> : IDisposable {
	private readonly ref T variable;
	private readonly T original;
	public ScopedOverride(ref T variable, T value) {
		this.variable = ref variable;
		original = variable;
		variable = value;
	}
	void IDisposable.Dispose() => variable = original;
}
