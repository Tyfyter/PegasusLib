using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib {
	interface IComplexMineDamageTile {
		void MinePower(int i, int j, int minePower, ref int damage) { }
	}
	public class ComplexMiningDamage : ILoadable {
		public void Load(Mod mod) {
			On_Player.GetPickaxeDamage += On_Player_GetPickaxeDamage;
			IL_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += (il) => {
				ILCursor c = new(il);
				int modTile = -1;
				int damageArg = -1;
				c.GotoNext(
					i => i.MatchCall(typeof(TileLoader), "GetTile"),
					i => i.MatchStloc(out modTile)
				);
				//IL_0151: ldarg.0
				//IL_0152: ldfld class Terraria.HitTile Terraria.Player::hitTile
				//IL_0157: ldloc.0
				//IL_0158: ldloc.1
				//IL_0159: ldc.i4.1
				//IL_015a: callvirt instance int32 Terraria.HitTile::AddDamage(int32, int32, bool)
				c.GotoNext(MoveType.Before,
					i => i.MatchLdarg(0),
					i => i.MatchLdfld<Player>("hitTile"),
					i => i.MatchLdloc(out _),
					i => i.MatchLdloc(out damageArg),
					i => i.MatchLdcI4(1),
					i => i.MatchCallvirt<HitTile>("AddDamage")
				);
				c.EmitLdloc(modTile);
				c.EmitLdarg3();
				c.EmitLdarg(4);
				c.EmitLdarg1();
				c.EmitLdfld(typeof(Item).GetField("hammer"));
				c.EmitLdloca(damageArg);
				c.EmitDelegate<MinePowerDel>((ModTile modTile, int x, int y, int minePower, ref int damage) => {
					if (modTile is IComplexMineDamageTile damageTile) {
						damageTile.MinePower(x, y, minePower, ref damage);
					}
				});
			};
		}

		public void Unload() { }
		delegate void MinePowerDel(ModTile modTile, int i, int j, int minePower, ref int damage);
		private int On_Player_GetPickaxeDamage(On_Player.orig_GetPickaxeDamage orig, Player self, int x, int y, int pickPower, int hitBufferIndex, Tile tileTarget) {
			int value = orig(self, x, y, pickPower, hitBufferIndex, tileTarget);
			ModTile modTile = ModContent.GetModTile(tileTarget.TileType);
			if (modTile is IComplexMineDamageTile damageTile) {
				damageTile.MinePower(x, y, pickPower, ref value);
			}
			return value;
		}
	}
}
