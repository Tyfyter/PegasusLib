using PegasusLib.UI;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PegasusLib {
	public class PegasusConfig : ModConfig {
		public static PegasusConfig Instance;
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public bool alwaysShowDyeSlotList = false;
		public BuffListControl buffHintListCondition = BuffListControl.Shift;
		public BuffListPosition buffHintListPosition = BuffListPosition.Side;
		[DefaultValue(1f), Range(0.5f, 1.5f)]
		public float buffHintListScale = 1f;
		[DefaultValue(true)]
		public bool showAdditionalDebuffsTooltip = true;
	}
}
