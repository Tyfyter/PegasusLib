#nullable enable
using MonoMod.Utils;
using PegasusLib.DynamicCode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PegasusLib;
public abstract class AutoModCall : ILoadable, IModType {
	readonly List<CallData> calls = new();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public Mod Mod { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public virtual string Name => GetType().Name;
	public virtual bool GetCallingMod => false;
	static bool canGetCallingMod = false;
	public string FullName => $"{Mod.Name}/{Name}";
	protected static Mod? CallingMod {
		get => canGetCallingMod ? field : throw new InvalidOperationException($"{nameof(GetCallingMod)} must be true or {nameof(GetCallingModAttribute)} must be used to get the calling mod");
		private set => field = value;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static bool TryCall(Mod mod, object[] args, out object result) {
		result = TryDoCall(mod, args, out bool callExists);
		return callExists;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object TryDoCall(Mod mod, object[] args, out bool callExists) {
		string name = (string)args[0];
		args = args[1..];
		callExists = mod.TryFind(name, out AutoModCall call);
		return call?.Invoke(args)!;
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static object DoCall(Mod mod, object[] args) {
		string name = (string)args[0];
		args = args[1..];
		return mod.Find<AutoModCall>(name).Invoke(args);
	}
	[MethodImpl(MethodImplOptions.NoInlining)]
	public object Invoke(object[] args) {
		ParameterSequence sequence = new(args);
		if (!GetApprorpriateCall(sequence, out CallData call)) {
			string correction;
			if (calls.Count == 1) {
				correction = $"Correct parameters are {calls[0].Parameters}";
			} else {
				correction = $"Available overloads are {string.Join(", ", calls.Select(p => p.Parameters.ToString()))}";
			}
			throw new KeyNotFoundException($"Cannot find call {FullName}{sequence}, {correction}");
		}
		CallingMod = null;
		canGetCallingMod = false;
		if (call.CanGetCallingMod) {
			canGetCallingMod = true;
			StackTrace trace = new(0);
			for (int i = 4; i < trace.FrameCount; i++) {
				if (trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly is not Assembly assembly) continue;
				if (assembly.GetMod() is Mod callingMod) {
					CallingMod = callingMod;
					break;
				}
			}
		}
		call.Parameters.CastDelegates(args);
		call.Parameters.CastEnums(args);
		return call.Call(args);
	}
	public static TDelegate GetCall<TDelegate>(Mod mod, string name) where TDelegate : Delegate {
		return mod.Find<AutoModCall>(name).GetOverload<TDelegate>();
	}
	public TDelegate GetOverload<TDelegate>() where TDelegate : Delegate {
		MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke")!;
		ParameterSequence sequence = new(invoke.GetParameters());
		if (!GetApprorpriateCall(sequence, out CallData call)) {
			string correction;
			if (calls.Count == 1) {
				correction = $"Correct parameters are {calls[0].Parameters}";
			} else {
				correction = $"Available overloads are {string.Join(", ", calls.Select(p => p.Parameters.ToString()))}";
			}
			throw new KeyNotFoundException($"Cannot find call {FullName}{sequence}, {correction}");
		}
		if (call.OriginalMethod.ReturnType != invoke.ReturnType) throw new ArgumentException($"{Name}({sequence}) returns {call.OriginalMethod.ReturnType}, {nameof(TDelegate)} returns {invoke.ReturnType}");
		if (!call.CanGetCallingMod && call.OriginalMethod.TryCreateDelegate<TDelegate>() is TDelegate result) return result;
		ParameterInfo[] parameters = call.OriginalMethod.GetParameters();
		for (int i = 0; i < sequence.Length; i++) if (sequence[i] != parameters[i].ParameterType) throw new ArgumentException($"Parameter types must be exactly equal to use GetCall");

		DynamicMethod delCall = new("Call", invoke.ReturnType, call.CanGetCallingMod ? [typeof(Mod), ..invoke.GetParameters().Select(p => p.ParameterType)] : invoke.GetParameters().Select(p => p.ParameterType).ToArray());
		ILGenerator gen = delCall.GetILGenerator();
		if (invoke.ReturnType != typeof(void)) gen.DeclareLocal(invoke.ReturnType);
		Label tryLabel = default;
		int parameterOffset = 0;
		if (call.CanGetCallingMod) {
			gen.Emit(OpCodes.Ldc_I4_1);
			gen.Emit(OpCodes.Stsfld, typeof(AutoModCall).GetField(nameof(canGetCallingMod), BindingFlags.NonPublic | BindingFlags.Static)!);
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Call, typeof(AutoModCall).GetProperty(nameof(CallingMod), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.SetMethod!);
			tryLabel = gen.BeginExceptionBlock();
			parameterOffset = 1;
		}
		{
			int i = 0;
			for (; i < sequence.Length; i++) gen.Emit(OpCodes.Ldarg, i + parameterOffset);
			for (; i < parameters.Length; i++) GenerateDefaultParameter(gen, parameters[i]);
		}
		gen.Emit(OpCodes.Call, call.OriginalMethod);

		if (call.CanGetCallingMod) {
			if (invoke.ReturnType != typeof(void)) gen.Emit(OpCodes.Stloc_0);
			gen.Emit(OpCodes.Leave_S, tryLabel);
			
			gen.BeginFinallyBlock();
			gen.Emit(OpCodes.Ldc_I4_0);
			gen.Emit(OpCodes.Stsfld, typeof(AutoModCall).GetField(nameof(canGetCallingMod), BindingFlags.NonPublic | BindingFlags.Static)!);
			gen.Emit(OpCodes.Endfinally);
			
			gen.EndExceptionBlock();
			if (invoke.ReturnType != typeof(void)) gen.Emit(OpCodes.Ldloc_0);
			gen.Emit(OpCodes.Ret);
			StackTrace trace = new(0);
			for (int i = 3; i < trace.FrameCount; i++) {
				var aaaaaaa = trace.GetFrame(i);
				if (trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly is not Assembly assembly) continue;
				if (assembly.GetMod() is Mod callingMod) {
					return delCall.CreateDelegate<TDelegate>(callingMod);
				}
			}
			throw new InvalidOperationException($"GetCall must be called from within a recognizable assembly if the requested call can get the calling mod");
		}
		gen.Emit(OpCodes.Ret);
		return delCall.CreateDelegate<TDelegate>();
	}
	bool GetApprorpriateCall(ParameterSequence sequence, out CallData call) {
		List<CallData> matchingCalls = [];
		int maxStretch = int.MaxValue;
		foreach (CallData callData in calls) {
			if (callData.Parameters.CanAccept(sequence, out int stretching)) {
				if (Minimize(ref maxStretch, stretching)) matchingCalls.Clear();
				else if (stretching > maxStretch) continue;
				matchingCalls.Add(callData);
			}
		}
		switch (matchingCalls.Count) {
			case 0:
			call = default;
			return false;
			case 1:
			call = matchingCalls[0];
			return true;
			default:
			throw new AmbiguousMatchException($"Ambiguous match found for {FullName}{sequence} found: {string.Join(", ", matchingCalls.Select(c => c.Parameters.ToString()))}");
		}
	}
	void ILoadable.Load(Mod mod) {
		Mod = mod;
		GenerateCalls(GetType().GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static).Where(method => method.Name == "Call").ToArray());
		if (calls.Count == 0) throw new NotImplementedException($"{nameof(AutoModCall)} must have at least one public static Call method");
		ModTypeLookup<AutoModCall>.Register(this);
		ModTypeLookup<AutoModCall>.RegisterLegacyNames(this, [Name.ToLowerInvariant(), Name.ToUpperInvariant()]);
		ModTypeLookup<AutoModCall>.RegisterLegacyNames(this, LegacyNameAttribute.GetLegacyNamesOfType(GetType()).SelectMany<string, string>(n => [n.ToLowerInvariant(), n.ToUpperInvariant()]).ToArray());
		Load();
	}
	bool ILoadable.IsLoadingEnabled(Mod mod) => !PreLoadedAutoModCalls.Contains(mod) && IsLoadingEnabled(mod);
	public virtual bool IsLoadingEnabled(Mod mod) => true;
	public virtual void Load() { }
	void GenerateCalls(MethodInfo[] methods) {
		bool[] broken = new bool[methods.Length];
		ParameterInfo[][] parameterLists = methods.Select(m => m.GetParameters()).ToArray();
		for (int i = 0; broken.Contains(false); i++) {
			for (int m = 0; m < methods.Length; m++) {
				if (broken[m]) continue;
				ParameterSequence parameterSequence = new(parameterLists[m].Take(parameterLists[m].Length - i));
				calls.Add(new(
					parameterSequence,
					GenerateCall(methods[m], parameterSequence.Length),
					GetCallingMod || methods[m].GetCustomAttribute<GetCallingModAttribute>() is not null,
					methods[m]
				));
				if (i >= parameterLists[m].Length) broken[m] = true;
				else if (!parameterLists[m][^(i + 1)].HasDefaultValue && parameterLists[m][^(i + 1)].GetCustomAttribute<DefaultValueAttribute>() is null) broken[m] = true;
			}
		}
	}
	private static ModCall GenerateCall(MethodInfo method, int length) {
		ParameterInfo[] parameters = method.GetParameters();
		DynamicMethod call = new("Call", typeof(object), [typeof(object[])]);
		ILGenerator gen = call.GetILGenerator();
		List<(LocalBuilder local, int index)> locals = [];

		for (int i = 0; i < length; i++) {
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4, i);
			if (parameters[i].ParameterType.IsByRef) {
				locals.Add((gen.DeclareLocal(parameters[i].ParameterType.GetElementType()!), i));
				gen.Emit(OpCodes.Ldelem, typeof(object));
				gen.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType.GetElementType()!);
				gen.Emit(OpCodes.Stloc, locals[^1].local);
				gen.Emit(OpCodes.Ldloca, locals[^1].local);
			} else {
				gen.Emit(OpCodes.Ldelem, typeof(object));
				gen.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
			}
		}
		for (int i = length; i < parameters.Length; i++) {
			GenerateDefaultParameter(gen, parameters[i]);
		}

		gen.Emit(OpCodes.Call, method);

		if (method.ReturnType == typeof(void)) gen.Emit(OpCodes.Ldnull);
		else if (method.ReturnType.IsValueType) gen.Emit(OpCodes.Box, method.ReturnType);

		for (int i = 0; i < locals.Count; i++) {
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4, locals[i].index);
			gen.Emit(OpCodes.Ldloc, locals[i].local);
			if (locals[i].local.LocalType.IsValueType) gen.Emit(OpCodes.Box, locals[i].local.LocalType);
			gen.Emit(OpCodes.Stelem, typeof(object));
		}

		gen.Emit(OpCodes.Ret);

		return call.CreateDelegate<ModCall>();
	}
	static void GenerateDefaultParameter(ILGenerator gen, ParameterInfo parameter) {
		if (parameter.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute defaultValue) {
			Type retType = defaultValue.Generate(gen, parameter);
			if (retType != parameter.ParameterType) throw new ArgumentException($"Invalid default value type, {retType} != {parameter.ParameterType}", defaultValue.ToString());
			return;
		}
		switch (parameter.DefaultValue) {
			case string value:
			gen.Emit(OpCodes.Ldstr, value);
			break;

			case int value:
			gen.Emit(OpCodes.Ldc_I4, value);
			break;

			case long value:
			gen.Emit(OpCodes.Ldc_I8, value);
			break;

			case float value:
			gen.Emit(OpCodes.Ldc_R4, value);
			break;

			case double value:
			gen.Emit(OpCodes.Ldc_R8, value);
			break;

			default:
			LocalBuilder local = gen.DeclareLocal(parameter.ParameterType);
			gen.Emit(OpCodes.Ldloca, local);
			gen.Emit(OpCodes.Initobj, parameter.ParameterType);
			gen.Emit(OpCodes.Ldloc, local);
			break;
		}
	}
	void ILoadable.Unload() { }
	delegate object ModCall(object[] args);
	class ParameterSequence {
		readonly ParameterType[] parameters;
		readonly bool[] isByRef;
		public readonly bool containsNulls;
		public Type this[int i] => parameters[i].type;
		public int Length => parameters.Length;
		public ParameterSequence(Type?[] parameters) {
			this.parameters = parameters.Select(t => new ParameterType(t ?? typeof(NullParameter))).ToArray();
			isByRef = new bool[parameters.Length];
			for (int i = 0; i < this.parameters.Length; i++) {
				if (this.parameters[i].type.IsByRef) {
					isByRef[i] = true;
					this.parameters[i] = this.parameters[i].type.GetElementType() ?? typeof(NullParameter);
				}
			}
			containsNulls = this.parameters.Contains(typeof(NullParameter));
		}
		public ParameterSequence(IEnumerable<object> parameters) : this(parameters.Select(p => p?.GetType()).ToArray()) { }
		public ParameterSequence(IEnumerable<ParameterInfo> parameters) : this(parameters.Select(p => p.ParameterType).ToArray()) { }
		public ParameterSequence(MethodInfo method) : this(method.GetParameters()) { }
		public override bool Equals(object? obj) => obj is ParameterSequence other && Equals(other);
		public bool Equals(ParameterSequence other) {
			if (other.parameters.Length != parameters.Length) return false;
			for (int i = 0; i < parameters.Length; i++) {
				if (other.parameters[i] != parameters[i]) return false;
			}
			return true;
		}
		public bool CanAccept(ParameterSequence other, out int stretching) {
			stretching = 0;
			if (other.parameters.Length != parameters.Length) return false;
			for (int i = 0; i < parameters.Length; i++) {
				if (!parameters[i].CanAccept(other.parameters[i], ref stretching)) return false;
			}
			return true;
		}
		public static bool operator ==(ParameterSequence left, ParameterSequence right) => left.Equals(right);
		public static bool operator !=(ParameterSequence left, ParameterSequence right) => !(left == right);
		public void CastDelegates(object[] args) {
			for (int i = 0; i < args.Length; i++) {
				if (parameters[i].IsDelegate && args[i].GetType() != parameters[i].type) args[i] = ((Delegate)args[i]).CastDelegate(parameters[i].type);
			}
		}
		public void CastEnums(object[] args) {
			try {
				for (int i = 0; i < args.Length; i++) {
					if (parameters[i].parseDict is not Dictionary<string, object> parseDict) continue;
					if (args[i] is string arg) args[i] = parseDict[arg];
					else if (args[i].GetType().IsEnum) args[i] = parseDict[args[i].ToString()!];
				}
			} catch (KeyNotFoundException e) {
				throw;
			}
		}
		public override int GetHashCode() {
			HashCode code = default;
			for (int i = 0; i < parameters.Length; i++) code.Add(parameters[i]);
			return code.ToHashCode();
		}
		public override string ToString() {
			StringBuilder builder = new("[");
			for (int i = 0; i < parameters.Length; i++) {
				if (i > 0) builder.Append(", ");
				if (parameters[i] == typeof(NullParameter)) {
					builder.Append("null");
				} else {
					builder.Append(parameters[i].ToString());
				}
				if (isByRef[i]) builder.Append('&');
			}
			builder.Append(']');
			return builder.ToString();
		}
	}
	public readonly struct ParameterType(Type type) {
		public readonly Type type = type;
		readonly DelegateSignature? delegateSignature = type.IsAssignableTo(typeof(Delegate)) ? 
			new(type.GetMethod("Invoke")!)
			: null;
		public readonly Dictionary<string, object>? parseDict = type.IsEnum ?
			new Dictionary<string, object>(Enum.GetValues(type).Cast<object>().Select(static value => new KeyValuePair<string, object>(value.ToString()!, value)), StringComparer.OrdinalIgnoreCase)
			: null;
		public readonly bool IsDelegate => delegateSignature is not null;
		public override string ToString() => IsDelegate ? $"delegate {delegateSignature!.ReturnType} {delegateSignature.Parameters}" : type.ToString();
		public override bool Equals([NotNullWhen(true)] object? obj) {
			if (obj is not ParameterType other) return false;
			switch ((delegateSignature, other.delegateSignature)) {
				case (DelegateSignature, DelegateSignature):
				return delegateSignature.Equals(other.delegateSignature);

				case (null, null):
				return type.Equals(other.type);
			}
			return false;
		}
		public bool CanAccept(ParameterType other, ref int stretching) {
			switch ((delegateSignature, other.delegateSignature)) {
				case (DelegateSignature, DelegateSignature):
				return delegateSignature.Equals(other.delegateSignature);

				case (null, null):
				if (parseDict is not null && (other.type.IsEnum || other.type == typeof(string))) {
					stretching++;
					return true;
				}
				return type.IsAssignableFrom(other.type) || (other.type == typeof(NullParameter) && (type.IsClass || type.IsInterface));
			}
			return false;
		}
		public static implicit operator ParameterType(Type type) => new(type);
		public override int GetHashCode() => delegateSignature?.GetHashCode() ?? type.GetHashCode();
		public static bool operator ==(ParameterType left, ParameterType right) => left.Equals(right);
		public static bool operator !=(ParameterType left, ParameterType right) => !(left == right);
		sealed record class DelegateSignature(ParameterSequence Parameters, ParameterType ReturnType) {
			public DelegateSignature(MethodInfo invoke) : this(new(invoke), invoke.ReturnType) { }
		}
	}
	[AttributeUsage(AttributeTargets.Parameter)]
	protected class DefaultValueAttribute(Type inType, params string[] path) : Attribute {
		/// <summary>
		/// Uses the containing type and parameter name as the type and path
		/// </summary>
		public DefaultValueAttribute() : this(null!) { }
		public Type Generate(ILGenerator gen, ParameterInfo forParameter) {
			inType ??= forParameter.Member.DeclaringType!;
			Type type = inType;
			if (path.Length == 0) path = [forParameter.Name!];
			static MemberInfo GetValidMember(Type type, string name, BindingFlags bindingFlags) {
				bindingFlags |= BindingFlags.Public | BindingFlags.NonPublic;
				if (type.GetField(name, bindingFlags) is FieldInfo field) return field;
				if (type.GetProperty(name, bindingFlags) is PropertyInfo property) {
					if (property.GetGetMethod() is not MethodInfo getter) throw new ArgumentException($"Property in default value path must have a getter, {property} does not have a getter", nameof(name));
					return getter;
				}
				if (type.GetMethod(name, bindingFlags, []) is MethodInfo method) return method;
				throw new KeyNotFoundException($"A valid {bindingFlags & (BindingFlags.Instance | BindingFlags.Static)} field, property, or method named {name} is not present in {type}");
			}
			switch (GetValidMember(type, path[0], BindingFlags.Static)) {
				case FieldInfo field:
				gen.Emit(OpCodes.Ldsfld, field);
				type = field.FieldType;
				break;
				case MethodInfo method:
				gen.Emit(OpCodes.Call, method);
				type = method.ReturnType;
				break;
			}
			for (int i = 1; i < path.Length; i++) {
				switch (GetValidMember(type, path[i], BindingFlags.Instance)) {
					case FieldInfo field:
					gen.Emit(OpCodes.Ldfld, field);
					type = field.FieldType;
					break;
					case MethodInfo method:
					gen.Emit(OpCodes.Call, method);
					type = method.ReturnType;
					break;
				}
			}
			return type;
		}
		public override string ToString() => $"{inType}.{string.Join('.', path)}";
	}
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	protected sealed class GetCallingModAttribute : Attribute { }
	record struct CallData(ParameterSequence Parameters, ModCall Call, bool CanGetCallingMod, MethodInfo OriginalMethod);
	struct NullParameter;
	readonly static HashSet<Mod> PreLoadedAutoModCalls = [];
	readonly static Action<Mod, ILoadable> Content_Add = PegasusLib.Compile<Action<Mod, ILoadable>>("Content_Add",
		OpCodes.Ldarg_0,
		Instr.Get<Mod>("Content"),
		OpCodes.Ldarg_1,
		Instr.CallV(typeof(Mod).Assembly.GetType("Terraria.ModLoader.ContentCache"), "Add"),
		OpCodes.Ret
	);
	public static void PreLoadAutoModCalls(Mod mod) => PegasusLib.PreLoadMods += () => {
		LoaderUtils.ForEachAndAggregateExceptions(
			(from t in AssemblyManager.GetLoadableTypes(mod.Code)
			 where !t.IsAbstract && !t.ContainsGenericParameters && t.IsAssignableTo(typeof(AutoModCall))
			 where AutoloadAttribute.GetValue(t).NeedsAutoloading
			 select t)
			.OrderBy(type => type.FullName, StringComparer.InvariantCulture),
			type => {
				ILoadable call = (ILoadable)Activator.CreateInstance(type)!;
				if (!call.IsLoadingEnabled(mod)) return;
				call.Load(mod);
				Content_Add(mod, call);
				ContentInstance.Register(call);
			}
		);
		PreLoadedAutoModCalls.Add(mod);
	};
}
#if DEBUG
public class TestCall : AutoModCall {
	[GetCallingMod]
	public static string Call(string value, int test = 7) {
		switch (value) {
			case "Bees?":
			return "Bees!";
		}
		return CallingMod?.Name ?? "No calling mod";
	}
	public static string Call(ModItem value) => value.FullName;
	public enum TestEnum {
		ONE,
		TWO,
		TWENTY_SEVEN
	}
	public static void Call(object _, TestEnum value) {
		ModContent.GetInstance<PegasusLib>().Logger.Info($"Result: {value}");
	}
	public static void Call(object _, string value) {
		ModContent.GetInstance<PegasusLib>().Logger.Info($"Different result: {value}");
	}
	public static string Call(TestEnum value, int test = 6) {
		return $"Result: {value} ({test})";
	}
	[GetCallingMod]
	public static void Call(int value, int test = 7) {
		ModContent.GetInstance<PegasusLib>().Logger.Info($"{value + test}: {(CallingMod?.Name ?? "No calling mod")}");
	}
}
#endif