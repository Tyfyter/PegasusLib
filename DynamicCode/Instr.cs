using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;

namespace PegasusLib.DynamicCode; 
public record struct Instr(OpCode OpCode, object Operand) {
	public static implicit operator Instr(OpCode opCode) => new(opCode, null);
	public static implicit operator Instr((OpCode opCode, object operand) value) => new(value.opCode, value.operand ?? throw new ArgumentNullException("operand"));
	public static implicit operator Instr(string value) => new(OpCodes.Ldstr, value);
	public static implicit operator Instr(Action<ILGenerator> action) => new(OpCodes.Nop, action);
	public static Instr GenerateOperand(OpCode opCode, Func<ILGenerator, object> operand) => new(opCode, operand);
	const BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic;
	const BindingFlags any_static = access | BindingFlags.Static;
	const BindingFlags any_instance = access | BindingFlags.Instance;
	const BindingFlags any_ownership = BindingFlags.Static | BindingFlags.Instance;
	const BindingFlags any = access | any_ownership;
	public static Instr LoadField<T>(string field) => LoadFieldA(typeof(T), field);
	public static Instr LoadField(Type type, string field) {
		FieldInfo fieldInfo = type.GetField(field, any);
		return new(fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
	}
	public static Instr LoadFieldA<T>(string field) => LoadFieldA(typeof(T), field);
	public static Instr LoadFieldA(Type type, string method) {
		FieldInfo fieldInfo = type.GetField(method, any);
		return new(fieldInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, fieldInfo);
	}
	public static Instr LoadToken<T>(string field) => LoadToken(typeof(T), field);
	public static Instr LoadToken(Type type, string method) => new(OpCodes.Ldtoken, type.GetMember(method, any));
	public static Instr Call<T>(string method) => Call(typeof(T), method);
	public static Instr Call(Type type, string method) => new(OpCodes.Call, type.GetMethod(method, any));
	public static Instr CallV<T>(string method) => CallV(typeof(T), method);
	public static Instr CallV(Type type, string method) => new(OpCodes.Callvirt, type.GetMethod(method, any_instance));
	public static Instr Get<T>(string property) => Get(typeof(T), property);
	public static Instr Get(Type type, string property) => new(OpCodes.Call, type.GetProperty(property, any).GetMethod);
	public static Instr Set<T>(string property) => Set(typeof(T), property);
	public static Instr Set(Type type, string property) => new(OpCodes.Call, type.GetProperty(property, any).SetMethod);
	public static Instr VGet<T>(string property) => VGet(typeof(T), property);
	public static Instr VGet(Type type, string property) => new(OpCodes.Callvirt, type.GetProperty(property, any).GetMethod);
	public static Instr VSet<T>(string property) => VSet(typeof(T), property);
	public static Instr VSet(Type type, string property) => new(OpCodes.Callvirt, type.GetProperty(property, any).SetMethod);
	public static Instr Switch(params Label[] labels) => new(OpCodes.Switch, labels);
	public static Instr Branch(OpCode opCode, out Ref<Label> label) {
		switch (opCode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Cond_Branch:
			break;
			default:
			throw new ArgumentException("OpCode must be a branch instruction", nameof(opCode));
		}
		Ref<Label> _label = label = new();
		return GenerateOperand(opCode, gen => _label.Value = gen.DefineLabel());
	}

}
