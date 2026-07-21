using PegasusLib.DynamicCode;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.ModLoader;

namespace PegasusLib.Interop; 
/// <summary>
/// Used to create a category of entity for the <see cref="ComponentRegistrar{TComponent}"/> ECS <br/>
/// A static TComponent.Initialize method taking only a <see cref="ComponentRegistrar{TComponent}.Components"/> will be called after initializing all components, if one exists
/// </summary>
/// <typeparam name="TComponent">The interface or abstract class implementing this interface</typeparam>
public interface IComponentKind<TComponent> : IAutoload<ComponentRegistrar<TComponent>> where TComponent : IComponentKind<TComponent> { }
public class ComponentRegistrar<TInterface> : IAutoloader where TInterface : IComponentKind<TInterface> {
	static int Count = 0;
	static readonly List<Func<TInterface>> initializers = [];
	static readonly Action<Components> finalInitializer = typeof(TInterface).GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(Components)])?.CreateDelegate<Action<Components>>();
	public static Components Rent() => new();
	public static void Autoload(Mod mod, Type type) {
		typeof(Index<>).MakeGenericType(typeof(TInterface), type).GetField(nameof(Index<>.index)).SetValue(null, Count);
		Count++;
		if (type.GetConstructor([]) is ConstructorInfo ctor) {
			initializers.Add(PegasusLib.Compile<Func<TInterface>>("Initialize",
				Instr.Newobj(ctor),
				Instr.Ret
			));
		} else {
			initializers.Add(null);
		}
	}
	static class Index<TComponent> where TComponent : TInterface {
		public static int index;
	}
	public class Components : IDisposable {
		static readonly Stack<TInterface[]> freeLists = new();
		readonly TInterface[] inner;
		internal Components() {
			if (!freeLists.TryPop(out inner)) inner = new TInterface[Count];
			for (int i = 0; i < inner.Length; i++) {
				if (initializers[i] is not null) inner[i] = initializers[i]();
			}
			finalInitializer?.Invoke(this);
		}
		public Span<TInterface> Iterate => inner;
		public TComponent Get<TComponent>() where TComponent : TInterface => (TComponent)inner[Index<TComponent>.index];
		public void Set<TComponent>(TComponent value) where TComponent : TInterface => inner[Index<TComponent>.index] = value;
		~Components() => freeLists.Push(inner);
		public void Dispose() {
			freeLists.Push(inner);
			GC.SuppressFinalize(this);
		}
	}
}