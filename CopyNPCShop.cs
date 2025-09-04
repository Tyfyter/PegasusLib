using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib {
	public class CopyNPCShop(int npcType, AbstractNPCShop shop) : AbstractNPCShop(npcType, shop.Name) {
		public override IEnumerable<Entry> ActiveEntries => shop.ActiveEntries;
		public override void FillShop(ICollection<Item> items, NPC npc) => shop.FillShop(items, npc);
		public override void FillShop(Item[] items, NPC npc, out bool overflow) => shop.FillShop(items, npc, out overflow);
	}
}
