using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PegasusLib.Networking {
	/// <summary>
	/// For use by Client-Side and NoSync mods which should be able to optionally communicate with other clients
	/// </summary>
	public abstract record class WeakSyncedAction : ILoadable {
		internal static readonly Dictionary<string, WeakSyncedAction> actions = [];
		public static bool TryGet(string name, out WeakSyncedAction action) => actions.TryGetValue(name, out action);
		protected virtual bool ShouldPerform => true;
		public string Name => $"{GetType().Namespace.Split('.')[0]}/{GetType().Name}";
		public void Load(Mod mod) {
			if (mod.Side == ModSide.Both) PegasusLib.LogLoadingWarning(Language.GetText(""));
			mod.Logger.Info($"{nameof(WeakSyncedAction)} Loading {Name}");
			actions.Add(Name, this);
		}
		public void Unload() { }
		public WeakSyncedAction Read(BinaryReader reader) {
			return NetReceive(reader);
		}
		/// <summary>
		/// Performs the action, then sends it if appropriate
		/// </summary>
		/// <param name="fromClient"></param>
		public void Perform(int fromClient = -2) {
			if (!ShouldPerform) return;
			Perform();
			if (NetmodeActive.MultiplayerClient && fromClient == -2) {
				Send(ignoreClient: fromClient);
			}
		}
		public void Send(int toClient = -1, int ignoreClient = -1) {
			if (NetmodeActive.SinglePlayer) return;
			ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
			packet.Write((byte)PegasusLib.Packets.WeakSyncedAction);
			packet.Write(Name);
			MemoryStream stream = new();
			BinaryWriter writer = new(stream);
			NetSend(writer);

			int size = (int)stream.Position;
			byte[] buffer = new byte[size];
			stream.Position = 0;
			stream.Read(buffer, 0, size);
			packet.Write(size);
			packet.Write(buffer);

			packet.Send(toClient, ignoreClient);
		}
		protected abstract void Perform();
		public abstract WeakSyncedAction NetReceive(BinaryReader reader);
		public abstract void NetSend(BinaryWriter writer);
	}
	public record class TestWeakSyncedAction(string Text) : WeakSyncedAction {
		public TestWeakSyncedAction() : this("") { }
		public override WeakSyncedAction NetReceive(BinaryReader reader) => this with {
			Text = "Bees"
		};
		public override void NetSend(BinaryWriter writer) {
			writer.Write(Text);
		}
		protected override void Perform() {
			ModContent.GetInstance<PegasusLib>().Logger.Info($"Performed WeakSyncedAction {Name}");
			ModContent.GetInstance<PegasusLib>().Logger.Info(Text);
			Main.NewText(Text);
		}
	}
	public class TestWeakSyncedActionCommand : ModCommand {
		public override string Command => "TestWeakSyncedAction";
		public override CommandType Type => CommandType.Chat;
		public override void Action(CommandCaller caller, string input, string[] args) {
			new TestWeakSyncedAction("Bees").Perform();
		}
	}
}
