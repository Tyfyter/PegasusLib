#nullable enable
using MonoMod.Cil;
using MonoMod.Utils;
using PegasusLib.Content.DropRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Content;
public class DropRuleDefinition(IDropRuleKind kind) {
	public DropChainDefinition[]? chainedRules;
	public bool Inherited { get; internal set; }
	public IDropRuleKind Kind {
		get;
		set {
			field = value;
			Inherited = false;
		}
	} = kind;
	/// <summary>
	/// Will be replaced with a copy after importing and during exporting, to ensure changes to the <see cref="DropRuleDefinition"/> are not unexpectedly or inconsistently reflected in rules
	/// </summary>
	public int[]? ItemIDs {
		get; set {
			if (value is null) {
				field = value;
				return;
			}
			if (ChildRules is not null) throw new InvalidOperationException($"{nameof(DropRuleDefinition)} may only use {nameof(ItemIDs)} or {nameof(ChildRules)}, not both");
			ValidateLength(value.Length);
			field = value;
		}
	}
	public DropRuleDefinition[]? ChildRules {
		get; set {
			if (value is null) {
				field = value;
				return;
			}
			if (ItemIDs is not null) throw new InvalidOperationException($"{nameof(DropRuleDefinition)} may only use {nameof(ItemIDs)} or {nameof(ChildRules)}, not both");
			ValidateLength(value.Length);
			field = value;
		}
	}
	void ValidateLength(int length) {
		switch (Kind) {
			case IDropExactOptionsCoundKind { OptionsCount: int optionsCount }:
			if (length != optionsCount) throw new InvalidOperationException($"{Kind} does not support multiple options");
			break;
			case IDropOptionsKind:
			break;
			default:
			if (length != 1) throw new InvalidOperationException($"{Kind} does not support multiple options");
			break;
		}
	}
	public static DropRuleDefinition? Import(IItemDropRule dropRule) => DropRuleKindLoader.Import(dropRule);
	public static DropRuleDefinition? ImportForCheck(IItemDropRule dropRule) => DropRuleKindLoader.Import(dropRule, true);
	public IItemDropRule Export(bool ignoreUnsafe = false) {
		if (!ignoreUnsafe) {
			if (Inherited) throw new InvalidOperationException($"Cannot export inherited drop rule kind unless ignoreUnsafe is set");
			for (int i = 0; i < chainedRules?.Length; i++) if (chainedRules[i].Rule is null) throw new InvalidOperationException($"Cannot export drop rule with unsupported chains unless ignoreUnsafe is set");
		}
		int[]? itemIDs = ItemIDs;
		if (ItemIDs is not null) ItemIDs = ItemIDs.ToArray();
		IItemDropRule rule;
		if (Kind is IChanceDenominatorKind chance && chance.ChanceDenominator < chance.ChanceNumerator) {
			int oldDenominator = chance.ChanceDenominator;
			chance.ChanceDenominator = chance.ChanceNumerator;
			rule = Kind.Export(this);
			chance.ChanceDenominator = oldDenominator;
		} else {
			rule = Kind.Export(this);
		}
		ItemIDs = itemIDs;

		if (ReferenceEquals(Kind, rule)) throw new InvalidOperationException($"Cannot export kind as a drop rule");
		for (int i = 0; i < chainedRules?.Length; i++) if (chainedRules[i].Rule is not null) rule.ChainedRules.Add(chainedRules[i].Export(ignoreUnsafe));
		return rule;
	}
}
public record struct DropChainDefinition(DropRuleDefinition? Rule, ItemDropAttemptResultState RequiredState) {
	public readonly IItemDropRuleChainAttempt Export(bool ignoreUnsafe) {
		switch (RequiredState) {
			case ItemDropAttemptResultState.Success:
			return new Chains.TryIfSucceeded(Rule!.Export(ignoreUnsafe));
			case ItemDropAttemptResultState.FailedRandomRoll:
			return new Chains.TryIfFailedRandomRoll(Rule!.Export(ignoreUnsafe));
			case ItemDropAttemptResultState.DoesntFillConditions:
			return new Chains.TryIfDoesntFillConditions(Rule!.Export(ignoreUnsafe));
		}
		throw new NotImplementedException($"Unsupported state {RequiredState}");
	}
	public static DropChainDefinition Import(IItemDropRuleChainAttempt chainAttempt) => Import(chainAttempt, false);
	public static DropChainDefinition Import(IItemDropRuleChainAttempt chainAttempt, bool withoutChains) {
		switch (chainAttempt) {
			case Chains.TryIfSucceeded:
			return new(DropRuleKindLoader.Import(chainAttempt.RuleToChain, withoutChains), ItemDropAttemptResultState.Success);
			case Chains.TryIfFailedRandomRoll:
			return new(DropRuleKindLoader.Import(chainAttempt.RuleToChain, withoutChains), ItemDropAttemptResultState.FailedRandomRoll);
			case Chains.TryIfDoesntFillConditions:
			return new(DropRuleKindLoader.Import(chainAttempt.RuleToChain, withoutChains), ItemDropAttemptResultState.DoesntFillConditions);
		}
		throw new NotImplementedException($"Unsupported chain type {chainAttempt.GetType()}, all 3 reasonable chain types are already implemented, what are you trying to accomplish?");
	}
}
#region interfaces
public interface IDropRuleKind {
	/// <summary>
	/// Implement <see cref="IDropRuleKind{X}"/> instead
	/// </summary>
	internal bool ImplementTheGenericVersionInstead { get; }
	internal IItemDropRule Export(DropRuleDefinition definition);
}
public interface IDropRuleKind<IRule> : IDropRuleKind, IAutoload<DropRuleKindLoader.ActualLoader<IRule>> where IRule : IItemDropRule {
	bool IDropRuleKind.ImplementTheGenericVersionInstead => false;
	IItemDropRule IDropRuleKind.Export(DropRuleDefinition definition) => Export(definition);
	/// <summary>
	/// Will always be called on <paramref name="definition"/>.kind
	/// </summary>
	/// <param name="definition">The drop rule definition to export</param>
	/// <returns>A <typeparamref name="IRule"/> created from <paramref name="definition"/></returns>
	new IRule Export(DropRuleDefinition definition);
	public abstract static DropRuleDefinition Import(IRule rule);
}
public interface IChanceDenominatorKind {
	public int ChanceDenominator { get; set; }
	/// <summary>
	/// If this isn't constant, use <see cref="IDropChanceKind"/> instead
	/// </summary>
	public int ChanceNumerator => 1;
}
public interface IDropChanceKind : IChanceDenominatorKind {
	int IChanceDenominatorKind.ChanceNumerator => ChanceNumerator;
	public new int ChanceNumerator { get; set; }
}
public interface IVariableDropChanceKind {
	public (int ChanceNumerator, int ChanceDenominator)[] Chances { get; set; }
}
public interface IDropQuantityKind {
	public int AmountDroppedMinimum { get; set; }
	public int AmountDroppedMaximum { get; set; }
}
public interface IDropSingleQuantityKind : IDropQuantityKind {
	public int Amount { get; set; }
	int IDropQuantityKind.AmountDroppedMinimum { get => Amount; set => Amount = value; }
	int IDropQuantityKind.AmountDroppedMaximum { get => Amount; set => Amount = value; }
}
public interface IDropConditionKind {
	public IItemDropRuleCondition Condition { get; set; }
}
public interface IDropOptionsKind { }
/// <summary>
/// Chain wrapper rules always import a layer of chains, even if chains would otherwise be ignored
/// </summary>
public interface IChainWrapperRuleKind { }
public interface IDropExactOptionsCoundKind {
	int OptionsCount { get; }
}
#endregion
public static class DropRuleKindLoader {
	static readonly HashSet<Type> knownUnsupportedTypes = [];
	static readonly HashSet<Type> knownInheritedTypes = [];
	static readonly Dictionary<Type, Func<IItemDropRule, DropRuleDefinition>> kinds = [];
	static Func<IItemDropRule, DropRuleDefinition>? GetImporter(Type ruleType) {
		if (ruleType is null) return null;
		if (!ruleType.IsAssignableTo(typeof(IItemDropRule))) return null;
		if (kinds.TryGetValue(ruleType, out Func<IItemDropRule, DropRuleDefinition>? importer)) return importer;
		importer = GetImporter(ruleType.BaseType!);
		if (importer is null) return null;
		if (knownInheritedTypes.Add(ruleType)) ModContent.GetInstance<PegasusLib>().Logger.Warn($"Drop rule {ruleType} using inherited drop rule kind ({ruleType.BaseType})");
		if (importer.Target is not InheritedImporterWrapper) importer = new InheritedImporterWrapper(importer).Invoke;
		kinds[ruleType] = importer;
		return importer;
	}
	struct InheritedImporterWrapper(Func<IItemDropRule, DropRuleDefinition> func) {
		public readonly DropRuleDefinition Invoke(IItemDropRule rule) {
			DropRuleDefinition drd = func(rule);
			drd.Inherited = true;
			return drd;
		}
	}
	public static DropRuleDefinition? Import(IItemDropRule dropRule) => Import(dropRule, false);
	public static DropRuleDefinition? Import(IItemDropRule dropRule, bool withoutChains) {
		DropRuleDefinition? definition = GetImporter(dropRule.GetType())?.Invoke(dropRule);
		if (definition is null) {
			if (knownUnsupportedTypes.Add(dropRule.GetType())) ModContent.GetInstance<PegasusLib>().Logger.Warn($"Unsupported drop rule type {dropRule.GetType()}");
			return null;
		}
		if (ReferenceEquals(definition.Kind, dropRule)) throw new InvalidOperationException($"Cannot use a drop rule as its own kind, make a copy of the rule or use a separate class instead");
		if (definition.ItemIDs is not null) definition.ItemIDs = definition.ItemIDs.ToArray();
		if (!withoutChains || definition is IChainWrapperRuleKind) {
			definition.chainedRules = new DropChainDefinition[dropRule.ChainedRules?.Count ?? 0];
			for (int i = 0; i < dropRule.ChainedRules?.Count; i++) definition.chainedRules[i] = DropChainDefinition.Import(dropRule.ChainedRules[i], withoutChains);
		}
		return definition;
	}
	public static DropRuleDefinition?[] Import(IItemDropRule[] dropRules) {
		DropRuleDefinition?[] definitions = new DropRuleDefinition[dropRules.Length];
		for (int i = 0; i < dropRules.Length; i++) definitions[i] = Import(dropRules[i]);
		return definitions;
	}
	public static float GetChance(this IChanceDenominatorKind drop) => drop.ChanceNumerator / (float)drop.ChanceDenominator;
	public static IItemDropRule[] Export(this DropRuleDefinition[] dropRules, bool ignoreUnsafe = false) {
		IItemDropRule[] definitions = new IItemDropRule[dropRules.Length];
		for (int i = 0; i < dropRules.Length; i++) definitions[i] = dropRules[i].Export(ignoreUnsafe);
		return definitions;
	}
	public class ActualLoader<IRule> : IAutoloader where IRule : IItemDropRule {
		static void IAutoloader.Autoload(Mod mod, Type type) {
			DynamicMethodDefinition dmd = new(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Single(m => m.ReturnType == typeof(IRule) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(DropRuleDefinition)  && m.Name.Contains("Export")));
			if (new ILCursor(new ILContext(dmd.Definition)).TryGotoNext(i => i.MatchLdarg0(), i => i.MatchRet())) {
				throw new InvalidOperationException($"Cannot export kind as a drop rule");
			}
			MethodInfo import = type.GetMethod($"PegasusLib.Content.IDropRuleKind<{typeof(IRule).FullName}>.Import", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				?? type.GetMethod("Import", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
			dmd = new(import);
			if (new ILCursor(new ILContext(dmd.Definition)).TryGotoNext(i => i.MatchLdarg0(), i => i.MatchStfld<DropRuleDefinition>(nameof(DropRuleDefinition.Kind)))) {
				throw new InvalidOperationException($"Cannot use a drop rule as its own kind, make a copy of the rule or use a separate class instead");
			}
			dmd = new("Import", typeof(DropRuleDefinition), [typeof(IItemDropRule)]);
			new ILCursor(new ILContext(dmd.Definition)).EmitLdarg0().EmitCastclass(typeof(IRule)).EmitCall(import).EmitRet();
			kinds[typeof(IRule)] = dmd.Generate().CreateDelegate<Func<IItemDropRule, DropRuleDefinition>>();
		}
	}
}
#if DEBUG
public class DRDTest : GlobalNPC {
	static readonly HashSet<IItemDropRule> modifiedRules = [];
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) {
		foreach (IItemDropRule rule in npcLoot.Get(false).FindDropRules<IItemDropRule>(static rule => !modifiedRules.Contains(rule) && (DropRuleDefinition.ImportForCheck(rule)?.ItemIDs?.Contains(ItemID.ChumBucket) ?? false))) {
			DropRuleDefinition? drd = DropRuleDefinition.Import(rule);
			if (drd?.Kind is IChanceDenominatorKind data) {
				data.ChanceDenominator -= data.ChanceNumerator * 2;
				IItemDropRule newRule = drd.Export();
				rule.OnFailedRoll(new LeadingConditionRule(new Conditions.PlayerNeedsHealing())).OnSuccess(newRule);
				modifiedRules.Add(newRule);
				modifiedRules.Add(rule);
			}
		}
	}
}
#endif