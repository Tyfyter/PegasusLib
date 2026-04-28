using Microsoft.CodeAnalysis;
using MonoMod.Cil;
using PegasusLib.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib.Content {
	public abstract class DamageRedirection : IModType, ILoadable {
		static readonly List<DamageRedirection> damageRedirections = [];
		internal static readonly List<DamageRedirection> orderedDamageRedirections = [];
		public Mod Mod { get; internal set; }
		public Player Player { get; internal set; }
		public virtual string Name => GetType().Name;

		/// <summary>
		/// The internal name of this, including the mod it is from.
		/// </summary>
		public string FullName => $"{Mod?.Name ?? "Terraria"}/{Name}";
		void ILoadable.Load(Mod mod) {
			Mod = mod;
			Load();
			Type = damageRedirections.Count;
			damageRedirections.Add(this);
		}
		public virtual void Load() { }
		public virtual bool IsLoadingEnabled(Mod mod) => true;
		public int Type { get; private set; }
		public int OrderedType { get; private set; }
		public abstract void ResetEffects();
		public abstract void Apply(ref double damage);
		public virtual void HookOnDamageEffects() { }
		public virtual IEnumerable<DamageRedirection> SortAfter() => [];
		public virtual IEnumerable<DamageRedirection> SortBefore() => [];

		protected virtual DamageRedirection NewInstance() => (DamageRedirection)Activator.CreateInstance(GetType(), true)!;
		internal DamageRedirection CreateInstance() {
			DamageRedirection inst = NewInstance();
			inst.Mod = Mod;
			inst.Type = Type;
			inst.OrderedType = OrderedType;
			return inst;
		}
		internal DamageRedirection NewInstance(Player player) {
			DamageRedirection inst = CreateInstance();
			inst.Player = player;
			return inst;
		}
		public virtual void Unload() { }
		internal static void Sort() {
			orderedDamageRedirections.AddRange(new TopoSort<DamageRedirection>(damageRedirections,
				dr => dr.SortAfter(),
				dr => dr.SortBefore()
			).Sort());
			for (int i = 0; i < orderedDamageRedirections.Count; i++) {
				orderedDamageRedirections[i].OrderedType = i;
				orderedDamageRedirections[i].HookOnDamageEffects();
			}
		}
	}
	[ReinitializeDuringResizeArrays]
	public static class DamageRedirectionLoader {
		static DamageRedirectionLoader() {
			if (!PegasusLib.ContentLoadingFinished) {
				IL_Player.Hurt_HurtInfo_bool += IL_Player_Hurt_HurtInfo_bool;
				return;
			}
			DamageRedirection.Sort();
		}

		[ThreadStatic]
		static double currentDamage;
		public static double Apply(Player player, double damage) => player.GetModPlayer<DamageRedirectionPlayer>().Apply(damage);
		static void IL_Player_Hurt_HurtInfo_bool(ILContext il) {
			ILCursor c = new(il);
			int damage = -1;
			c.GotoNext(MoveType.After,
				static i => i.MatchLdarga(1),
				static i => i.MatchCall<Player.HurtInfo>("get_" + nameof(Player.HurtInfo.Damage)),
				static i => i.MatchConvR8(),
				i => i.MatchStloc(out damage)
			);
			c.GotoNext(MoveType.After,
				i => i.MatchCall<CombatText>(nameof(CombatText.NewText)),
				i => i.MatchPop()
			);
			ILLabel label = c.MarkLabel();
			c.Index--;
			MonoModMethods.SkipPrevArgument(c);
			c.EmitLdarg0();
			c.EmitLdloc(damage);
			c.EmitCall(((Delegate)CalculateRedirection).Method);
			c.EmitBrfalse(label);

			c.GotoNext(MoveType.After,
				i => i.MatchLdloc(out _),
				i => i.MatchLdloc(damage)
			);
			c.EmitCall(((Delegate)ApplyRedirection).Method);
			c.GotoNext(MoveType.After,
				static i => i.MatchLdfld<Player>(nameof(Player.statLife)),
				i => i.MatchLdloc(damage)
			);
			c.EmitCall(((Delegate)ApplyRedirection).Method);
		}
		static bool CalculateRedirection(Player player, double damage) {
			currentDamage = Apply(player, damage);
			return currentDamage > 0;
		}
		static double ApplyRedirection(double damage) => currentDamage;
		public static T GetRedirection<T>(this Player player) where T : DamageRedirection => player.GetRedirection(ModContent.GetInstance<T>());
		public static T GetRedirection<T>(this Player player, T instance) where T : DamageRedirection => (T)player.GetModPlayer<DamageRedirectionPlayer>().redirections[instance.OrderedType];
	}
	class DamageRedirectionPlayer : ModPlayer {
		internal readonly List<DamageRedirection> redirections = [];
		public override ModPlayer NewInstance(Player entity) {
			DamageRedirectionPlayer inst = (DamageRedirectionPlayer)base.NewInstance(entity);
			Reset(entity, inst);
			return inst;
		}
		private static void Reset(Player entity, DamageRedirectionPlayer inst) {
			inst.redirections.Clear();
			inst.redirections.AddRange(DamageRedirection.orderedDamageRedirections.Select(dr => dr.NewInstance(entity)));
		}
		public override void SetStaticDefaults() {
			redirections.Clear();
			redirections.AddRange(DamageRedirection.orderedDamageRedirections.Select(dr => dr.CreateInstance()));
		}
		public override void ResetEffects() {
			for (int i = 0; i < redirections.Count; i++) {
				if (redirections[i].OrderedType != i) {
					break;
				}
			}
			for (int i = 0; i < redirections.Count; i++) redirections[i].ResetEffects();
		}
		public double Apply(double damage) {
			for (int i = 0; i < redirections.Count; i++) redirections[i].Apply(ref damage);
			return damage;
		}
	}
	public abstract class PercentageDamageRedirection : DamageRedirection {
		public float Strength {
			get => field;
			set => field = float.Clamp(value, 0, 1);
		}
		public float DamageMultiplier {
			get => 1 - Strength;
			set => Strength = 1 - value;
		}
		public virtual float CurrentStrength => Strength;
		public override void ResetEffects() => Strength = 0;
	}
	public abstract class DamageRedirectionToMana : PercentageDamageRedirection {
		public abstract float CostMultiplier { get; }
		public override void HookOnDamageEffects() => ModContent.GetInstance<ModifyMagicCuffsEffect>().Modify += Apply;
		public override void Apply(ref double damage) {
			float strength = float.Clamp(CurrentStrength, 0, 1);
			if (strength > 0) {
				float costMult = CostMultiplier;
				double manaDamage = Math.Min(damage * costMult * strength, Player.statMana);
				damage -= manaDamage / costMult;
				if (manaDamage >= 1 && !OnDamageEffectModifier.IsModifyingEffect) DamageMana((int)Math.Floor(manaDamage));
			}
		}
		public virtual void DamageMana(int manaDamage) {
			Player.CheckMana(manaDamage, true, true);
			Max(ref Player.manaRegenDelay, (int)Player.maxRegenDelay);
			CombatText.NewText(Player.Hitbox, new Color(160, 71, 202), manaDamage);
		}
	}
	public abstract class OnDamageEffectModifier : ILoadable {
		[ThreadStatic]
		static bool isModifyingEffect;
		public static bool IsModifyingEffect => isModifyingEffect;
		internal List<(DamageRedirection kind, DamageModifier function)> modifiers = [];
		public event DamageModifier Modify {
			add => modifiers.Add(((DamageRedirection)value.Target, value));
			remove => modifiers.Remove(modifiers.Find(m => m.function.Method == value.Method));
		}
		public delegate void DamageModifier(ref double damage);
		public int ModifyUsedDamage(int damage, Player player) {
			using ScopedOverride<bool> _ = isModifyingEffect.ScopedOverride(true);
			double _damage = damage;
			List<DamageRedirection> redirections = player.GetModPlayer<DamageRedirectionPlayer>().redirections;
			for (int i = 0; i < modifiers.Count; i++) {
				(DamageRedirection kind, DamageModifier function) = modifiers[i];
				Reflection.DelegateMethods._target.SetValue(function, redirections[kind.OrderedType]);
				function(ref _damage);
			}
			return (int)_damage;
		}
		public abstract void Load(Mod mod);
		void ILoadable.Unload() {}
	}
	public class ModifyMagicCuffsEffect : OnDamageEffectModifier {
		public override void Load(Mod mod) {
			IL_Player.OnHurt_Part2 += _IL_Player_OnHurt_Part2;
		}
		void _IL_Player_OnHurt_Part2(ILContext il) {
			ILCursor c = new(il);
			c.GotoNext(i => i.MatchStfld<Player>(nameof(Player.statMana)));
			c.GotoPrev(MoveType.After, i => i.MatchCall<Player.HurtInfo>("get_" + nameof(Player.HurtInfo.SourceDamage)));
			c.EmitLdarg0();
			c.EmitDelegate(ModifyUsedDamage);
		}
	}
}
