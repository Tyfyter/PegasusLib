using System.Collections.Generic;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ObjectData;
using System;
using Terraria.ModLoader.Core;
using MonoMod.Core.Platforms;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Terraria.UI;

namespace PegasusLib {
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class PegasusLib : Mod {
		internal static List<IUnloadable> unloadables = [];
		public override void Load() {
			On_Main.DrawNPCDirect += IDrawNPCEffect.On_Main_DrawNPCDirect;
			On_Main.DrawProj_Inner += IDrawProjectileEffect.On_Main_DrawProj_Inner;
			On_Main.DrawItem += IDrawItemInWorldEffect.On_Main_DrawItem;
			On_ItemSlot.DrawItemIcon += IDrawItemInInventoryEffect.On_ItemSlot_DrawItemIcon;
		}
		public override void Unload() {
			foreach (IUnloadable unloadable in unloadables) {
				unloadable.Unload();
			}
			unloadables = null;
			DropRuleExt.Unload();
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
}
