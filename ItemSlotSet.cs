using Terraria;

namespace PegasusLib {
	public struct ItemSlotSet {
		public int headSlot;
		public int bodySlot;
		public int legSlot;
		public int beardSlot;
		public int backSlot;
		public int faceSlot;
		public int neckSlot;
		public int shieldSlot;
		public int wingSlot;
		public int waistSlot;
		public int shoeSlot;
		public int frontSlot;
		public int handOffSlot;
		public int handOnSlot;
		public int balloonSlot;
		public ItemSlotSet(Item item) {
			headSlot = item.headSlot;
			bodySlot = item.bodySlot;
			legSlot = item.legSlot;
			beardSlot = item.beardSlot;
			backSlot = item.backSlot;
			faceSlot = item.faceSlot;
			neckSlot = item.neckSlot;
			shieldSlot = item.shieldSlot;
			wingSlot = item.wingSlot;
			waistSlot = item.waistSlot;
			shoeSlot = item.shoeSlot;
			frontSlot = item.frontSlot;
			handOffSlot = item.handOffSlot;
			handOnSlot = item.handOnSlot;
			balloonSlot = item.balloonSlot;
		}
		public readonly void Apply(Item item) {
			item.headSlot = headSlot;
			item.bodySlot = bodySlot;
			item.legSlot = legSlot;
			item.beardSlot = beardSlot;
			item.backSlot = backSlot;
			item.faceSlot = faceSlot;
			item.neckSlot = neckSlot;
			item.shieldSlot = shieldSlot;
			item.wingSlot = wingSlot;
			item.waistSlot = waistSlot;
			item.shoeSlot = shoeSlot;
			item.frontSlot = frontSlot;
			item.handOffSlot = handOffSlot;
			item.handOnSlot = handOnSlot;
			item.balloonSlot = balloonSlot;
		}
		public ItemSlotSet(Player player) {
			headSlot = player.head;
			bodySlot = player.body;
			legSlot = player.legs;
			beardSlot = player.beard;
			backSlot = player.back;
			faceSlot = player.face;
			neckSlot = player.neck;
			shieldSlot = player.shield;
			wingSlot = player.wings;
			waistSlot = player.waist;
			shoeSlot = player.shoe;
			frontSlot = player.front;
			handOffSlot = player.handoff;
			handOnSlot = player.handon;
			balloonSlot = player.balloon;
		}
		public readonly void Apply(Player player) {
			player.head = headSlot;
			player.body = bodySlot;
			player.legs = legSlot;
			player.beard = beardSlot;
			player.back = backSlot;
			player.face = faceSlot;
			player.neck = neckSlot;
			player.shield = shieldSlot;
			player.wings = wingSlot;
			player.waist = waistSlot;
			player.shoe = shoeSlot;
			player.front = frontSlot;
			player.handoff = handOffSlot;
			player.handon = handOnSlot;
			player.balloon = balloonSlot;
		}
	}
}
