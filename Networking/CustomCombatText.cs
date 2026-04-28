using Terraria;

namespace PegasusLib.Networking {
	public record struct CustomCombatNumber(Rectangle Location, Color Color, int Amount, bool Dramatic = false, bool Dot = false) : IAutoSyncedAction {
		readonly void ISyncedAction.Perform() {
			CombatText.NewText(Location, Color, Amount, Dramatic, Dot);
		}
	}
}
