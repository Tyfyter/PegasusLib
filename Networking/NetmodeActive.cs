using Terraria;
using Terraria.ID;

namespace PegasusLib.Networking {
	public static class NetmodeActive {
		public static bool SinglePlayer => Main.netMode == NetmodeID.SinglePlayer;
		public static bool MultiplayerClient => Main.netMode == NetmodeID.MultiplayerClient;
		public static bool Server => Main.netMode == NetmodeID.Server;
	}
}
