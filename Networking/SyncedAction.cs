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
using static PegasusLib.Networking.AutoSyncedAction;
using static PegasusLib.Networking.ISyncedAction;
using static PegasusLib.Networking.SyncedAction;

namespace PegasusLib.Networking;
public interface ISyncedAction : IAutoload<Impl> {
	bool ServerOnly => false;
	bool ShouldPerform => true;
	protected abstract void Perform();
	public abstract void NetSend(BinaryWriter writer);
	protected static abstract Read GetReader(Type type);
	public class Impl : IAutoloader {
		static readonly MethodInfo getReader = typeof(Impl).GetMethod(nameof(GetReader), BindingFlags.NonPublic | BindingFlags.Static);
		static readonly MethodInfo load = typeof(Impl).GetMethod(nameof(Load), BindingFlags.NonPublic | BindingFlags.Static);
		static void IAutoloader.Autoload(Mod mod, Type type) {
			if (!type.IsAssignableTo(typeof(ISyncedAction))) throw new InvalidOperationException($"Attempted to register invalid type {type} as {nameof(SyncedAction)}");
			if (mod.Side != ModSide.Both && mod is not PegasusLib) throw new InvalidOperationException("SyncedActions can only be added by Both-side mods");
			mod.Logger.Info($"{nameof(SyncedAction)} Loading {type.Name}");

			actionIDsByType.Add(type, readers.Count);
			ownedByMod.Add(type, mod);

			foreach (Type iAutoload in type.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPreLoad<>)))
				load.MakeGenericMethod(iAutoload.GetGenericArguments()).Invoke(null, [type]);

			readers.Add((Read)getReader.MakeGenericMethod([type]).Invoke(null, [type]));

			foreach (Type iAutoload in type.GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IOnLoad<>)))
				load.MakeGenericMethod(iAutoload.GetGenericArguments()).Invoke(null, [type]);
		}
		static Read GetReader<T>(Type type) where T : ISyncedAction => T.GetReader(type);
		static void Load<T>(Type type) where T : ILoadImpl => T.Load(type);
	}
	public delegate ISyncedAction Read(BinaryReader reader);
	static readonly List<Read> readers = [];
	static readonly Dictionary<Type, int> actionIDsByType = [];
	static readonly Dictionary<Type, Mod> ownedByMod = [];
	public sealed static Read GetReader(int type) => readers[type];
	/// <summary>
	/// Performs the action, then sends it if appropriate
	/// </summary>
	/// <param name="fromClient"></param>
	public sealed void Perform(int fromClient = -2) {
		if (!ShouldPerform) return;
		if (!NetmodeActive.MultiplayerClient || !ServerOnly) Perform();
		if ((NetmodeActive.Server && !ServerOnly) || (NetmodeActive.MultiplayerClient && fromClient == -2)) {
			Send(ignoreClient: fromClient);
		}
	}
	public sealed void Send(int toClient = -1, int ignoreClient = -1) {
		if (NetmodeActive.SinglePlayer) return;
		ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
		packet.Write((byte)PegasusLib.Packets.SyncedAction);
		WriteType(packet, GetType());
		NetSend(packet);
		packet.Send(toClient, ignoreClient);
	}
	static sealed void WriteType(BinaryWriter writer, Type type) {
		int _type = actionIDsByType[type];
		if (readers.Count < byte.MaxValue) writer.Write((byte)_type);
		else if (readers.Count < ushort.MaxValue) writer.Write((ushort)_type);
		else writer.Write((int)_type);
	}
	static sealed int ReadType(BinaryReader reader) {
		if (readers.Count < byte.MaxValue) return reader.ReadByte();
		if (readers.Count < ushort.MaxValue) return reader.ReadUInt16();
		return reader.ReadInt32();
	}
	public interface IOnLoad<TImpl> where TImpl : ILoadImpl { }
	public interface IPreLoad<TImpl> where TImpl : ILoadImpl { }
	public interface ILoadImpl {
		public static abstract void Load(Type type);
	}
}
public abstract record class SyncedAction : ISyncedAction, IOnLoad<SyncedAction.OnLoadImpl> {
	static readonly Dictionary<Type, SyncedAction> actions = [];
	public SyncedAction GetTemplate() => actions[GetType()];
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
	static SyncedAction CreateDefault(Type type) {
		if (actions.TryGetValue(type, out SyncedAction cached)) return cached;
		ConstructorInfo ctor = type.GetConstructors()[0];
		return actions[type] = (SyncedAction)ctor.Invoke([..ctor.GetParameters().Select(static parameter => {
			if (parameter.ParameterType.IsValueType)
				return Activator.CreateInstance(parameter.ParameterType);
			return null;
		})]);
	}
	static Read ISyncedAction.GetReader(Type type) => CreateDefault(type).Read;
	public class OnLoadImpl : ILoadImpl {
		static void ILoadImpl.Load(Type type) => CreateDefault(type).Load();
	}
	public virtual void Load() { }
	public SyncedAction Read(BinaryReader reader) {
		return NetReceive(reader);
	}
	void ISyncedAction.Perform() => Perform();
	protected abstract void Perform();
	public abstract SyncedAction NetReceive(BinaryReader reader);
	public abstract void NetSend(BinaryWriter writer);
	/// <inheritdoc cref="ISyncedAction.Perform(int)"/>
	public void Perform(int fromClient = -2) => ((ISyncedAction)this).Perform(fromClient);
	/// <inheritdoc cref="ISyncedAction.Send(int, int)"/>
	public void Send(int toClient = -1, int ignoreClient = -1) => ((ISyncedAction)this).Send(toClient, ignoreClient);
	#region sync validation
	/// <summary>
	/// Any actions returned here will be used to verify that the action is synced properly, used from <see cref="Mod.PostSetupContent"/>
	/// </summary>
	protected virtual IEnumerable<SyncedAction> SyncTests => [];
	private protected static void ValidateSync(SyncedAction action) {
		try {
			LoaderUtils.ForEachAndAggregateExceptions(action.SyncTests, syncTest => {
				if (syncTest.GetType() != action.GetType()) throw new InvalidOperationException($"{action.GetType()} tried to test its synchronization of other synced action type {syncTest.GetType()}");
				MemoryStream stream = new();
				BinaryWriter writer = new(stream);
				syncTest.NetSend(writer);
				stream.Position = 0;
				SyncedAction result = action.NetReceive(new(stream));
				if (!result.Equals(syncTest)) throw new InaccurateSynchronizationException(syncTest, result);
			});
		} catch (Exception e) {
			if (e.AttributeTo(ownedByMod[action.GetType()])) throw;
		}
	}
	internal static void TestAllSync() {
		LoaderUtils.ForEachAndAggregateExceptions(actions.Values, ValidateSync);
	}
	[Serializable]
	public class InaccurateSynchronizationException(ISyncedAction tested, ISyncedAction result) : Exception($"{tested.GetType()} not synchronized properly, {tested} -> {result}") { }
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
		LooseDictionary<Type, (MethodInfo write, MethodInfo read)> syncSets = GetSyncSets(GetType());

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
			if (!syncSets.TryGetValue(property.PropertyType, out (MethodInfo write, MethodInfo read) sync, out int inaccuracy)) {
				throw new NotSupportedException($"\nProperty {property} is unsupported type {property.PropertyType}, you can add support with {nameof(AutoSyncSendAttribute)} and {nameof(AutoSyncReceiveAttribute)}");
			}
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
	internal static LooseDictionary<Type, (MethodInfo write, MethodInfo read)> GetSyncSets(Type type) {
		Type originalType = type;
		LooseDictionary<Type, (MethodInfo write, MethodInfo read)> syncSets = new(LooseDictionary.ParentType);
		void TryAddVanilla<T>(Action<BinaryWriter, T> _write, Func<BinaryReader, T> _read) {
			MethodInfo write = _write.Method;
			MethodInfo read = _read.Method;
			if (write.ReturnType != typeof(void)) throw new ArgumentException($"Invalid write return type {write.ReturnType}", nameof(_write));
			if (write.GetParameters().Length != 2) throw new ArgumentException($"Invalid write arg count {write.GetParameters().Length}", nameof(_write));
			if (read.GetParameters().Length != 1) throw new ArgumentException($"Invalid read arg count {read.GetParameters().Length}", nameof(_read));
			syncSets.TryAdd(read.ReturnType, (write, read));
		}
		void AddFromType(Type type) {
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
				ownedByMod[originalType].Logger.Warn($"Ignoring send/receive methods in {originalType} due to not being balanced:[{string.Join(", ", warnings.Select(type => $"{type} missing {(reads.ContainsKey(type) ? "send" : "receive")}"))}]");
			}
			foreach (Type copyFrom in type.GetCustomAttributes<AutoSyncMethodsAttribute>().SelectMany(a => a)) {
				AddFromType(copyFrom);
			}
			foreach (Type copyFrom in type.GetInterfaces().SelectMany(i => i.GetCustomAttributes<AutoSyncMethodsAttribute>().SelectMany(a => a))) {
				AddFromType(copyFrom);
			}
		}
		do {
			AddFromType(type);
		} while ((type = type.BaseType) is not null);
		TryAddVanilla(Utils.WriteVector2, Utils.ReadVector2);
		return syncSets;
	}
	public override void NetSend(BinaryWriter writer) => ((AutoSyncedAction)GetTemplate()).write(this, writer);
	public override SyncedAction NetReceive(BinaryReader reader) => read(this, reader);
	#region attributes
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
	#endregion
	#region helper methods
#pragma warning disable IDE0051
	[AutoSyncSend<Boolean>]
	static void WriteBoolean(BinaryWriter writer, Boolean value) => writer.Write(value);
	[AutoSyncReceive<Boolean>]
	static Boolean ReadBoolean(BinaryReader reader) => reader.ReadBoolean();

	[AutoSyncSend<Byte>]
	static void WriteByte(BinaryWriter writer, Byte value) => writer.Write(value);
	[AutoSyncReceive<Byte>]
	static Byte ReadByte(BinaryReader reader) => reader.ReadByte();

	[AutoSyncSend<Char>]
	static void WriteChar(BinaryWriter writer, Char value) => writer.Write(value);
	[AutoSyncReceive<Char>]
	static Char ReadChar(BinaryReader reader) => reader.ReadChar();

	[AutoSyncSend<Decimal>]
	static void WriteDecimal(BinaryWriter writer, Decimal value) => writer.Write(value);
	[AutoSyncReceive<Decimal>]
	static Decimal ReadDecimal(BinaryReader reader) => reader.ReadDecimal();

	[AutoSyncSend<Double>]
	static void WriteDouble(BinaryWriter writer, Double value) => writer.Write(value);
	[AutoSyncReceive<Double>]
	static Double ReadDouble(BinaryReader reader) => reader.ReadDouble();

	[AutoSyncSend<Half>]
	static void WriteHalf(BinaryWriter writer, Half value) => writer.Write(value);
	[AutoSyncReceive<Half>]
	static Half ReadHalf(BinaryReader reader) => reader.ReadHalf();

	[AutoSyncSend<Int32>]
	static void WriteInt32(BinaryWriter writer, Int32 value) => writer.Write(value);
	[AutoSyncReceive<Int32>]
	static Int32 ReadInt32(BinaryReader reader) => reader.ReadInt32();

	[AutoSyncSend<Int64>]
	static void WriteInt64(BinaryWriter writer, Int64 value) => writer.Write(value);
	[AutoSyncReceive<Int64>]
	static Int64 ReadInt64(BinaryReader reader) => reader.ReadInt64();

	[AutoSyncSend<SByte>]
	static void WriteSByte(BinaryWriter writer, SByte value) => writer.Write(value);
	[AutoSyncReceive<SByte>]
	static SByte ReadSByte(BinaryReader reader) => reader.ReadSByte();

	[AutoSyncSend<Single>]
	static void WriteSingle(BinaryWriter writer, Single value) => writer.Write(value);
	[AutoSyncReceive<Single>]
	static Single ReadSingle(BinaryReader reader) => reader.ReadSingle();

	[AutoSyncSend<String>]
	static void WriteString(BinaryWriter writer, String value) => writer.Write(value);
	[AutoSyncReceive<String>]
	static String ReadString(BinaryReader reader) => reader.ReadString();

	[AutoSyncSend<UInt16>]
	static void WriteUInt16(BinaryWriter writer, UInt16 value) => writer.Write(value);
	[AutoSyncReceive<UInt16>]
	static UInt16 ReadUInt16(BinaryReader reader) => reader.ReadUInt16();

	[AutoSyncSend<UInt32>]
	static void WriteUInt32(BinaryWriter writer, UInt32 value) => writer.Write(value);
	[AutoSyncReceive<UInt32>]
	static UInt32 ReadUInt32(BinaryReader reader) => reader.ReadUInt32();

	[AutoSyncSend<UInt64>]
	static void WriteUInt64(BinaryWriter writer, UInt64 value) => writer.Write(value);
	[AutoSyncReceive<UInt64>]
	static UInt64 ReadUInt64(BinaryReader reader) => reader.ReadUInt64();
#pragma warning restore IDE0051
	#endregion
}
public static class SyncedActionExtensions {
	/// <inheritdoc cref="ISyncedAction.Perform(int)"/>
	public static void Perform(this ISyncedAction action, int fromClient = -2) => action.Perform(fromClient);
	/// <inheritdoc cref="ISyncedAction.Send(int, int)"/>
	public static void Send(this ISyncedAction action, int toClient = -1, int ignoreClient = -1) => action.Send(toClient, ignoreClient);
	public static Mod GetMod(this ISyncedAction action) => ownedByMod.TryGetValue(action.GetType(), out Mod mod) ? mod : null;
}
[AutoSyncMethods<AutoSyncedAction>]
public interface IAutoSyncedAction : ISyncedAction, IPreLoad<IAutoSyncedAction.PreLoadImpl>, IOnLoad<IAutoSyncedAction.OnLoadImpl> {
	private sealed static Dictionary<Type, Write> Writes { get; set; } = [];
	private sealed static Dictionary<Type, Read> Reads { get; set; } = [];
	public class PreLoadImpl : ILoadImpl {
		static void ILoadImpl.Load(Type type) {
			if (!type.IsValueType) throw new InvalidOperationException($"{nameof(IAutoSyncedAction)}s must be structs, use {nameof(AutoSyncedAction)} for classes");
			StringBuilder warnings = new();
			LooseDictionary<Type, (MethodInfo write, MethodInfo read)> syncSets = GetSyncSets(type);

			DynamicMethod _write = new("write", typeof(void), [typeof(IAutoSyncedAction), typeof(BinaryWriter)]);
			ILGenerator write = _write.GetILGenerator();
			DynamicMethod _read = new("read", typeof(IAutoSyncedAction), [typeof(BinaryReader)]);
			ILGenerator read = _read.GetILGenerator();
			write.DeclareLocal(type);
			write.Emit(OpCodes.Ldarg_0);
			write.Emit(OpCodes.Unbox_Any, type);
			write.Emit(OpCodes.Stloc_0);

			read.DeclareLocal(type);
			read.Emit(OpCodes.Ldloca_S, 0);
			read.Emit(OpCodes.Initobj, type);

			foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (property.GetMethod is not MethodInfo get) continue;
				if (property.SetMethod is not MethodInfo set) continue;
				if (!syncSets.TryGetValue(property.PropertyType, out (MethodInfo write, MethodInfo read) sync, out int inaccuracy)) {
					throw new NotSupportedException($"\nProperty {property} is unsupported type {property.PropertyType}, you can add support with {nameof(AutoSyncSendAttribute)} and {nameof(AutoSyncReceiveAttribute)}");
				}
				if (inaccuracy > 0) warnings.Append($"\nProperty {property} falling back to ");
				(MethodInfo send, MethodInfo receive) = sync;
				write.Emit(OpCodes.Ldarg_1);
				write.Emit(OpCodes.Ldloca_S, 0);
				write.Emit(OpCodes.Call, get);
				write.Emit(OpCodes.Call, send);

				read.Emit(OpCodes.Ldloca_S, 0);
				read.Emit(OpCodes.Ldarg_0);
				read.Emit(OpCodes.Call, receive);
				read.Emit(OpCodes.Call, set);
			}

			write.Emit(OpCodes.Ret);
			read.Emit(OpCodes.Ldloc_0);
			read.Emit(OpCodes.Box, type);
			read.Emit(OpCodes.Ret);
			Writes[type] = _write.CreateDelegate<Write>();
			Reads[type] = _read.CreateDelegate<Read>();
			if (warnings.Length > 0) ownedByMod[type].Logger.Warn($"Some data may be lost when syncing {type}:{warnings}");
			PropertyInfo SyncTests = type.GetProperty("SyncTests", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (SyncTests is null) return;
			if (SyncTests.GetMethod is null || SyncTests.PropertyType != typeof(IEnumerable<>).MakeGenericType(type)) throw new InvalidOperationException($"{type} tried to test its synchronization with invalid sync tests {SyncTests}");
			Read readForTests = Reads[type];
			var exceptions = new List<Exception>();

			foreach (object _syncTest in (IEnumerable)SyncTests.GetValue(null)) {
				try {
					ISyncedAction syncTest = (ISyncedAction)_syncTest;
					MemoryStream stream = new();
					BinaryWriter writer = new(stream);
					syncTest.NetSend(writer);
					stream.Position = 0;
					ISyncedAction result = readForTests(new(stream));
					if (!result.Equals(syncTest)) throw new InaccurateSynchronizationException(syncTest, result);
				} catch (Exception ex) {
					var aaaaaa = ex.GetType();
					ex.Data["contentType"] = type;
					exceptions.Add(ex);
				}
			}

			LoaderUtils.RethrowAggregatedExceptions(exceptions);
		}
	}
	public class OnLoadImpl : ILoadImpl {
		static void ILoadImpl.Load(Type type) => ownedByMod[type].Logger.Info($"Many friendly bees helped load {type}");
	}
	void ISyncedAction.NetSend(BinaryWriter writer) => Writes[GetType()](this, writer);
	static Read ISyncedAction.GetReader(Type type) => Reads[type];
	public delegate void Write(IAutoSyncedAction action, BinaryWriter writer);
}
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true, AllowMultiple = true)]
public class AutoSyncMethodsAttribute(params Type[] types) : Attribute, IEnumerable<Type> {
	public IEnumerable<Type> Types { get; } = types;
	IEnumerator<Type> IEnumerable<Type>.GetEnumerator() {
		return Types.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable)Types).GetEnumerator();
	}
}
public class AutoSyncMethodsAttribute<T>() : AutoSyncMethodsAttribute(typeof(T)) { }