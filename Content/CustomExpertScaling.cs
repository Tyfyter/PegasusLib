using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Content {
	[ReinitializeDuringResizeArrays]
	public class CustomExpertScaling : ILoadable {
		static Action<NPC>[] ScalingFunctions { get; } = NPCID.Sets.Factory.CreateCustomSet<Action<NPC>>(null);
		public static void Add(ModNPC npc, Action<NPC> scalingFunction) => Add(npc.Mod, npc.Type, scalingFunction);
		public static void Add(Mod mod, int npcType, Action<NPC> scalingFunction) {
			PegasusLib.Require(mod, LibFeature.CustomExpertScaling);
			ScalingFunctions[npcType] += scalingFunction;
		}
		public void Load(Mod mod) {
			try {
				On_NPC.ScaleStats_ApplyExpertTweaks += (orig, self) => {
					orig(self);
					ScalingFunctions.GetIfInRange(self.type)?.Invoke(self);
				};
			} catch (Exception e) {
				PegasusLib.FeatureError(LibFeature.CustomExpertScaling, e);
			}
		}
		public void Unload() { }
	}
}
