using System;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace PegasusLib.Config {
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Enum)]
	public class CustomModConfigItemAttribute<TElement>() : CustomModConfigItemAttribute(typeof(TElement)) where TElement : ConfigElement, new() { }
}
