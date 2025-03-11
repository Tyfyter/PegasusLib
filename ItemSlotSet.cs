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
		public ItemSlotSet(int headSlot = -2, int bodySlot = -2, int legSlot = -2, int beardSlot = -2, int backSlot = -2, int faceSlot = -2, int neckSlot = -2, int shieldSlot = -2, int wingSlot = -2, int waistSlot = -2, int shoeSlot = -2, int frontSlot = -2, int handOffSlot = -2, int handOnSlot = -2, int balloonSlot = -2) {
			this.headSlot = headSlot;
			this.bodySlot = bodySlot;
			this.legSlot = legSlot;
			this.beardSlot = beardSlot;
			this.backSlot = backSlot;
			this.faceSlot = faceSlot;
			this.neckSlot = neckSlot;
			this.shieldSlot = shieldSlot;
			this.wingSlot = wingSlot;
			this.waistSlot = waistSlot;
			this.shoeSlot = shoeSlot;
			this.frontSlot = frontSlot;
			this.handOffSlot = handOffSlot;
			this.handOnSlot = handOnSlot;
			this.balloonSlot = balloonSlot;
		}
		static void ApplySlot(ref int target, int value) {
			if (value != -2) target = value;
		}
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
			ApplySlot(ref item.headSlot, headSlot);
			ApplySlot(ref item.bodySlot, bodySlot);
			ApplySlot(ref item.legSlot, legSlot);
			ApplySlot(ref item.beardSlot, beardSlot);
			ApplySlot(ref item.backSlot, backSlot);
			ApplySlot(ref item.faceSlot, faceSlot);
			ApplySlot(ref item.neckSlot, neckSlot);
			ApplySlot(ref item.shieldSlot, shieldSlot);
			ApplySlot(ref item.wingSlot, wingSlot);
			ApplySlot(ref item.waistSlot, waistSlot);
			ApplySlot(ref item.shoeSlot, shoeSlot);
			ApplySlot(ref item.frontSlot, frontSlot);
			ApplySlot(ref item.handOffSlot, handOffSlot);
			ApplySlot(ref item.handOnSlot, handOnSlot);
			ApplySlot(ref item.balloonSlot, balloonSlot);
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
			ApplySlot(ref player.head, headSlot);
			ApplySlot(ref player.body, bodySlot);
			ApplySlot(ref player.legs, legSlot);
			ApplySlot(ref player.beard, beardSlot);
			ApplySlot(ref player.back, backSlot);
			ApplySlot(ref player.face, faceSlot);
			ApplySlot(ref player.neck, neckSlot);
			ApplySlot(ref player.shield, shieldSlot);
			ApplySlot(ref player.wings, wingSlot);
			ApplySlot(ref player.waist, waistSlot);
			ApplySlot(ref player.shoe, shoeSlot);
			ApplySlot(ref player.front, frontSlot);
			ApplySlot(ref player.handoff, handOffSlot);
			ApplySlot(ref player.handon, handOnSlot);
			ApplySlot(ref player.balloon, balloonSlot);
		}
	}
}
