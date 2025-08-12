global using Color = Microsoft.Xna.Framework.Color;
global using Rectangle = Microsoft.Xna.Framework.Rectangle;
global using Vector2 = Microsoft.Xna.Framework.Vector2;
global using Vector3 = Microsoft.Xna.Framework.Vector3;
using MonoMod.Utils;
using PegasusLib.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ObjectData;
using Terraria.UI;

namespace PegasusLib {
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class PegasusLib : Mod {
		internal static List<IUnloadable> unloadables = [];
		internal static Dictionary<LibFeature, List<Mod>> requiredFeatures = [];
		internal static Dictionary<LibFeature, Exception> erroredFeatures = [];
		internal static Dictionary<LibFeature, Action<Exception>> onFeatureError = [];
		public override void Load() {
			On_Main.DrawNPCDirect += IDrawNPCEffect.On_Main_DrawNPCDirect;
			On_Main.DrawProj_Inner += IDrawProjectileEffect.On_Main_DrawProj_Inner;
			On_Main.DrawItem += IDrawItemInWorldEffect.On_Main_DrawItem;
			On_ItemSlot.DrawItemIcon += IDrawItemInInventoryEffect.On_ItemSlot_DrawItemIcon;

			MonoModHooks.Modify(typeof(NPCLoader).GetMethod(nameof(NPCLoader.PreDraw)), IDrawNPCEffect.AddIteratePreDraw);
			MonoModHooks.Modify(typeof(NPCLoader).GetMethod(nameof(NPCLoader.PostDraw)), IDrawNPCEffect.AddIteratePostDraw);
		}
		public static void Require(Mod mod, params LibFeature[] features) {
			for (int i = 0; i < features.Length; i++) {
				LibFeature feature = features[i];
				if (erroredFeatures.TryGetValue(feature, out Exception exception)) {
					throw new Exception($"Error while loading feature {feature} required by mod {mod.DisplayNameClean}:", exception);
				} else {
					if (!requiredFeatures.TryGetValue(feature, out List<Mod> reqs)) requiredFeatures[feature] = reqs = [];
					if (!reqs.Contains(mod)) reqs.Add(mod);
				}
			}
		}
		public static void FeatureError(LibFeature feature, Exception exception) {
			if (onFeatureError.TryGetValue(feature, out Action<Exception> handlers)) handlers(exception);
			if (requiredFeatures.TryGetValue(feature, out List<Mod> mods)) {
				throw new Exception($"Error while loading feature {feature} required by mods [{string.Join(", ", mods.Select(mod => mod.DisplayNameClean))}]:", exception);
			} else {
				erroredFeatures.Add(feature, exception);
				ModContent.GetInstance<PegasusLib>().Logger.Error(exception);
			}
		}
		public static bool IsFeatureErrored(LibFeature feature) => erroredFeatures.ContainsKey(feature);
		delegate void _onFeatureError(Exception exception);
		public static void OnFeatureError(LibFeature feature, Action<Exception> handler) {
			if (erroredFeatures.TryGetValue(feature, out Exception exception)) {
				handler(exception);
				return;
			}
			if (onFeatureError.TryGetValue(feature, out Action<Exception> @delegate)) {
				onFeatureError[feature] = @delegate + handler;
			} else {
				onFeatureError[feature] = handler;
			}
		}
		public override void Unload() {
			foreach (IUnloadable unloadable in unloadables) {
				unloadable.Unload();
			}
			unloadables = null;
			requiredFeatures = null;
			erroredFeatures = null;
			DropRuleExt.Unload();
		}
		public override object Call(params object[] args) {
			switch (((string)args[0]).ToUpperInvariant()) {
				case "ADDRULECHILDFINDER":
				if (args[1] is Type type && args[2] is Delegate del && del.TryCastDelegate(out DropRuleExt.RuleChildFinder finder)) {
					return DropRuleExt.RuleChildFinders.TryAdd(type, finder);
				}
				return false;
			}
			return null;
		}
		public static Color GetRarityColor(int rare, bool expert = false, bool master = false) {
			if (expert || rare == ItemRarityID.Expert) {
				return Main.DiscoColor;
			}
			if (master || rare == ItemRarityID.Master) {
				return new Color(255, (int)(Main.masterColor * 200), 0);
			}
			if (rare >= ItemRarityID.Count) {
				return RarityLoader.GetRarity(rare).RarityColor;
			}
			switch (rare) {
				case ItemRarityID.Quest:
				return Colors.RarityAmber;

				case ItemRarityID.Gray:
				return Colors.RarityTrash;

				case ItemRarityID.Blue:
				return Colors.RarityBlue;

				case ItemRarityID.Green:
				return Colors.RarityGreen;

				case ItemRarityID.Orange:
				return Colors.RarityOrange;

				case ItemRarityID.LightRed:
				return Colors.RarityRed;

				case ItemRarityID.Pink:
				return Colors.RarityPink;

				case ItemRarityID.LightPurple:
				return Colors.RarityPurple;

				case ItemRarityID.Lime:
				return Colors.RarityLime;

				case ItemRarityID.Yellow:
				return Colors.RarityYellow;

				case ItemRarityID.Cyan:
				return Colors.RarityCyan;

				case ItemRarityID.Red:
				return Colors.RarityDarkRed;

				case ItemRarityID.Purple:
				return Colors.RarityDarkPurple;
			}
			return Colors.RarityNormal;
		}
		/// <inheritdoc cref="TileUtils.GetMultiTileTopLeft(int, int, TileObjectData, out int, out int)"/>
		[Obsolete("Moved to TileUtils")]
		public static void GetMultiTileTopLeft(int i, int j, TileObjectData data, out int left, out int top) => TileUtils.GetMultiTileTopLeft(i, j, data, out left, out top);
		public static T Compile<T>(string name, params (OpCode, object)[] instructions) where T : Delegate {
			MethodInfo invoke = typeof(T).GetMethod("Invoke");
			DynamicMethod method = new(name, invoke.ReturnType, invoke.GetParameters().Select(p => p.ParameterType).ToArray());
			ILGenerator gen = method.GetILGenerator();
			for (int i = 0; i < instructions.Length; i++) {
				object operand = instructions[i].Item2;
				if (operand is byte @byte) gen.Emit(instructions[i].Item1, @byte);
				else if (operand is short @short) gen.Emit(instructions[i].Item1, @short);
				else if (operand is long @long) gen.Emit(instructions[i].Item1, @long);
				else if (operand is float @float) gen.Emit(instructions[i].Item1, @float);
				else if (operand is double @double) gen.Emit(instructions[i].Item1, @double);
				else if (operand is int @int) gen.Emit(instructions[i].Item1, @int);
				else if (operand is MethodInfo methodInfo) gen.Emit(instructions[i].Item1, methodInfo);
				else if (operand is SignatureHelper signatureHelper) gen.Emit(instructions[i].Item1, signatureHelper);
				else if (operand is ConstructorInfo constructorInfo) gen.Emit(instructions[i].Item1, constructorInfo);
				else if (operand is Type @type) gen.Emit(instructions[i].Item1, @type);
				else if (operand is Label label) gen.Emit(instructions[i].Item1, label);
				else if (operand is Label[] labels) gen.Emit(instructions[i].Item1, labels);
				else if (operand is FieldInfo fieldInfo) gen.Emit(instructions[i].Item1, fieldInfo);
				else if (operand is string @string) gen.Emit(instructions[i].Item1, @string);
				else if (operand is LocalBuilder localBuilder) gen.Emit(instructions[i].Item1, localBuilder);
				else if (operand is sbyte @sbyte) gen.Emit(instructions[i].Item1, @sbyte);
				else if (operand is null) gen.Emit(instructions[i].Item1);
			}
			return method.CreateDelegate<T>();
		}
		public static bool TrySet<T>(ref T value, T newValue) {
			if (!Equals(value, newValue)) {
				value = newValue;
				return true;
			}
			return false;
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			switch ((Packets)reader.ReadByte()) {
				case Packets.SyncKeybindHandler:
				int forPlayer = reader.ReadByte();
				string name = reader.ReadString();
				BitArray values = Utils.ReceiveBitArray(reader.ReadByte(), reader);
				if (whoAmI != forPlayer && Main.netMode == NetmodeID.Server) break;

				if (KeybindHandlerPlayer.playerIDsByName.TryGetValue(name, out int index)) {
					KeybindHandlerPlayer khPlayer = (KeybindHandlerPlayer)Main.player[forPlayer].ModPlayers[index];
					khPlayer.netBits = values;
					if (Main.netMode == NetmodeID.Server) khPlayer.SendSync(whoAmI);
				} else {
					ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
					packet.Write((byte)Packets.SyncKeybindHandler);
					packet.Write((byte)forPlayer);
					packet.Write(name);
					packet.Write((byte)values.Count);
					Utils.SendBitArray(values, packet);
					packet.Send(ignoreClient: whoAmI);
				}
				break;

				case Packets.SyncedAction:
				SyncedAction.Get(reader.ReadUInt16()).Read(reader).Perform(whoAmI);
				break;
			}
		}
		internal enum Packets : byte {
			SyncKeybindHandler,
			SyncedAction
		}
	}
	public ref struct ReverseEntityGlobalsEnumerator<TGlobal>(TGlobal[] baseGlobals, TGlobal[] entityGlobals) where TGlobal : GlobalType<TGlobal> {
		static readonly Func<IEntityWithGlobals<TGlobal>, TGlobal[]> getArray = PegasusLib.Compile<Func<IEntityWithGlobals<TGlobal>, TGlobal[]>>("getEntityGlobals",
			(OpCodes.Ldarg_0, null),
			(OpCodes.Callvirt, typeof(IEntityWithGlobals<TGlobal>).GetProperty(nameof(IEntityWithGlobals<TGlobal>.EntityGlobals)).GetGetMethod()),
			(OpCodes.Ldfld, typeof(RefReadOnlyArray<TGlobal>).GetField("array", BindingFlags.NonPublic | BindingFlags.Instance)),
			(OpCodes.Ret, null)
		);
		private readonly RefReadOnlyArray<TGlobal> baseGlobals = (entityGlobals is null) ? [] : baseGlobals;
		private readonly RefReadOnlyArray<TGlobal> entityGlobals = entityGlobals;
		private int i = 1;
		private TGlobal current = null;
		public readonly TGlobal Current => current;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReverseEntityGlobalsEnumerator(IEntityWithGlobals<TGlobal> entity) : this(GlobalTypeLookups<TGlobal>.GetGlobalsForType(entity.Type), entity) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReverseEntityGlobalsEnumerator(TGlobal[] baseGlobals, IEntityWithGlobals<TGlobal> entity) : this(baseGlobals, getArray(entity)) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext() {
			while (i <= baseGlobals.Length) {
				current = baseGlobals[^i++];
				short slot = current.PerEntityIndex;
				if (slot < 0) {
					return true;
				}
				current = entityGlobals[slot];
				if (current != null) {
					return true;
				}
			}
			return false;
		}
		public readonly ReverseEntityGlobalsEnumerator<TGlobal> GetEnumerator() {
			return this;
		}
	}
	public enum LibFeature {
		IDrawNPCEffect,
		IComplexMineDamageTile_Hammer,
		WrappingTextSnippet,
		ExtraDyeSlots,
	}
}
