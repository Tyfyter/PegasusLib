using System;
using System.Reflection.Emit;
using System.Reflection;

namespace PegasusLib {
	public class FastFieldInfo<TParent, T> {
		public readonly FieldInfo field;
		RefGetter refGetter;
		Func<TParent, T> getter;
		Action<TParent, T> setter;
		public FastFieldInfo(string name, BindingFlags bindingFlags, bool init = false) {
			field = typeof(TParent).GetField(name, bindingFlags | BindingFlags.Instance);
			if (field is null) throw new ArgumentException($"could not find {name} in type {typeof(TParent)} with flags {bindingFlags}");
			if (init) {
				refGetter = CreateRefGetter();
				getter = CreateGetter();
				setter = CreateSetter();
			}
		}
		public FastFieldInfo(FieldInfo field, bool init = false) {
			this.field = field;
			if (init) {
				refGetter = CreateRefGetter();
				getter = CreateGetter();
				setter = CreateSetter();
			}
		}
		public T GetValue(TParent parent) {
			return (getter ??= CreateGetter())(parent);
		}
		public void SetValue<Ref_TParent>(Ref_TParent parent, T value) where Ref_TParent : class, TParent {
			(setter ??= CreateSetter())(parent, value);
		}
		public void SetValue(ref TParent parent, T value) {
			(refGetter ??= CreateRefGetter())(ref parent) = value;
		}
		private delegate ref T RefGetter(ref TParent parent);
		private Func<TParent, T> CreateGetter() {
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod getterMethod = new(methodName, typeof(T), [typeof(TParent)], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, field);
			gen.Emit(OpCodes.Ret);

			return (Func<TParent, T>)getterMethod.CreateDelegate(typeof(Func<TParent, T>));
		}
		private Action<TParent, T> CreateSetter() {
			if (!field.FieldType.IsClass) return null;
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
			DynamicMethod setterMethod = new(methodName, null, [typeof(TParent), typeof(T)], true);
			ILGenerator gen = setterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Stfld, field);
			gen.Emit(OpCodes.Ret);

			return (Action<TParent, T>)setterMethod.CreateDelegate(typeof(Action<TParent, T>));
		}
		private RefGetter CreateRefGetter() {
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod getterMethod = new(methodName, typeof(T).MakeByRefType(), [typeof(TParent).MakeByRefType()], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldflda, field);
			gen.Emit(OpCodes.Ret);

			return getterMethod.CreateDelegate<RefGetter>();
		}
	}
	public class FastStaticFieldInfo<TParent, T>(string name, BindingFlags bindingFlags, bool init = false) : FastStaticFieldInfo<T>(typeof(TParent), name, bindingFlags, init) { }
	public class FastStaticFieldInfo<T> {
		public readonly FieldInfo field;
		RefGetter refGetter;
		Func<T> getter;
		Action<T> setter;
		public FastStaticFieldInfo(Type type, string name, BindingFlags bindingFlags, bool init = false) {
			field = type.GetField(name, bindingFlags | BindingFlags.Static);
			if (field is null) throw new InvalidOperationException($"No such static field {name} exists");
			if (init) {
				refGetter = CreateRefGetter();
				getter = CreateGetter();
				setter = CreateSetter();
			}
		}
		public FastStaticFieldInfo(FieldInfo field, bool init = false) {
			if (!field.IsStatic) throw new InvalidOperationException($"field {field.Name} is not static");
			this.field = field;
			if (init) {
				refGetter = CreateRefGetter();
				getter = CreateGetter();
				setter = CreateSetter();
			}
		}
		public ref T Value => ref (refGetter ??= CreateRefGetter())();
		public T GetValue() {
			return (getter ??= CreateGetter())();
		}
		public void SetValue(T value) {
			(setter ??= CreateSetter())(value);
		}
		private Func<T> CreateGetter() {
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod getterMethod = new(methodName, typeof(T), [], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldsfld, field);
			gen.Emit(OpCodes.Ret);

			return (Func<T>)getterMethod.CreateDelegate(typeof(Func<T>));
		}
		private Action<T> CreateSetter() {
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
			DynamicMethod setterMethod = new(methodName, null, [typeof(T)], true);
			ILGenerator gen = setterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Stsfld, field);
			gen.Emit(OpCodes.Ret);

			return (Action<T>)setterMethod.CreateDelegate(typeof(Action<T>));
		}
		private delegate ref T RefGetter();
		private RefGetter CreateRefGetter() {
			if (field.FieldType != typeof(T)) throw new InvalidOperationException($"type of {field.Name} does not match provided type {typeof(T)}");
			string methodName = field.ReflectedType.FullName + ".getref_" + field.Name;
			DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(T).MakeByRefType(), [], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldsflda, field);
			gen.Emit(OpCodes.Ret);

			return getterMethod.CreateDelegate<RefGetter>();
		}
		public static explicit operator T(FastStaticFieldInfo<T> fastFieldInfo) {
			return fastFieldInfo.GetValue();
		}
	}
}
