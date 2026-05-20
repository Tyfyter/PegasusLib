using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib.Content {
	public interface IInheritedDamageClass : IAutoload<IInheritedDamageClass.RequireFeature> {
		public void ModifyInheritence(DamageClass damageClass, ref StatInheritanceData inheritance);
		class Loader : ILoadable {
			void ILoadable.Load(Mod mod) {
				try {
					IL_Player.GetTotalDamage += Modify;
					IL_Player.GetTotalCritChance += Modify;
					IL_Player.GetTotalAttackSpeed += Modify;
					IL_Player.GetTotalArmorPenetration += Modify;
					IL_Player.GetTotalKnockback += Modify;
				} catch (Exception e) {
					PegasusLib.FeatureError(LibFeature.IInheritedDamageClass, e);
				}
			}
			static void Modify(ILContext context) {
				ILCursor c = new(context);
				int index = -1;
				int inheritance = -1;
				c.GotoNext(MoveType.After,
					i => i.MatchLdloc(out index),
					i => i.MatchCallvirt<List<DamageClass>>("get_Item"),
					i => i.MatchCallvirt<DamageClass>(nameof(DamageClass.GetModifierInheritance)),
					i => i.MatchStloc(out inheritance)
				);
				c.EmitLdarg0();
				c.EmitLdarg1();
				c.EmitLdloc(index);
				c.EmitLdloca(inheritance);
				c.EmitCall(((Delegate)ApplyInheritence).Method);
			}
			void ILoadable.Unload() {}
			static void ApplyInheritence(Player player, DamageClass forClass, int _fromClass, ref StatInheritanceData inheritance) {
				DamageClass fromClass = DamageClassLoader.GetDamageClass(_fromClass);
				if (fromClass is IInheritedDamageClass inheritedClass) inheritedClass.ModifyInheritence(forClass, ref inheritance);
				GlobalDamageClass.ModifyAnyInheritence(player, forClass, fromClass, ref inheritance);
			}
		}
		public class RequireFeature : IAutoloader {
			static void IAutoloader.Autoload(Mod mod, Type type) => PegasusLib.Require(mod, LibFeature.IInheritedDamageClass);
		}
	}
	public abstract class GlobalDamageClass : ILoadable, IAutoload<IInheritedDamageClass.RequireFeature> {
		static readonly List<GlobalDamageClass> globals = [];
		void ILoadable.Load(Mod mod) => globals.Add(this);
		void ILoadable.Unload() { }
		internal static void ModifyAnyInheritence(Player player, DamageClass forClass, DamageClass fromClass, ref StatInheritanceData inheritance) {
			for (int i = 0; i < globals.Count; i++) globals[i].ModifyInheritence(player, forClass, fromClass, ref inheritance);
		}
		public virtual void ModifyInheritence(Player player, DamageClass forClass, DamageClass fromClass, ref StatInheritanceData inheritance) { }
	}
}
