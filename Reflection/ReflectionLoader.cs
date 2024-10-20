using System;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Terraria.ModLoader;

namespace PegasusLib.Reflection {
	public class DelegateMethods : ILoadable {
		public static FastFieldInfo<Delegate, object> _target;
		public void Load(Mod mod) {
			_target = new("_target", BindingFlags.NonPublic | BindingFlags.Instance);
		}
		public void Unload() {
			_target = null;
		}
	}
	public abstract class ReflectionLoader : ILoadable {
		public abstract Type HostType { get; }
		public void Load(Mod mod) {
			LoadReflections(HostType);
		}
		public void Unload() {
			UnloadReflections(HostType);
		}
		~ReflectionLoader() {
			Unload();
		}
		public static T MakeInstanceCaller<T>(Type type, string name) where T : Delegate {
			return MakeInstanceCaller<T>(type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
		}
		public static T MakeInstanceCaller<T>(MethodInfo method) where T : Delegate {
			string methodName = method.ReflectedType.FullName + ".call_" + method.Name;
			MethodInfo invoke = typeof(T).GetMethod("Invoke");
			ParameterInfo[] parameters = invoke.GetParameters();
			DynamicMethod getterMethod = new(methodName, invoke.ReturnType, parameters.Select(p => p.ParameterType).ToArray(), true);
			ILGenerator gen = getterMethod.GetILGenerator();

			for (int i = 0; i < parameters.Length; i++) {
				gen.Emit(OpCodes.Ldarg_S, i);
			}
			gen.Emit(OpCodes.Call, method);
			gen.Emit(OpCodes.Ret);

			return getterMethod.CreateDelegate<T>();
		}
		public static void LoadReflections(Type type) {
			foreach (FieldInfo item in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
				if (item.Name.StartsWith('<')) continue;
				LoadReflection(item);
				/*string name = item.GetCustomAttribute<ReflectionMemberNameAttribute>()?.MemberName ?? item.Name;
				if (item.FieldType.IsAssignableTo(typeof(Delegate))) {
					Type parentType = item.GetCustomAttribute<ReflectionParentTypeAttribute>().ParentType;
					ParameterInfo[] parameters = item.FieldType.GetMethod("Invoke").GetParameters();
					Type[] paramTypes = new Type[parameters.Length];
					//ParameterModifier paramMods = new ParameterModifier(parameters.Length);
					for (int i = 0; i < parameters.Length; i++) {
						paramTypes[i] = parameters[i].ParameterType;
						//paramMods[i] = parameters[i].ParameterType.IsByRef;
					}
					MethodInfo info = parentType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, paramTypes);
					if (info.IsStatic) {
						item.SetValue(null, info.CreateDelegate(item.FieldType));
					} else {
						object target;
						if (item.GetCustomAttribute<ReflectionDefaultInstanceAttribute>() is ReflectionDefaultInstanceAttribute defaultObject) {
							static object GetValue(object source, string name) {
								if (source is Type type) {
									source = null;
								} else {
									type = source.GetType();
								}
								BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (source is null ? BindingFlags.Static : BindingFlags.Instance);
								if (type.GetField(name, flags) is FieldInfo field) {
									return field.GetValue(source);
								} else {
									return type.GetProperty(name, flags).GetValue(source);
								}
							}
							target = GetValue(defaultObject.Type, defaultObject.FieldNames[0]);
							for (int i = 1; i < defaultObject.FieldNames.Length; i++) {
								target = GetValue(target, defaultObject.FieldNames[i]);
							}
						} else {
							target = Activator.CreateInstance(parentType);
						}
						item.SetValue(null, info.CreateDelegate(item.FieldType, target));
					}
				} else if (item.FieldType.IsGenericType) {
					Type genericType = item.FieldType.GetGenericTypeDefinition();
					if (genericType == typeof(FastFieldInfo<,>) || genericType == typeof(FastStaticFieldInfo<,>)) {
						item.SetValue(
							null,
							item.FieldType.GetConstructor([typeof(string), typeof(BindingFlags), typeof(bool)])
							.Invoke([name, BindingFlags.Public | BindingFlags.NonPublic, true])
						);
					} else if (genericType == typeof(FastStaticFieldInfo<>)) {
						item.SetValue(
							null,
							item.FieldType.GetConstructor([typeof(Type), typeof(string), typeof(BindingFlags), typeof(bool)])
							.Invoke([item.GetCustomAttribute<ReflectionParentTypeAttribute>().ParentType, name, BindingFlags.Public | BindingFlags.NonPublic, true])
						);
					}
				}*/
			}
			foreach (PropertyInfo item in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
				LoadReflection(item);
			}
		}
		public static void LoadReflection(MemberInfo item) {
			Action<object, object> setValue;
			Type fieldType;
			if (item is FieldInfo field) {
				fieldType = field.FieldType;
				setValue = field.SetValue;
			} else if (item is PropertyInfo property) {
				fieldType = property.PropertyType;
				setValue = property.SetValue;
				if (setValue is null) return;
			} else {
				return;
				//throw new ArgumentException($"Invalid MemberInfo type {item.GetType()}", nameof(item));
			}
			string name = item.GetCustomAttribute<ReflectionMemberNameAttribute>()?.MemberName ?? item.Name;
			if (fieldType.IsAssignableTo(typeof(Delegate))) {
				Type parentType = item.GetCustomAttribute<ReflectionParentTypeAttribute>().ParentType;
				ParameterInfo[] parameters = fieldType.GetMethod("Invoke").GetParameters();
				Type[] paramTypes = new Type[parameters.Length];
				//ParameterModifier paramMods = new ParameterModifier(parameters.Length);
				for (int i = 0; i < parameters.Length; i++) {
					paramTypes[i] = parameters[i].ParameterType;
					//paramMods[i] = parameters[i].ParameterType.IsByRef;
				}
				MethodInfo info = parentType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, paramTypes);
				if (info.IsStatic) {
					setValue(null, info.CreateDelegate(fieldType));
				} else {
					object target;
					if (item.GetCustomAttribute<ReflectionDefaultInstanceAttribute>() is ReflectionDefaultInstanceAttribute defaultObject) {
						static object GetValue(object source, string name) {
							if (source is Type type) {
								source = null;
							} else {
								type = source.GetType();
							}
							BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (source is null ? BindingFlags.Static : BindingFlags.Instance);
							if (type.GetField(name, flags) is FieldInfo field) {
								return field.GetValue(source);
							} else {
								return type.GetProperty(name, flags).GetValue(source);
							}
						}
						target = GetValue(defaultObject.Type, defaultObject.FieldNames[0]);
						for (int i = 1; i < defaultObject.FieldNames.Length; i++) {
							target = GetValue(target, defaultObject.FieldNames[i]);
						}
					} else {
						target = Activator.CreateInstance(parentType);
					}
					setValue(null, info.CreateDelegate(fieldType, target));
				}
			} else if (fieldType.IsGenericType) {
				Type genericType = fieldType.GetGenericTypeDefinition();
				if (genericType == typeof(FastFieldInfo<,>) || genericType == typeof(FastStaticFieldInfo<,>)) {
					setValue(
					null,
						fieldType.GetConstructor([typeof(string), typeof(BindingFlags), typeof(bool)])
						.Invoke([name, BindingFlags.Public | BindingFlags.NonPublic, true])
					);
				} else if (genericType == typeof(FastStaticFieldInfo<>)) {
					setValue(
					null,
						fieldType.GetConstructor([typeof(Type), typeof(string), typeof(BindingFlags), typeof(bool)])
						.Invoke([item.GetCustomAttribute<ReflectionParentTypeAttribute>().ParentType, name, BindingFlags.Public | BindingFlags.NonPublic, true])
					);
				}
			}
		}
		public static void UnloadReflections(Type type) {
			foreach (FieldInfo item in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
				if (!item.FieldType.IsValueType) {
					item.SetValue(null, null);
				}
			}
		}
	}
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ReflectionMemberNameAttribute(string memberName) : Attribute {
		public string MemberName { get; init; } = memberName;
	}
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ReflectionParentTypeAttribute(Type type) : Attribute {
		public Type ParentType { get; init; } = type;
	}
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ReflectionDefaultInstanceAttribute(Type type, params string[] fieldNames) : Attribute {
		public Type Type { get; init; } = type;
		public string[] FieldNames { get; init; } = fieldNames;
	}
}
