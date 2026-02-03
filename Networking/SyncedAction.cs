using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PegasusLib.Networking {
	public abstract record class SyncedAction : ILoadable {
		static readonly List<SyncedAction> actions = [];
		static readonly Dictionary<Type, int> actionIDsByType = [];
		static readonly Dictionary<Type, Mod> ownedByMod = [];
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
			if (mod.Side != ModSide.Both && mod is not PegasusLib) throw new InvalidOperationException("SyncedActions can only be added by Both-side mods");
			type = actions.Count;
			actions.Add(this);
			actionIDsByType.Add(GetType(), type);
			ownedByMod.Add(GetType(), mod);
			mod.Logger.Info($"{nameof(SyncedAction)} Loading {GetType().Name}");
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
			else if (actions.Count < ushort.MaxValue) writer.Write((ushort)type);
			else writer.Write((int)type);
		}
		internal static int ReadType(BinaryReader reader) {
			if (actions.Count < byte.MaxValue) return reader.ReadByte();
			if (actions.Count < ushort.MaxValue) return reader.ReadUInt16();
			return reader.ReadInt32();
		}
		#region sync validation
		/// <summary>
		/// Any actions returned here will be used to verify that the action is synced properly, used from <see cref="Mod.PostSetupContent"/>
		/// </summary>
		protected virtual IEnumerable<SyncedAction> SyncTests => [];
		static void ValidateSync(SyncedAction action) {
			try {
				LoaderUtils.ForEachAndAggregateExceptions(action.SyncTests, syncTest => {
					if (syncTest.GetType() != action.GetType()) throw new InvalidOperationException($"{action.GetType()} tried to test its synchronization of other synced action type {syncTest.GetType()}");
					if (syncTest == action) return;
					MemoryStream stream = new();
					BinaryWriter writer = new(stream);
					syncTest.NetSend(writer);
					stream.Position = 0;
					SyncedAction result = action.NetReceive(new(stream));
					if (syncTest != result) throw new InaccurateSynchronizationException(syncTest, result);
				});
			} catch (Exception e) {
				if (e.AttributeTo(ownedByMod[action.GetType()])) throw;
			}
		}
		internal static void TestAllSync() {
			LoaderUtils.ForEachAndAggregateExceptions(actions, ValidateSync);
		}
		[Serializable]
		public class InaccurateSynchronizationException(SyncedAction tested, SyncedAction result) : Exception($"{tested.GetType()} not synchronized properly, {tested} -> {result}") {}
		#endregion
		#region helper methods
		public static void WritePlayer(BinaryWriter writer, Player player) => writer.Write((byte)player.whoAmI);
		public static Player ReadPlayer(BinaryReader reader) => Main.player[reader.ReadByte()];
		public static void WriteProjectile(BinaryWriter writer, Projectile projectile) {
			writer.Write((byte)projectile.owner);
			writer.Write((ushort)projectile.identity);
		}
		public static Projectile ReadProjectile(BinaryReader reader) {
			byte owner = reader.ReadByte();
			ushort identity = reader.ReadUInt16();
			foreach (Projectile projectile in Main.ActiveProjectiles) {
				if (projectile.owner == owner && projectile.identity == identity) return projectile;
			}
			return null;
		}
		public static void WriteNPC(BinaryWriter writer, NPC npc) => writer.Write((byte)npc.whoAmI);
		public static NPC ReadNPC(BinaryReader reader) => Main.npc[reader.ReadByte()];
		/// <summary>
		/// <paramref name="options"/> must have the same order as when used in <see cref="ReadUnorderedSet"/>
		/// </summary>
		public static void WriteUnorderedSet<T>(BinaryWriter writer, IEnumerable<T> options, params T[] values) {
			BitArray array = new(options.Select(values.Contains).ToArray());
			writer.Write((ushort)array.Length);
			Utils.SendBitArray(array, writer);
		}
		/// <summary>
		/// <paramref name="options"/> must have the same order as when used in <see cref="WriteUnorderedSet"/>
		/// </summary>
		public static IEnumerable<T> ReadUnorderedSet<T>(BinaryReader reader, IEnumerable<T> options) {
			BitArray array = Utils.ReceiveBitArray(reader.ReadUInt16(), reader);
			int i = 0;
			foreach (T item in options) {
				if (array[i++]) yield return item;
			}
		}
		/// <summary>
		/// Reorders <paramref name="values"/> to follow the order of <paramref name="options"/>
		/// </summary>
		public static T[] ReorderSet<T>(IEnumerable<T> options, params T[] values) {
			T[] output = new T[values.Length];
			int i = 0;
			foreach (T item in options) {
				if (values.Contains(item)) output[i++] = item;
			}
			Array.Resize(ref output, i);
			return output;
		}
		public static void WriteArray<T>(BinaryWriter writer, T[] items, Action<BinaryWriter, T> write) {
			writer.Write((ushort)items.Length);
			for (int i = 0; i < items.Length; i++) {
				write(writer, items[i]);
			}
		}
		public static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> read) {
			T[] items = new T[reader.ReadUInt16()];
			for (int i = 0; i < items.Length; i++) {
				items[i] = read(reader);
			}
			return items;
		}
		#endregion
	}
}
