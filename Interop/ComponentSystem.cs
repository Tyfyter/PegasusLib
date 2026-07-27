using PegasusLib.DynamicCode;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib.Interop;
/// <summary>
/// Used to create a category of entity for the <see cref="ComponentRegistrar{TComponent}"/> ECS <br/>
/// A static TComponent.Initialize method taking only a <see cref="ComponentRegistrar{TComponent}.Components"/> will be called after initializing all components, if one exists
/// </summary>
/// <typeparam name="TComponent">The interface or abstract class implementing this interface</typeparam>
public interface IComponentKind<TComponent> : IAutoload<ComponentRegistrar<TComponent>> where TComponent : IComponentKind<TComponent> { }
/// <summary>
/// Skips initializing this component type, even if it has a public parameterless constructor
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Constructor)]
public sealed class ComponentMayBeNullAttribute : Attribute { }
public class ComponentRegistrar<TInterface> : IAutoloader where TInterface : IComponentKind<TInterface> {
	static int Count = 0;
	static readonly List<Func<TInterface>> initializers = [];
	static readonly Action<Components> finalInitializer = typeof(TInterface).GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(Components)])?.CreateDelegate<Action<Components>>();
	public static Components Rent() => new();
	static object GetIndexValue(Type type, string name) => typeof(Index<>).MakeGenericType(typeof(TInterface), type).GetField(name).GetValue(null);
	static void SetIndexValue(Type type, string name, object value) => typeof(Index<>).MakeGenericType(typeof(TInterface), type).GetField(name).SetValue(null, value);
	public static void Autoload(Mod mod, Type type) {
		Type rootType = type;
		while (rootType.BaseType?.IsAssignableTo(typeof(TInterface)) ?? false) rootType = rootType.BaseType;

		if ((bool)GetIndexValue(rootType, nameof(Index<>.ownsIndex))) {
			object rootIndex = GetIndexValue(rootType, nameof(Index<>.index));
			SetIndexValue(type, nameof(Index<>.index), rootIndex);
		} else {
			SetIndexValue(rootType, nameof(Index<>.ownsIndex), true);
			rootType = type;
			while (rootType.BaseType is not null) {
				SetIndexValue(rootType, nameof(Index<>.index), Count);
				rootType = rootType.BaseType;
			}
			Count++;
			if (type.GetCustomAttribute<ComponentMayBeNullAttribute>() is null && type.GetConstructor([]) is ConstructorInfo ctor && ctor.GetCustomAttribute<ComponentMayBeNullAttribute>() is null) {
				initializers.Add(PegasusLib.Compile<Func<TInterface>>("Initialize",
					Instr.Newobj(ctor),
					Instr.Ret
				));
			} else {
				initializers.Add(null);
			}
		}
	}
	static class Index<TComponent> where TComponent : TInterface {
		public static int index;
		public static bool ownsIndex;
	}
	public class Components : IDisposable {
		static readonly Stack<TInterface[]> freeLists = new();
		readonly TInterface[] inner;
		internal Components() {
			if (freeLists.TryPop(out inner)) Array.Clear(inner);
			else inner = new TInterface[Count];
			for (int i = 0; i < inner.Length; i++) {
				if (initializers[i] is not null) inner[i] = initializers[i]();
			}
			finalInitializer?.Invoke(this);
		}
		public Enumerator Iterate => new(inner);
		public TComponent Get<TComponent>() where TComponent : TInterface => (TComponent)inner[Index<TComponent>.index];
		public void Set<TComponent>(TComponent value) where TComponent : TInterface => inner[Index<TComponent>.index] = value;
		~Components() => freeLists.Push(inner);
		public void Dispose() {
			freeLists.Push(inner);
			GC.SuppressFinalize(this);
		}
		public ref struct Enumerator {
			private readonly Span<TInterface> _span;
			private int _index = -1;
			internal Enumerator(Span<TInterface> span) => _span = span;
			public readonly ref TInterface Current {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref _span[_index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() {
				while (++_index < _span.Length) {
					if (Current is not null) return true;
				}
				return false;
			}
			public readonly Enumerator GetEnumerator() => this;
		}
	}
}
#if DEBUG
public class TestComponentsCommand : ModCommand {
	public override string Command => "TestComponents";
	public override CommandType Type => CommandType.Chat;
	public override void Action(CommandCaller caller, string input, string[] args) {
		ComponentRegistrar<ITestComponent>.Components components = ComponentRegistrar<ITestComponent>.Rent();
		components.Set<HealthComponent>(new PlayerHealthComponent(caller.Player));
		for (int i = 0; i < 50; ++i) foreach (ITestComponent component in components.Iterate) component.Tick(components);
		components.Set(new PoisonComponent(120));
		for (int i = 0; i < 50; ++i) foreach (ITestComponent component in components.Iterate) component.Tick(components);
		components.Set<HealthComponent>(new ArbitraryHealthComponent() { Health = 75 });
		for (int i = 0; i < 50; ++i) foreach (ITestComponent component in components.Iterate) component.Tick(components);
		components.Set<PoisonComponent>(null);
		for (int i = 0; i < 50; ++i) foreach (ITestComponent component in components.Iterate) component.Tick(components);
	}
}
interface ITestComponent : IComponentKind<ITestComponent> {
	public void Tick(ComponentRegistrar<ITestComponent>.Components components) { }
}
abstract class HealthComponent : ITestComponent {
	public abstract int Health { get; set; }
}
class PlayerHealthComponent(Player player) : HealthComponent {
	public override int Health { get => player.statLife; set => player.statLife = value; }
}
class ArbitraryHealthComponent : HealthComponent {
	public override int Health {
		get;
		set {
			field = value;
			Main.NewText(value);
		}
	}
}
[ComponentMayBeNull]
class PoisonComponent(int dp2s) : ITestComponent {
	int timer;
	void ITestComponent.Tick(ComponentRegistrar<ITestComponent>.Components components) {
		if (timer.CycleUp(120, dp2s)) components.Get<HealthComponent>().Health--;
	}
}
#endif