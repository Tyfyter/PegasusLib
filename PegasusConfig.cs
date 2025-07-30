using Terraria.ModLoader.Config;

namespace PegasusLib {
	public class PegasusConfig : ModConfig {
		public static PegasusConfig Instance;
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public bool alwaysShowDyeSlotList = false;
	}
}
