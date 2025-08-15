using PegasusLib.Networking;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib {
	[Autoload(Side = ModSide.Client)]
	internal class MissingSyncWarn : ModPlayer {
		public override void OnEnterWorld() {
			if (NetmodeActive.SinglePlayer) return;
			if (!PegasusLib.IsNetSynced) {
				if (KeybindHandlerPlayer.playerIDsByName.Count > 0) {
					HashSet<string> mods = [];
					foreach (int player in KeybindHandlerPlayer.playerIDsByName.Values) {
						if (Main.LocalPlayer?.ModPlayers[player]?.Mod is not Mod mod) continue;
						mods.Add(mod.DisplayName);
					}
					Main.NewText(TextUtils.Format("Mods.PegasusLib.NotSynced_KeybindHandlerPlayer", mods));
				}
				if (WeakSyncedAction.actions.Count > 0) {
					Main.NewText(TextUtils.Format("Mods.PegasusLib.NotSynced_WeakSyncedAction", WeakSyncedAction.actions.Keys));
				}
			}
		}
	}
}
