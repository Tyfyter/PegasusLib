using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PegasusLib.Networking;
public abstract record class SyncedAction : ILoadable {
	static readonly List<SyncedAction> actions = [];
	static readonly Dictionary<Type, int> actionIDsByType = [];
	static readonly Dictionary<Type, Mod> ownedByMod = [];
	public static SyncedAction Get(int type) => actions[type];
	protected SyncedAction() {
		actionIDsByType?.TryGetValue(GetType(), out type);
	}
	private protected int type;
	public Mod Mod => ownedByMod.TryGetValue(GetType(), out Mod mod) ? mod : null;
	/// <summary>
	/// In multiplayer, this is used to determine whether or not the action runs on clients
	/// Return true for anything that should only be run by the process that handles NPCs, the world, etc.
	/// </summary>
	public virtual bool ServerOnly => false;
	protected virtual bool ShouldPerform => true;
	void ILoadable.Load(Mod mod) {
		if (mod.Side != ModSide.Both && mod is not PegasusLib) throw new InvalidOperationException("SyncedActions can only be added by Both-side mods");
		type = actions.Count;
		actions.Add(this);
		actionIDsByType.Add(GetType(), type);
		ownedByMod.Add(GetType(), mod);
		mod.Logger.Info($"{nameof(SyncedAction)} Loading {GetType().Name}");
		Load();
	}
	public virtual void Load() { }
	void ILoadable.Unload() { }
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
	private protected static void ValidateSync(SyncedAction action) {
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
	public class InaccurateSynchronizationException(SyncedAction tested, SyncedAction result) : Exception($"{tested.GetType()} not synchronized properly, {tested} -> {result}") { }
	public virtual bool Equals(SyncedAction other) => ReferenceEquals(this, other) || this.EqualityContract == other.EqualityContract;
	public override int GetHashCode() => EqualityContract.GetHashCode();
	#endregion
	#region helper methods
	[AutoSyncedAction.AutoSyncSend<Point16>]
	public static void WritePoint16(BinaryWriter writer, Point16 point) {
		writer.Write((short)point.X);
		writer.Write((short)point.Y);
	}
	[AutoSyncedAction.AutoSyncReceive<Point16>]
	public static Point16 ReadPoint16(BinaryReader reader) => new(reader.ReadInt16(), reader.ReadInt16());
	[AutoSyncedAction.AutoSyncSend<Point>]
	public static void WritePoint(BinaryWriter writer, Point point) {
		writer.Write((int)point.X);
		writer.Write((int)point.Y);
	}
	[AutoSyncedAction.AutoSyncReceive<Point>]
	public static Point ReadPoint(BinaryReader reader) => new(reader.ReadInt32(), reader.ReadInt32());
	[AutoSyncedAction.AutoSyncSend<Player>]
	public static void WritePlayer(BinaryWriter writer, Player player) => writer.Write((byte)player.whoAmI);
	[AutoSyncedAction.AutoSyncReceive<Player>]
	public static Player ReadPlayer(BinaryReader reader) => Main.player[reader.ReadByte()];
	[AutoSyncedAction.AutoSyncSend<Projectile>]
	public static void WriteProjectile(BinaryWriter writer, Projectile projectile) {
		writer.Write((byte)projectile.owner);
		writer.Write((ushort)projectile.identity);
	}
	[AutoSyncedAction.AutoSyncReceive<Projectile>]
	public static Projectile ReadProjectile(BinaryReader reader) {
		byte owner = reader.ReadByte();
		ushort identity = reader.ReadUInt16();
		foreach (Projectile projectile in Main.ActiveProjectiles) {
			if (projectile.owner == owner && projectile.identity == identity) return projectile;
		}
		return null;
	}
	[AutoSyncedAction.AutoSyncSend<NPC>]
	public static void WriteNPC(BinaryWriter writer, NPC npc) => writer.Write((byte)npc.whoAmI);
	[AutoSyncedAction.AutoSyncReceive<NPC>]
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
public abstract record class AutoSyncedAction : SyncedAction {
	Action<AutoSyncedAction, BinaryWriter> write;
	Func<AutoSyncedAction, BinaryReader, AutoSyncedAction> read;
	public virtual bool Equals(AutoSyncedAction other) => ReferenceEquals(this, other) || this.EqualityContract == other.EqualityContract;
	public override int GetHashCode() => EqualityContract.GetHashCode();
	public override void Load() {
		StringBuilder warnings = new();
		LooseDictionary<Type, (MethodInfo write, MethodInfo read)> syncSets = GetSyncSets();

		DynamicMethod _write = new("write", typeof(void), [typeof(AutoSyncedAction), typeof(BinaryWriter)]);
		ILGenerator write = _write.GetILGenerator();
		DynamicMethod _read = new("read", typeof(AutoSyncedAction), [typeof(AutoSyncedAction), typeof(BinaryReader)]);
		ILGenerator read = _read.GetILGenerator();
		read.DeclareLocal(GetType());
		read.Emit(OpCodes.Ldarg_0);
		read.Emit(OpCodes.Newobj, GetType().GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, [GetType()]));
		read.Emit(OpCodes.Stloc_0);

		foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
			if (property.GetMethod is not MethodInfo get) continue;
			if (property.SetMethod is not MethodInfo set) continue;
			if (!set.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(System.Runtime.CompilerServices.IsExternalInit))) continue;
			if (!syncSets.TryGetValue(property.PropertyType, out (MethodInfo write, MethodInfo read) sync, out int inaccuracy)) continue;
			if (inaccuracy > 0) warnings.Append($"\nProperty {property} falling back to ");
			(MethodInfo send, MethodInfo receive) = sync;
			write.Emit(OpCodes.Ldarg_1);
			write.Emit(OpCodes.Ldarg_0);
			write.Emit(OpCodes.Call, get);
			write.Emit(OpCodes.Call, send);

			read.Emit(OpCodes.Ldloc_0);
			read.Emit(OpCodes.Ldarg_1);
			read.Emit(OpCodes.Call, receive);
			read.Emit(OpCodes.Call, set);
		}

		write.Emit(OpCodes.Ret);
		read.Emit(OpCodes.Ldloc_0);
		read.Emit(OpCodes.Ret);
		this.write = _write.CreateDelegate<Action<AutoSyncedAction, BinaryWriter>>();
		this.read = _read.CreateDelegate<Func<AutoSyncedAction, BinaryReader, AutoSyncedAction>>();
		if (warnings.Length > 0) Mod.Logger.Warn($"Some data may be lost when syncing {GetType()}:{warnings}");
		ValidateSync(this);
	}
	LooseDictionary<Type, (MethodInfo write, MethodInfo read)> GetSyncSets() {
		LooseDictionary<Type, (MethodInfo write, MethodInfo read)> syncSets = new(LooseDictionary.ParentType);

		Type type = GetType();
		do {
			Dictionary<Type, MethodInfo> writes = [];
			Dictionary<Type, MethodInfo> reads = [];
			foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
				if (method.GetCustomAttribute<AutoSyncSendAttribute>() is AutoSyncSendAttribute send) {
					StringBuilder errors = new();
					if (method.ReturnType != typeof(void)) errors.Append("\nReturn type must be null");
					if (method.GetParameters().Length != 2
						|| method.GetParameters()[0].ParameterType != typeof(BinaryWriter)
						|| method.GetParameters()[1].ParameterType != send.Type
					) errors.Append($"\nParameters must be (BinaryWriter, {send.Type})");

					if (errors.Length > 0) throw new InvalidProgramException($"{method} cannot write {send.Type} because:{errors}");
					writes.Add(send.Type, method);
					continue;
				}
				if (method.GetCustomAttribute<AutoSyncReceiveAttribute>() is AutoSyncReceiveAttribute receive) {
					StringBuilder errors = new();
					if (method.ReturnType != receive.Type) errors.Append($"\nReturn type must be {receive.Type}");
					if (method.GetParameters().Length != 1 || method.GetParameters()[0].ParameterType != typeof(BinaryReader)) errors.Append($"\nParameters must be (BinaryReader)");

					if (errors.Length > 0) throw new InvalidProgramException($"{method} cannot read {receive.Type} because:{errors}");
					reads.Add(receive.Type, method);
				}
			}
			foreach ((Type syncType, MethodInfo write) in writes) {
				if (reads.TryGetValue(syncType, out MethodInfo read)) {
					syncSets.TryAdd(syncType, (write, read));
				}
			}
			HashSet<Type> warnings = writes.Keys.ToHashSet();
			warnings.SymmetricExceptWith(reads.Keys.ToHashSet());
			if (warnings.Count > 0) {
				Mod.Logger.Warn($"Ignoring send/receive methods in {GetType()} due to not being balanced:[{
					string.Join(", ", warnings.Select(type => $"{type} missing {(reads.ContainsKey(type) ? "send" : "receive")}"))
				}]");
			}
		} while ((type = type.BaseType) is not null);
		return syncSets;
	}
	public override void NetSend(BinaryWriter writer) => ((AutoSyncedAction)Get(type)).write(this, writer);
	public override SyncedAction NetReceive(BinaryReader reader) => read(this, reader);
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	protected internal class AutoSyncSendAttribute(Type type) : Attribute {
		public Type Type { get; } = type;
	}
	protected internal sealed class AutoSyncSendAttribute<T>() : AutoSyncSendAttribute(typeof(T)) { }
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	protected internal class AutoSyncReceiveAttribute(Type type) : Attribute {
		public Type Type { get; } = type;
	}
	protected internal sealed class AutoSyncReceiveAttribute<T>() : AutoSyncReceiveAttribute(typeof(T)) { }
}