using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Sets {
	[ReinitializeDuringResizeArrays]
	public static class NPCSets {
		public static Predicate<NPC>[] CustomGroundedCheck { get; } = NPCID.Sets.Factory.CreateNamedSet($"{nameof(CustomGroundedCheck)}")
		.Description("Used to prevent exploits from some on-hit effects")
		.RegisterCustomSet<Predicate<NPC>>(null,
			NPCID.WallCreeperWall, AlwaysGrounded,
			NPCID.BlackRecluseWall, AlwaysGrounded,
			NPCID.BloodCrawlerWall, AlwaysGrounded,
			NPCID.DesertScorpionWall, AlwaysGrounded,
			NPCID.JungleCreeperWall, AlwaysGrounded
		);
		public static readonly Predicate<NPC> AlwaysGrounded = npc => true;
		public static bool IsGrounded(this NPC npc) {
			if (CustomGroundedCheck[npc.type] is not null) return CustomGroundedCheck[npc.type](npc);
			return npc.collideY;
		}
	}
}
