using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria;
using System.Text.RegularExpressions;
using Terraria.ModLoader;

namespace PegasusLib {
	public class ProgressFlag {
		ProgressFlag(string localizationKey) {
			condition = new(localizationKey, () => IsSet);
		}
		public readonly Condition condition;
		public bool IsSet { get; private set; }
		public void Set(bool value = true) {
			if (IsSet != value) {
				IsSet = value;
				NetMessage.SendData(MessageID.WorldData);
			}
		}
		public override string ToString() => $"[{condition.Description}: {IsSet}]";
		public static bool operator true(ProgressFlag flag) => flag.IsSet;
		public static bool operator false(ProgressFlag flag) => !flag.IsSet;
		public static implicit operator Condition(ProgressFlag flag) => flag.condition;
#pragma warning disable IDE0044 // Add readonly modifier
		static bool out0;
		static bool out1;
		static bool out2;
		static bool out3;
		static bool out4;
		static bool out5;
		static bool out6;
		static bool out7;
#pragma warning restore IDE0044 // Add readonly modifier
		public static void SetupFlags(Type type, out Action<TagCompound> saveData, out Action<TagCompound> loadData, out Action<BinaryWriter> netSend, out Action<BinaryReader> netReceive, out Action clearWorld) {
			List<(FieldInfo field, string name)> fields = [];
			Regex nameExtractorRegex = new("^<(@?\\w+)>k__BackingField$");
			string keyBase = $"Mods.{type.Namespace.Split('.')[0]}.Conditions.";
			foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Static)) {
				if (field.FieldType != typeof(ProgressFlag)) continue;
				Match match = nameExtractorRegex.Match(field.Name);
				if (!match.Success) continue;
				field.SetValue(null, new ProgressFlag(keyBase + match.Groups[1].Value));
				fields.Add((field, match.Groups[1].Value));
			}
			PropertyInfo isSet = typeof(ProgressFlag).GetProperty(nameof(IsSet));
			MethodInfo getFlag = isSet.GetMethod;
			MethodInfo setFlag = isSet.SetMethod;
			fields.Sort((a, b) => a.name.CompareTo(b.name));
			FieldInfo[] outFields = typeof(ProgressFlag).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(f => f.Name.StartsWith("out")).OrderBy(f => f.Name).ToArray();
			{
				DynamicMethod _saveData = new("saveData", typeof(void), [typeof(TagCompound)], true);
				DynamicMethod _loadData = new("loadData", typeof(void), [typeof(TagCompound)], true);
				ILGenerator _saveDataGen = _saveData.GetILGenerator();
				ILGenerator _loadDataGen = _loadData.GetILGenerator();
				MethodInfo tagCompound_Set = typeof(TagCompound).GetMethod(nameof(TagCompound.Set));
				MethodInfo tagCompound_TryGet = typeof(TagCompound).GetMethod(nameof(TagCompound.TryGet)).MakeGenericMethod(typeof(bool));
				foreach ((FieldInfo field, string name) in fields) {
					_saveDataGen.Emit(OpCodes.Ldarg_0);
					_saveDataGen.Emit(OpCodes.Ldstr, name);
					_saveDataGen.Emit(OpCodes.Ldsfld, field);
					_saveDataGen.Emit(OpCodes.Call, getFlag);
					_saveDataGen.Emit(OpCodes.Box, typeof(bool));
					_saveDataGen.Emit(OpCodes.Ldc_I4_0);
					_saveDataGen.Emit(OpCodes.Call, tagCompound_Set);

					_loadDataGen.Emit(OpCodes.Ldarg_0);
					_loadDataGen.Emit(OpCodes.Ldstr, name);
					_loadDataGen.Emit(OpCodes.Ldsflda, outFields[0]);
					_loadDataGen.Emit(OpCodes.Call, tagCompound_TryGet);
					_loadDataGen.Emit(OpCodes.Pop);

					_loadDataGen.Emit(OpCodes.Ldsfld, field);
					_loadDataGen.Emit(OpCodes.Ldsfld, outFields[0]);
					_loadDataGen.Emit(OpCodes.Call, setFlag);
				}
				_saveDataGen.Emit(OpCodes.Ret);
				_loadDataGen.Emit(OpCodes.Ret);
				saveData = _saveData.CreateDelegate<Action<TagCompound>>();
				loadData = _loadData.CreateDelegate<Action<TagCompound>>();
			}
			{
				DynamicMethod _netSend = new("netSend", typeof(void), [typeof(BinaryWriter)], true);
				DynamicMethod _netReceive = new("netReceive", typeof(void), [typeof(BinaryReader)], true);
				ILGenerator _netSendGen = _netSend.GetILGenerator();
				ILGenerator _netReceiveGen = _netReceive.GetILGenerator();
				MethodInfo writeFlags = typeof(BinaryIO).GetMethod(nameof(BinaryIO.WriteFlags));
				MethodInfo readFlags = typeof(BinaryIO).GetMethod(nameof(BinaryIO.ReadFlags), [typeof(BinaryReader), .. Enumerable.Repeat(typeof(bool).MakeByRefType(), 8)]);
				int targetCount = (int)(MathF.Ceiling(fields.Count / 8f) * 8);
				for (int i = 0; i < targetCount; i++) {
					if (i % 8 == 0) {
						_netSendGen.Emit(OpCodes.Ldarg_0);
						_netReceiveGen.Emit(OpCodes.Ldarg_0);
					}
					if (i < fields.Count) {
						FieldInfo field = fields[i].field;
						_netSendGen.Emit(OpCodes.Ldsfld, field);
						_netSendGen.Emit(OpCodes.Call, getFlag);
					} else {
						_netSendGen.Emit(OpCodes.Ldc_I4_0);
					}
					_netReceiveGen.Emit(OpCodes.Ldsflda, outFields[i % 8]);

					if ((i + 1) % 8 == 0) {
						_netSendGen.Emit(OpCodes.Call, writeFlags);
						_netReceiveGen.Emit(OpCodes.Call, readFlags);
						for (int j = i - 7; j <= i && j < fields.Count; j++) {
							_netReceiveGen.Emit(OpCodes.Ldsfld, fields[j].field);
							_netReceiveGen.Emit(OpCodes.Ldsfld, outFields[j % 8]);
							_netReceiveGen.Emit(OpCodes.Call, setFlag);
						}
					}
				}
				_netSendGen.Emit(OpCodes.Ret);
				_netReceiveGen.Emit(OpCodes.Ret);
				netSend = _netSend.CreateDelegate<Action<BinaryWriter>>();
				netReceive = _netReceive.CreateDelegate<Action<BinaryReader>>();
			}
			{
				DynamicMethod _clearWorld = new("clearWorld", typeof(void), [], true);
				ILGenerator _clearWorldGen = _clearWorld.GetILGenerator();
				foreach ((FieldInfo field, _) in fields) {
					_clearWorldGen.Emit(OpCodes.Ldsfld, field);
					_clearWorldGen.Emit(OpCodes.Ldc_I4_0);
					_clearWorldGen.Emit(OpCodes.Call, setFlag);
				}
				_clearWorldGen.Emit(OpCodes.Ret);
				clearWorld = _clearWorld.CreateDelegate<Action>();
			}
		}
	}
	public abstract class ProgressFlagSystem : ModSystem {
		public override void Load() {
			ProgressFlag.SetupFlags(GetType(),
				out saveData,
				out loadData,
				out netSend,
				out netReceive,
				out clearWorld
			);
		}
		public override void SaveWorldData(TagCompound tag) => saveData(tag);
		public override void LoadWorldData(TagCompound tag) => loadData(tag);
		public override void NetSend(BinaryWriter writer) => netSend(writer);
		public override void NetReceive(BinaryReader reader) => netReceive(reader);
		public override void ClearWorld() => clearWorld();
		static Action<TagCompound> saveData;
		static Action<TagCompound> loadData;
		static Action<BinaryWriter> netSend;
		static Action<BinaryReader> netReceive;
		static Action clearWorld;
	}
}
