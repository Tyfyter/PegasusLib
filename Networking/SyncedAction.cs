using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace PegasusLib.Networking {
	public abstract record class SyncedAction : ILoadable {
		static readonly List<SyncedAction> actions = [];
		static readonly Dictionary<Type, int> actionIDsByType = [];
		public static SyncedAction Get(int type) => actions[type];
		protected SyncedAction() {
			actionIDsByType?.TryGetValue(GetType(), out type);
		}
		private int type;
		/// <summary>
		/// In multiplayer, this is used to determine whether or not the action runs on clients
		/// Return true for anything that should only be run by the process that handles NPCs, the world, etc.
		/// </summary>
		public virtual bool ServerOnly => false;
		protected virtual bool ShouldPerform => true;
		public void Load(Mod mod) {
			type = (ushort)actions.Count;
			actions.Add(this);
			actionIDsByType.Add(GetType(), type);
		}
		public void Unload() { }
		public SyncedAction Read(BinaryReader reader) {
			return NetReceive(reader);
		}
		/// <summary>
		/// Performs the action, then sends it if appropriate
		/// </summary>
		/// <param name="fromClient"></param>
		public void Perform(int fromClient = -2) {
			if (!ShouldPerform) return;
			if (!NetmodeActive.MultiplayerClient || !ServerOnly) Perform();
			if ((NetmodeActive.Server && !ServerOnly) || (NetmodeActive.MultiplayerClient && fromClient == -2)) {
				Send(ignoreClient: fromClient);
			}
		}
		public void Send(int toClient = -1, int ignoreClient = -1) {
			if (NetmodeActive.SinglePlayer) return;
			ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
			packet.Write((byte)PegasusLib.Packets.SyncedAction);
			WriteType(packet);
			NetSend(packet);
			packet.Send(toClient, ignoreClient);
		}
		protected abstract void Perform();
		public abstract SyncedAction NetReceive(BinaryReader reader);
		public abstract void NetSend(BinaryWriter writer);
		internal void WriteType(BinaryWriter writer) {
			if (actions.Count < byte.MaxValue) writer.Write((byte)type);
			else if(actions.Count < ushort.MaxValue) writer.Write((ushort)type);
			else writer.Write((int)type);
		}
		internal static int ReadType(BinaryReader reader) {
			if (actions.Count < byte.MaxValue) return reader.ReadByte();
			if (actions.Count < ushort.MaxValue) return reader.ReadUInt16();
			return reader.ReadInt32();
		}
	}
}
