using PegasusLib.Config;
using PegasusLib.UI;
using System.ComponentModel;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace PegasusLib {
	public class PegasusConfig : ModConfig {
		public static PegasusConfig Instance;
		public override ConfigScope Mode => ConfigScope.ClientSide;
		public bool alwaysShowDyeSlotList = false;
		[CustomModConfigItem<SlotNameOrderElement>]
		public SlotNamePlacement[] dyeSlotNamePlacement = [SlotNamePlacement.SlotName, SlotNamePlacement.ItemName, SlotNamePlacement.OtherTooltips];
		public BuffListControl buffHintListCondition = BuffListControl.Shift;
		public BuffListPosition buffHintListPosition = BuffListPosition.Side;
		[DefaultValue(1f), Range(0.5f, 1.5f)]
		public float buffHintListScale = 1f;
		[DefaultValue(true)]
		public bool showAdditionalDebuffsTooltip = true;
		[DefaultValue(true)]
		public bool colorBuffHints = true;
		[DefaultValue(false)]
		public bool invertOnHitBuffHintColor = false;
		[DefaultValue(TextInputContainerExtensions.CursorType.Line)]
		public TextInputContainerExtensions.CursorType preferredTextCursor = TextInputContainerExtensions.CursorType.Line;
	}
	public class SlotNameOrderElement : OrderConfigElement<SlotNamePlacement> {
		public override UIElement GetElement(SlotNamePlacement value) => new UIText(Language.GetTextValue("Mods.PegasusLib.Configs.SlotNamePlacement." + value.ToString()), 0.8f) {
			Width = new(0, 1),
			Top = new(0, 0.25f),
			Height = new(FontAssets.MouseText.Value.MeasureString(Language.GetTextValue("Mods.PegasusLib.Configs.SlotNamePlacement." + value.ToString())).Y * 0.8f, 0),
			VAlign = 0f,
			PaddingTop = 0,
			MarginTop = 0
		};
	}
}
