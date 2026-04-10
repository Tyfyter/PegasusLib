using Terraria.GameContent.ItemDropRules;

namespace PegasusLib.Reflection {
	public class ItemDropping : ReflectionLoader {
		delegate ItemDropAttemptResult Del_ResolveRule(IItemDropRule rule, DropAttemptInfo info);
		[ReflectionParentType(typeof(ItemDropResolver)), ReflectionMemberName(nameof(ResolveRule))]
		[ReflectionDefaultInstance(typeof(ItemDropping), nameof(empty))]
		static Del_ResolveRule _ResolveRule { get; set; }
		static readonly ItemDropResolver empty = new(null);
		public static ItemDropAttemptResult ResolveRule(IItemDropRule rule, DropAttemptInfo info) => _ResolveRule(rule, info);
	}
}
