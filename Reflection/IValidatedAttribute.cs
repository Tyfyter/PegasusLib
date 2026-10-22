using System;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Core;

namespace PegasusLib.Reflection; 
public interface IValidatedAttribute {
	public bool IsValidOn(MemberInfo member);
}
public static class AttributeValidator {
	public static void ValidateAttributes(this MemberInfo member) {
		LoaderUtils.ForEachAndAggregateExceptions(member.GetCustomAttributes().OfType<IValidatedAttribute>(), attr => {
			if (!attr.IsValidOn(member)) throw new Exception($"{attr} cannot be applied to {member}");
		});
	}
}