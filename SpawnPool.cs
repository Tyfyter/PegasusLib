using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PegasusLib {
	public abstract class SpawnPool : ModType {
		public float Priority = SpawnPoolPriority.BiomeLow;
		public abstract bool IsActive(NPCSpawnInfo spawnInfo);
		public List<(int npcType, SpawnRate condition)> Spawns { get; private set; } = [];
		public void AddSpawn(int npcType, SpawnRate condition) => Spawns.Add((npcType, condition));
		public void AddSpawn(int npcType, Func<NPCSpawnInfo, float> rate, LocalizedText description = null) => AddSpawn(npcType, new SpawnRate(rate, description));
		public void AddSpawn(int npcType, float rate) => AddSpawn(npcType, _ => rate);
		public void AddSpawn<TNPC>(Func<NPCSpawnInfo, float> rate, LocalizedText description = null) where TNPC : ModNPC => AddSpawn(ModContent.NPCType<TNPC>(), new SpawnRate(rate, description));
		public void AddSpawn<TNPC>(float rate) where TNPC : ModNPC => AddSpawn(ModContent.NPCType<TNPC>(), _ => rate);
		public sealed override void SetupContent() {
			SetStaticDefaults();
		}
		protected sealed override void Register() {
			ModTypeLookup<SpawnPool>.Register(this);
			SpawnPoolLoader.SpawnPools.Add(this);
		}
		public static class SpawnPoolPriority {
			public const float BiomeLow = 1;
			public const float BiomeHigh = 2;
			public const float Environment = 3;
			public const float Event = 4;
			public const float EventHigh = 5;
		}
	}
	public record class SpawnRate(Func<NPCSpawnInfo, float> Rate, LocalizedText Description = null);
	public class SpawnPoolLoader : GlobalNPC {
		public static List<SpawnPool> SpawnPools { get; private set; } = [];
		public override void Unload() {
			SpawnPools = null;
		}
		static bool InvasionActive(NPCSpawnInfo spawnInfo) {
			const float dist = 3000;
			if (Main.invasionType != 0 && Main.invasionDelay == 0 && Main.invasionSize > 0 && (spawnInfo.Player.position.Y < Main.worldSurface * 16 + spawnInfo.SpawnTileY || Main.remixWorld)) {
				if (spawnInfo.Player.position.X > Main.invasionX * 16 - dist && spawnInfo.Player.position.X < Main.invasionX * 16 + dist) return true;
				if (Main.invasionX >= (double)(Main.maxTilesX / 2 - 5) && Main.invasionX <= (double)(Main.maxTilesX / 2 + 5)) {
					for (int l = 0; l < 200; l++) {
						if (Main.npc[l].townNPC && Math.Abs(spawnInfo.Player.position.X - Main.npc[l].Center.X) < dist) {
							return Main.rand.NextBool(3);
						}
					}
				}
			}
			return false;
		}
		public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) {
			SpawnPool selectedPool = null;
			float priority = 0;
			if (InvasionActive(spawnInfo)) priority = SpawnPool.SpawnPoolPriority.Event;
			if (spawnInfo.Player.ZoneTowerNebula || spawnInfo.Player.ZoneTowerSolar || spawnInfo.Player.ZoneTowerStardust || spawnInfo.Player.ZoneTowerVortex) priority = SpawnPool.SpawnPoolPriority.EventHigh;
			for (int i = 0; i < SpawnPools.Count; i++) {
				SpawnPool currentPool = SpawnPools[i];
				if (currentPool.Priority > priority && currentPool.IsActive(spawnInfo)) {
					priority = currentPool.Priority;
					selectedPool = currentPool;
				}
			}
			if (selectedPool is not null) {
				foreach (int type in pool.Keys) pool[type] = 0;
				for (int i = 0; i < selectedPool.Spawns.Count; i++) {
					(int npcType, SpawnRate condition) = selectedPool.Spawns[i];
					pool[npcType] = condition.Rate(spawnInfo) * 1000;
				}
			}
		}
		public static int GetSpawn<TPool>(NPCSpawnInfo spawnInfo) where TPool : SpawnPool {
			WeightedRandom<int> pool = new(Main.rand);
			SpawnPool selectedPool = ModContent.GetInstance<TPool>();
			for (int i = 0; i < selectedPool.Spawns.Count; i++) {
				(int npcType, SpawnRate condition) = selectedPool.Spawns[i];
				pool.Add(npcType, condition.Rate(spawnInfo));
			}
			if (pool.elements.Count <= 0) return -1;
			return pool.Get();
		}
	}
}
