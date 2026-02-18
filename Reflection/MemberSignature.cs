using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PegasusLib.Reflection {
	public record MemberSignature(string Name) {
		public static bool TryCreate(MemberInfo member, out MemberSignature signature) {
			signature = null;
			if (member is FieldInfo field && !field.IsSpecialName) signature = new FieldSignature(field);
			if (member is PropertyInfo property && !property.IsSpecialName) signature = new PropertySignature(property);
			if (member is EventInfo @event && !@event.IsSpecialName) signature = new EventSignature(@event);
			if (member is MethodInfo method && !method.IsSpecialName) signature = new MethodSignature(method);
			return signature is not null;
		}
		public static bool TryCreateIncludeSpecialNames(MemberInfo member, out MemberSignature signature) {
			signature = null;
			if (member is FieldInfo field) signature = new FieldSignature(field);
			if (member is PropertyInfo property) signature = new PropertySignature(property);
			if (member is EventInfo @event) signature = new EventSignature(@event);
			if (member is MethodInfo method) signature = new MethodSignature(method);
			return signature is not null;
		}
		record FieldSignature(string Name, Type FieldType) : MemberSignature(Name) {
			public FieldSignature(FieldInfo field) : this(field.Name, field.FieldType) { }
		}
		record PropertySignature(string Name, Type FieldType, bool HasGet, bool HasSet) : MemberSignature(Name) {
			public PropertySignature(PropertyInfo property) : this(property.Name, property.PropertyType, property.GetGetMethod() is not null, property.GetSetMethod() is not null) { }
		}
		record EventSignature(string Name, Type FieldType) : MemberSignature(Name) {
			public EventSignature(EventInfo @event) : this(@event.Name, @event.EventHandlerType) { }
		}
		record MethodSignature(string Name, Type ReturnType, ParameterList Parameters, string Text) : MemberSignature(Name) {
			public MethodSignature(MethodInfo method) : this(method.Name, method.ReturnType, new(method.GetParameters()), method.ToString()) { }
		}
		public class ParameterList(ParameterInfo[] parameters) : List<ParameterInfo>(parameters) {
			public override bool Equals(object obj) {
				if (obj is not ParameterList other) return false;
				for (int i = 0; i < Count; i++) {
					if (this[i].ParameterType != other[i].ParameterType) return false;
				}
				return true;
			}
			public override int GetHashCode() {
				HashCode hash = new();
				for (int i = 0; i < Count; i++) {
					hash.Add(this[i].ParameterType.GetHashCode());
				}
				return hash.ToHashCode();
			}
		}
	}
}
