using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PegasusLib.Sets {
	public static class ShaderSets {
		[ForceInitialize]
		public static class Armor {
			public static ArmorShaderSet<bool> BasicColorDye { get; } = new(valueOverrides: (shader, value) => {
				if (value) {
					Type type = shader.GetType();
					if (type != typeof(ArmorShaderData) && type.GetMethod(nameof(ArmorShaderData.GetSecondaryShader), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) is not null) return false;
				}
				return value;
			}) {
				[(Main.pixelShader, "ArmorColored")] = true,
				[(Main.pixelShader, "ArmorColoredAndBlack")] = true,
				[(Main.pixelShader, "ArmorColoredAndSilverTrim")] = true
			};
			public class ArmorShaderSet<T>(T defaultValue = default, Func<ArmorShaderData, T, T> valueOverrides = null) : BakedSet<(Effect effect, string pass), T>(defaultValue) {
				public override void Setup() {
					FastFieldInfo<ArmorShaderDataSet, List<ArmorShaderData>> _shaderData = "_shaderData";
					FastFieldInfo<ShaderData, string> _passName = "_passName";
					List<ArmorShaderData> shaders = _shaderData.GetValue(GameShaders.Armor);
					ActualSet = new T[shaders.Count + 1];
					Array.Fill(ActualSet, DefaultValue);
					for (int i = 0; i < shaders.Count; i++) {
						ArmorShaderData shader = shaders[i];
						if (setupData.TryGetValue((shader.Shader, _passName.GetValue(shader)), out T value)) {
							ActualSet[i + 1] = value;
						}
					}
					if (valueOverrides is null) return;
					for (int i = 1; i < ActualSet.Length; i++) {
						ActualSet[i] = valueOverrides(shaders[i - 1], ActualSet[i]);
					}
				}
			}
		}
	}
	/// <summary>
	/// Should be used with <see cref="ForceInitializeAttribute"/>
	/// </summary>
	public abstract class BakedSet<TKey, TValue> : IBakedSet {
		protected Dictionary<TKey, TValue> setupData = [];
		protected TValue DefaultValue { get; private set; }
		protected TValue[] ActualSet { get; set; }
		public TValue this[int index] => ActualSet[index];
		public TValue this[TKey key] {
			set => setupData[key] = value;
		}
		public BakedSet(TValue defaultValue = default) {
			if (IBakedSet.tooLateToCreate) throw new InvalidOperationException("Cannot create a baked set after initialization");
			IBakedSet.toSetup.Add(this);
			DefaultValue = defaultValue;
		}
		public void Add(TKey key, TValue value) => setupData.Add(key, value);
		public void SetupSet() {
			Setup();
			setupData = null;
		}
		public abstract void Setup();
	}
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	sealed class ForceInitializeAttribute : Attribute { }
	internal interface IBakedSet {
		internal static bool tooLateToCreate = false;
		protected static List<IBakedSet> toSetup = [];
		public void SetupSet();
		class BakedSetSystem : ModSystem {
			public override void OnModLoad() {
				MonoModHooks.Add(
					typeof(ModContent).GetMethod("ResizeArrays", BindingFlags.NonPublic | BindingFlags.Static),
					(Action<bool> orig, bool unloading) => {
						orig(unloading);
						tooLateToCreate = !unloading;
					}
				);
				MonoModHooks.Add(
					typeof(SystemLoader).GetMethod("EnsureResizeArraysAttributeStaticCtorsRun", BindingFlags.NonPublic | BindingFlags.Static),
					(Action<Mod> orig, Mod mod) => {
						orig(mod);
						foreach (Type type in AssemblyManager.GetLoadableTypes(mod.Code)) {
							if (type.GetAttribute<ForceInitializeAttribute>() != null) RuntimeHelpers.RunClassConstructor(type.TypeHandle);
						}
					}
				);
			}
			public override void AddRecipes() {
				for (int i = 0; i < toSetup.Count; i++) toSetup[i].SetupSet();
			}
		}
	}
}
