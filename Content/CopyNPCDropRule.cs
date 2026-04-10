using PegasusLib.Reflection;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Content;
public class CopyNPCDropRule(int type) : IItemDropRule {
	static readonly RecursionCheckedSet<int> recursionBlocker = new();
	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => true;
	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo) {
		using IDisposable recursionBlock = recursionBlocker.TryAdd(type);
		if (recursionBlock is null) return;
		foreach (IItemDropRule rule in Main.ItemDropsDB.GetRulesForNPCID(type, false)) rule.ReportDroprates(drops, ratesInfo);
	}

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info) {
		using IDisposable recursionBlock = recursionBlocker.TryAdd(type);
		if (recursionBlock is null) return new ItemDropAttemptResult() {
			State = ItemDropAttemptResultState.DidNotRunCode
		};
		foreach (IItemDropRule rule in Main.ItemDropsDB.GetRulesForNPCID(type, false)) ItemDropping.ResolveRule(rule, info);
		return new ItemDropAttemptResult() {
			State = ItemDropAttemptResultState.Success
		};
	}
}