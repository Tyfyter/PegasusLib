using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace PegasusLib {
	public static class DropRuleExt {
		public static void Unload() {
			RuleChildFinders = null;
		}
		static Dictionary<Type, Func<IItemDropRule, IEnumerable<IItemDropRule>>> _RuleChildFinders => new() {
			[typeof(AlwaysAtleastOneSuccessDropRule)] = r => ((AlwaysAtleastOneSuccessDropRule)r).rules,
			[typeof(DropBasedOnExpertMode)] = r => [((DropBasedOnExpertMode)r).ruleForNormalMode, ((DropBasedOnExpertMode)r).ruleForExpertMode],
			[typeof(DropBasedOnMasterAndExpertMode)] = r => [((DropBasedOnMasterAndExpertMode)r).ruleForDefault, ((DropBasedOnMasterAndExpertMode)r).ruleForExpertmode, ((DropBasedOnMasterAndExpertMode)r).ruleForMasterMode],
			[typeof(DropBasedOnMasterMode)] = r => [((DropBasedOnMasterMode)r).ruleForDefault, ((DropBasedOnMasterMode)r).ruleForMasterMode],
			[typeof(FewFromRulesRule)] = r => ((FewFromRulesRule)r).options,
			[typeof(OneFromRulesRule)] = r => ((OneFromRulesRule)r).options,
			[typeof(SequentialRulesNotScalingWithLuckRule)] = r => ((SequentialRulesNotScalingWithLuckRule)r).rules,
			[typeof(SequentialRulesRule)] = r => ((SequentialRulesRule)r).rules,
		};
		public static Dictionary<Type, Func<IItemDropRule, IEnumerable<IItemDropRule>>> RuleChildFinders { get; private set; }  = _RuleChildFinders;
		/// <summary>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dropRules"></param>
		/// <param name="predicate"></param>
		/// <returns>The first <see cref="IItemDropRule"/> matching <paramref name="predicate"/> in <paramref name="dropRules"/>, or null if no matching rule was found</returns>
		public static T FindDropRule<T>(this IEnumerable<IItemDropRule> dropRules, Predicate<T> predicate) where T : class, IItemDropRule {
			foreach (var dropRule in dropRules) {
				if (dropRule is T rule && predicate(rule)) return rule;
				if (dropRule.ChainedRules.Count != 0 && dropRule.ChainedRules.Select(c => c.RuleToChain).FindDropRule(predicate) is T foundRule) return foundRule;
				if (RuleChildFinders.TryGetValue(dropRule.GetType(), out var ruleChildFinder) && ruleChildFinder(dropRule).FindDropRule(predicate) is T foundRule2) return foundRule2;
			}
			return null;
		}
	}
}
