using PegasusLib.Reflection;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.Utilities;
using static PegasusLib.FishingLootPool;

namespace PegasusLib {
	public abstract class FishingLootPool : ModType {
		public abstract bool IsActive(Player player, FishingAttempt attempt);
		public List<FishingCatch> Crate { get; } = [];
		public List<FishingCatch> Legendary { get; } = [];
		public List<FishingCatch> VeryRare { get; } = [];
		public List<FishingCatch> Rare { get; } = [];
		public List<FishingCatch> Uncommon { get; } = [];
		public List<FishingCatch> QuestFish { get; } = [];
		public List<FishingCatch> Common { get; } = [];
		public List<FishingCatch> Any { get; } = [];
		public IEnumerable<int> ReportDrops() {
			foreach (int drop in Crate.ReportDrops()) yield return drop;
			foreach (int drop in Legendary.ReportDrops()) yield return drop;
			foreach (int drop in VeryRare.ReportDrops()) yield return drop;
			foreach (int drop in Rare.ReportDrops()) yield return drop;
			foreach (int drop in QuestFish.ReportDrops()) yield return drop;
			foreach (int drop in Uncommon.ReportDrops()) yield return drop;
			foreach (int drop in Common.ReportDrops()) yield return drop;
			foreach (int drop in Any.ReportDrops()) yield return drop;
		}
		/*public IEnumerable<FishingDropInfo> ReportDropRates(Player player, FishingAttempt attempt) {
			foreach (FishingDropInfo drop in Crate.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in Legendary.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in VeryRare.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in Rare.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in QuestFish.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in Uncommon.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in Common.ReportDropRates(player, attempt)) yield return drop;
			foreach (FishingDropInfo drop in Any.ReportDropRates(player, attempt)) yield return drop;
		}*/
		public void AddCrates(int prehardmode, int hardmode) {
			Crate.Add(FishingCatch.BiomeCrate(prehardmode, false));
			Crate.Add(FishingCatch.BiomeCrate(hardmode, true));
		}
		public void AddQuestFish(int item, Func<Player, FishingAttempt, bool> canDrop = null) {
			QuestFish.Add(new NormalFishingCatch(canDrop + ((_, attempt) => attempt.questFish == item), 1) {
				ItemType = item
			});
		}
		public sealed override void SetupContent() {
			SetStaticDefaults();
		}
		protected sealed override void Register() {
			ModTypeLookup<FishingLootPool>.Register(this);
			FishingLootPoolLoader.LootPools.Add(this);
		}
		public abstract class FishingCatch(Func<Player, FishingAttempt, bool> canDrop, float weight) {
			[Obsolete("Use ReportDropRates")]
			public virtual IEnumerable<int> ReportDrops() => ReportDropRates(default).TrySelect((FishingDropInfo info, out int item) => (item = info.Item) >= 0);
			public virtual IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain) => [];
			public virtual IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain, Player player, FishingAttempt attempt) => CanDrop(player, attempt) ? ReportDropRates(chain) : [];
			public abstract void GetCatch(Player player, FishingAttempt attempt, ref int itemType, ref int npcType);
			public Func<Player, FishingAttempt, bool> CanDrop { get; init; } = canDrop;
			public float Weight { get; init; } = weight;
			public static NormalFishingCatch BiomeCrate(int item, bool hardmode) {
				return Item(item, (player, attempt) => {
					return (Main.hardMode == hardmode) && ShouldDropBiomeCrate(player, attempt);
				});
			}
			[Obsolete("Use AddQuestFish", true)]
			public static NormalFishingCatch QuestFish(int item) => new((_, attempt) => attempt.questFish == item, 1) {
				ItemType = item
			};
			public static NormalFishingCatch Item(int item, Func<Player, FishingAttempt, bool> canDrop = null, float weight = 1) => new(canDrop ?? ((_, _) => true), weight) {
				ItemType = item
			};
			public static NormalFishingCatch NPC(int npc, Func<Player, FishingAttempt, bool> canDrop = null, float weight = 1) => new(canDrop ?? ((_, _) => true), weight) {
				NPCType = npc
			};
			protected static void FallThrough() => FishingLootPoolLoader.fallThrough = true;
		}
		/// <summary>
		/// Used to denote that a rarity pool should have a chance to fail, even if it contains a valid item, such as catches with <see cref="FishingAttempt.legendary"/> not always catching legendary fish
		/// </summary>
		public class FallthroughFishingCatch(Func<Player, FishingAttempt, bool> canDrop = null, float weight = 1) : FishingCatch(canDrop ?? ((_, _) => true), weight) {
			public override void GetCatch(Player player, FishingAttempt attempt, ref int itemType, ref int npcType) => FallThrough();
			public override IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain) => [];
		}
		public class NormalFishingCatch : FishingCatch {
			public int ItemType { get; init; } = -1;
			public int NPCType { get; init; } = -1;
			public override IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain) => [new(chain.Chance, ItemType, NPCType)];
			public override void GetCatch(Player player, FishingAttempt attempt, ref int itemType, ref int npcType) {
				itemType = ItemType;
				npcType = NPCType;
			}
			internal NormalFishingCatch(Func<Player, FishingAttempt, bool> canDrop, float weight) : base(canDrop, weight) { }
		}
		public static bool ShouldDropBiomeCrate(Player player, FishingAttempt attempt) {
			if (!attempt.rare || attempt.veryrare || attempt.legendary) return false;
			if (player.ZoneDungeon) return false;
			if (player.ZoneBeach || (Main.remixWorld && attempt.heightLevel == 1 && attempt.Y >= Main.rockLayer && FishingLootPoolLoader.skipBiomeCratesIfRemixUGOcean)) return false;
			return true;
		}
		public class SequentialCatches(Func<Player, FishingAttempt, bool> canDrop = null, float weight = 1, params FishingCatch[] catches) : FishingCatch(canDrop ?? ((_, _) => true), weight) {
			public SequentialCatches(params FishingCatch[] catches) : this(null, catches: catches) { }
			public override IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain) {
				float totalWeight = 0;
				for (int i = 0; i < catches.Length; i++) totalWeight += catches[i].Weight;
				totalWeight = 1 / totalWeight;
				for (int i = 0; i < catches.Length; i++) {
					foreach (FishingDropInfo drop in catches[i].ReportDropRates(chain * totalWeight)) {
						yield return drop;
					}
				}
			}
			public override IEnumerable<FishingDropInfo> ReportDropRates(FishingDropChain chain, Player player, FishingAttempt attempt) {
				float totalWeight = 0;
				for (int i = 0; i < catches.Length; i++) {
					if (catches[i].CanDrop(player, attempt)) totalWeight += catches[i].Weight;
				}
				totalWeight = 1 / totalWeight;
				for (int i = 0; i < catches.Length; i++) {
					if (!catches[i].CanDrop(player, attempt)) continue;
					foreach (FishingDropInfo drop in catches[i].ReportDropRates(chain * totalWeight)) {
						yield return drop;
					}
				}
			}
			public override void GetCatch(Player player, FishingAttempt attempt, ref int itemType, ref int npcType) {
				int oldItemDrop = itemType;
				int oldNPCSpawn = npcType;
				foreach (FishingCatch @catch in catches) {
					if (!@catch.CanDrop(player, attempt)) continue;
					@catch.GetCatch(player, attempt, ref itemType, ref npcType);
					if (itemType != oldItemDrop || npcType != oldNPCSpawn) return;
				}
			}
		}
	}
	public class FishingLootPoolLoader : ModPlayer {
		public static List<FishingLootPool> LootPools { get; private set; } = [];
		internal static bool fallThrough = false;
		public override void Unload() {
			LootPools = null;
		}
		internal static bool skipBiomeCratesIfRemixUGOcean;
		public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition) {
			skipBiomeCratesIfRemixUGOcean = Main.rand.NextBool(2);
			int oldItemDrop = itemDrop;
			int oldNPCSpawn = npcSpawn;
			WeightedRandom<FishingCatch>[] drops = new WeightedRandom<FishingCatch>[8];
			for (int i = 0; i < drops.Length; i++) drops[i] = new();
			for (int i = 0; i < LootPools.Count; i++) {
				FishingLootPool pool = LootPools[i];
				if (!pool.IsActive(Player, attempt)) continue;

				if (attempt.crate) AddPool(0, pool.Crate);
				if (attempt.legendary) AddPool(1, pool.Legendary);
				if (attempt.veryrare) AddPool(2, pool.VeryRare);
				if (attempt.rare) AddPool(3, pool.Rare);
				if (attempt.uncommon) {
					AddPool(4, pool.QuestFish);
					AddPool(5, pool.Uncommon);
				}
				if (attempt.common) AddPool(6, pool.Common);
				AddPool(7, pool.Any);

				void AddPool(int poolNum, List<FishingCatch> newPool) {
					for (int i = 0; i < newPool.Count; i++) {
						if (newPool[i].CanDrop(Player, attempt)) drops[poolNum].Add(newPool[i], newPool[i].Weight);
					}
				}
			}
			bool DoPool(int poolNum, ref int itemDrop, ref int npcSpawn) {
				fallThrough = false;
				while (drops[poolNum].TryPop(out FishingCatch @catch)) {
					fallThrough = false;
					@catch.GetCatch(Player, attempt, ref itemDrop, ref npcSpawn);
					if (itemDrop != oldItemDrop || npcSpawn != oldNPCSpawn) return true;
					if (fallThrough) return false;
				}
				return false;
			}
			if (attempt.crate) {
				DoPool(0, ref itemDrop, ref npcSpawn);
				return;
			}
			for (int i = 1; i < drops.Length; i++) {
				if (DoPool(i, ref itemDrop, ref npcSpawn)) return;
			}
		}
		public static bool ResolveSequentialDrops(IEnumerable<FishingCatch> catches, Player player, FishingAttempt attempt, ref int itemDrop, ref int npcSpawn) {
			int oldItemDrop = itemDrop;
			int oldNPCSpawn = npcSpawn;
			foreach (FishingCatch @catch in catches) {
				if (@catch.CanDrop(player, attempt)) continue;
				@catch.GetCatch(player, attempt, ref itemDrop, ref npcSpawn);
				if (itemDrop != oldItemDrop || npcSpawn != oldNPCSpawn) return true;
			}
			return false;
		}
	}
	public record struct FishingDropInfo(float Chance, int Item = -1, int NPC = -1) {
		public override readonly string ToString() => $"{(Item >= 0 ? Lang.GetItemNameValue(Item) : Lang.GetNPCNameValue(NPC))} ({Chance:0P})";
	}
	public record struct FishingDropChain(float Chance) {
		public static FishingDropChain operator *(FishingDropChain chain, float scalar) => new(chain.Chance * scalar);
	}
}
