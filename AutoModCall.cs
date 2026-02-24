using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Terraria.ModLoader;

namespace PegasusLib;
public abstract class AutoModCall : ILoadable, IModType {
	readonly Dictionary<ParameterSequence, ModCall> calls = [];
	public Mod Mod { get; private set; }
	public virtual string Name => GetType().Name;
	public string FullName => $"{Mod.Name}/{Name}";
	public static object DoCall(Mod mod, object[] args) {
		string name = (string)args[0];
		args = args[1..];
		return mod.Find<AutoModCall>(name).Invoke(args);
	}
	public object Invoke(object[] args) {
		ParameterSequence sequence = new(args);
		if (!calls.TryGetValue(sequence, out ModCall call)) {
			string correction;
			if (calls.Count == 1) {
				correction = $"Correct parameters are {calls.Keys.First()}";
			} else {
				correction = $"Available overloads are {string.Join(", ", calls.Keys)}";
			}
			throw new KeyNotFoundException($"Cannot find call {FullName}{sequence}, {correction}");
		}
		return call(args);
	}
	void ILoadable.Load(Mod mod) {
		Mod = mod;
		foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static)) {
			if (method.Name != "Call") continue;
			GenerateCalls(method);
		}
		if (calls.Count == 0) throw new NotImplementedException($"{nameof(AutoModCall)} must have at least one public static Call method");
		ModTypeLookup<AutoModCall>.Register(this);
	}
	void GenerateCalls(MethodInfo method) {
		ParameterInfo[] parameters = method.GetParameters();
		for (int i = parameters.Length; i >= 0; i--) {
			GenerateCall(method, i);
			if (i <= 0) break;
			if (!parameters[i - 1].HasDefaultValue && parameters[i - 1].GetCustomAttribute<DefaultValueAttribute>() is null) break;
		}
	}
	void GenerateCall(MethodInfo method, int length) {
		ParameterInfo[] parameters = method.GetParameters();
		DynamicMethod call = new("Call", typeof(object), [typeof(object[])]);
		ILGenerator gen = call.GetILGenerator();
		List<(LocalBuilder local, int index)> locals = [];

		for (int i = 0; i < length; i++) {
			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldc_I4, i);
			if (parameters[i].ParameterType.IsByRef) {
				locals.Add((gen.DeclareLocal(parameters[i].ParameterType.GetElementType()), i));
				gen.Emit(OpCodes.Ldelem, typeof(object));
				gen.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType.GetElementType());
				gen.Emit(OpCodes.Stloc, locals[^1].local);
				gen.Emit(OpCodes.Ldloca, locals[^1].local);
			} else {
				gen.Emit(OpCodes.Ldelem, typeof(object));
				gen.Emit(OpCodes.Unbox_Any, parameters[i].ParameterType);
			}
		}
		for (int i = length; i < parameters.Length; i++) {
			if (parameters[i].GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute defaultValue) {
				Type retType = defaultValue.Generate(gen, parameters[i]);
				if (retType != parameters[i].ParameterType) throw new ArgumentException($"Invalid default value type, {retType} != {parameters[i].ParameterType}", defaultValue.ToString());
				continue;
			}
			switch (parameters[i].DefaultValue) {
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
				LocalBuilder local = gen.DeclareLocal(parameters[i].ParameterType);
				gen.Emit(OpCodes.Ldloca, local);
				gen.Emit(OpCodes.Initobj, parameters[i].ParameterType);
				gen.Emit(OpCodes.Ldloc, local);
				break;
			}
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

		calls[new ParameterSequence(parameters.Take(length))] = call.CreateDelegate<ModCall>();
	}
	void ILoadable.Unload() { }
	delegate object ModCall(object[] args);
	class ParameterSequence {
		readonly Type[] parameters;
		readonly bool[] isByRef;
		public int Length => parameters.Length;
		public ParameterSequence(Type[] parameters) {
			this.parameters = parameters.ToArray();
			isByRef = new bool[parameters.Length];
			for (int i = 0; i < this.parameters.Length; i++) {
				if (this.parameters[i].IsByRef) {
					isByRef[i] = true;
					this.parameters[i] = this.parameters[i].GetElementType();
				}
			}
		}
		public ParameterSequence(IEnumerable<object> parameters) : this(parameters.Select(p => p.GetType()).ToArray()) { }
		public ParameterSequence(IEnumerable<ParameterInfo> parameters) : this(parameters.Select(p => p.ParameterType).ToArray()) { }
		public ParameterSequence(MethodInfo method) : this(method.GetParameters()) { }
		public override bool Equals(object obj) => obj is ParameterSequence other && Equals(other);
		public bool Equals(ParameterSequence other) {
			if (other.parameters.Length != parameters.Length) return false;
			for (int i = 0; i < parameters.Length; i++) {
				if (other.parameters[i] != parameters[i]) return false;
			}
			return true;
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
				builder.Append(parameters[i].ToString());
				if (isByRef[i]) builder.Append('&');
			}
			builder.Append(']');
			return builder.ToString();
		}
	}
	[AttributeUsage(AttributeTargets.Parameter)]
	protected class DefaultValueAttribute(Type inType, params string[] path) : Attribute {
		/// <summary>
		/// Uses the containing type and parameter name as the type and path
		/// </summary>
		public DefaultValueAttribute() : this(null) { }
		public Type Generate(ILGenerator gen, ParameterInfo forParameter) {
			inType ??= forParameter.Member.DeclaringType;
			Type type = inType;
			if (path.Length == 0) path = [forParameter.Name];
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
}
