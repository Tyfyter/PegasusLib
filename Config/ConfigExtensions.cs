using System;
using System.Globalization;
using System.Reflection;

namespace PegasusLib.Config {
	public static class ConfigExtensions {
		public static PropertyInfo WithCanWrite(this PropertyInfo info, bool canWrite) => (canWrite || !info.CanWrite) ? info : new ReadOnlyPropertyInfo(info);
	}
	public class ReadOnlyPropertyInfo(PropertyInfo realInfo) : PropertyInfo {
		public override bool CanWrite => false;
		public override PropertyAttributes Attributes => realInfo.Attributes;
		public override bool CanRead => realInfo.CanRead;
		public override Type PropertyType => realInfo.PropertyType;
		public override Type DeclaringType => realInfo.DeclaringType;
		public override string Name => realInfo.Name;
		public override Type ReflectedType => realInfo.ReflectedType;
		public override MethodInfo[] GetAccessors(bool nonPublic) {
			return realInfo.GetAccessors(nonPublic);
		}
		public override object[] GetCustomAttributes(bool inherit) {
			return realInfo.GetCustomAttributes(inherit);
		}
		public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
			return realInfo.GetCustomAttributes(attributeType, inherit);
		}
		public override MethodInfo GetGetMethod(bool nonPublic) {
			return realInfo.GetGetMethod(nonPublic);
		}
		public override ParameterInfo[] GetIndexParameters() {
			return realInfo.GetIndexParameters();
		}
		public override MethodInfo GetSetMethod(bool nonPublic) {
			return realInfo.GetSetMethod(nonPublic);
		}
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			return realInfo.GetValue(obj, invokeAttr, binder, index, culture);
		}
		public override bool IsDefined(Type attributeType, bool inherit) {
			return realInfo.IsDefined(attributeType, inherit);
		}
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) {
			realInfo.SetValue(obj, value, invokeAttr, binder, index, culture);
		}
	}
}
