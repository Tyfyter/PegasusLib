using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PegasusLib {
	public abstract class KeybindHandlerPlayer : ModPlayer {
		Action<KeybindHandlerPlayer> updateOldKeys;
		Action<KeybindHandlerPlayer> updateCurrentKeys;
		internal static Dictionary<string, int> playerIDsByName = [];

		List<FieldInfo> netKeys = [];
		public sealed override void Load() {
			List<FieldInfo> keys = [];
			foreach (FieldInfo item in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(f => f.Name, StringComparer.InvariantCulture)) {
				if (item.GetCustomAttribute<KeybindAttribute>() is not KeybindAttribute attribute) continue;
				item.SetValue(this, new AutoKeybind(Mod, attribute.Name ?? item.Name, attribute.DefaultBinding));
				keys.Add(item);
				if (attribute.NeedsSync) netKeys.Add(item);
			}
			{
				DynamicMethod updateCurrentKeys = new("updateCurrentKeys", typeof(void), [typeof(KeybindHandlerPlayer)], true);
				ILGenerator gen = updateCurrentKeys.GetILGenerator();
				LocalBuilder self = gen.DeclareLocal(GetType());
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Unbox_Any, GetType());
				gen.Emit(OpCodes.Stloc, self);
				MethodInfo update = typeof(AutoKeybind).GetMethod(nameof(AutoKeybind.UpdateCurrent), BindingFlags.NonPublic | BindingFlags.Instance);
				for (int i = 0; i < keys.Count; i++) {
					gen.Emit(OpCodes.Ldloc_0, self);
					gen.Emit(OpCodes.Ldflda, keys[i]);
					gen.Emit(OpCodes.Call, update);
				}
				gen.Emit(OpCodes.Ret);
				this.updateCurrentKeys = updateCurrentKeys.CreateDelegate<Action<KeybindHandlerPlayer>>();
			}
			{
				DynamicMethod updateOldKeys = new("updateOldKeys", typeof(void), [typeof(KeybindHandlerPlayer)], true);
				ILGenerator gen = updateOldKeys.GetILGenerator();
				LocalBuilder self = gen.DeclareLocal(GetType());
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Unbox_Any, GetType());
				gen.Emit(OpCodes.Stloc, self);
				MethodInfo update = typeof(AutoKeybind).GetMethod(nameof(AutoKeybind.UpdateOld), BindingFlags.NonPublic | BindingFlags.Instance);
				for (int i = 0; i < keys.Count; i++) {
					gen.Emit(OpCodes.Ldloc_0, self);
					gen.Emit(OpCodes.Ldflda, keys[i]);
					gen.Emit(OpCodes.Call, update);
				}
				gen.Emit(OpCodes.Ret);
				this.updateOldKeys = updateOldKeys.CreateDelegate<Action<KeybindHandlerPlayer>>();
			}
			netBits = new(netKeys.Count);
		}
		public override void SetStaticDefaults() {
			playerIDsByName[FullName] = Index;
		}
		internal BitArray netBits;
		public override ModPlayer Clone(Player newEntity) {
			KeybindHandlerPlayer clone = (KeybindHandlerPlayer)base.Clone(newEntity);
			clone.netBits = new(netBits);
			return clone;
		}
		public sealed override void CopyClientState(ModPlayer targetCopy) {
			KeybindHandlerPlayer clone = (KeybindHandlerPlayer)targetCopy;
			clone.netBits = new(netBits);
		}
		public sealed override void SendClientChanges(ModPlayer clientPlayer) {
			KeybindHandlerPlayer clone = (KeybindHandlerPlayer)clientPlayer;
			if (new BitArray(netBits).Xor(clone.netBits).HasAnySet()) {
				SendSync();
			}
		}
		internal void SendSync(int ignoreClient = -1) {
			ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
			packet.Write((byte)PegasusLib.Packets.SyncKeybindHandler);
			packet.Write((byte)Player.whoAmI);
			packet.Write(FullName);
			packet.Write((byte)netBits.Count);
			Utils.SendBitArray(netBits, packet);
			packet.Send(ignoreClient: ignoreClient);
		}
		void SetNetKeys() {
			for (int i = 0; i < netKeys.Count; i++) {
				AutoKeybind binding = (AutoKeybind)netKeys[i].GetValue(this);
				binding.Current = netBits[i];
				netKeys[i].SetValue(this, binding);
			}
		}
		public virtual void OnLoad() { }
		protected override bool CloneNewInstances => true;
		protected string NetBitDebugIndicators() {
			StringBuilder builder = new();
			for (int i = 0; i < netKeys.Count; i++) {
				builder.Append($"[c/{(netBits[i] ? "0000FF" : "FF0000")}:|]");
			}
			return builder.ToString();
		}
		public sealed override void ProcessTriggers(TriggersSet triggersSet) {
			updateCurrentKeys(this);
			PostProcessTriggers(triggersSet);
			if (Main.netMode != NetmodeID.SinglePlayer && Player.whoAmI == Main.myPlayer) {
				for (int i = 0; i < netKeys.Count; i++) {
					netBits[i] = ((AutoKeybind)netKeys[i].GetValue(this)).Current;
				}
			}
		}
		public sealed override void PreUpdate() {
			updateOldKeys(this);
			if (Player.whoAmI != Main.myPlayer) SetNetKeys();
		}
		public virtual void PostProcessTriggers(TriggersSet triggersSet) { }
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
		protected class KeybindAttribute(string name, string defaultBinding = "None", bool needsSync = true) : Attribute {
			public KeybindAttribute(string defaultBinding = "None", bool needsSync = true) : this(null, defaultBinding, needsSync) { }
			public KeybindAttribute(string name, Keys key, bool needsSync = true) : this(name, key.ToString(), needsSync) { }
			public KeybindAttribute(Keys key, bool needsSync = true) : this(null, key.ToString(), needsSync) { }
			public string Name { get; } = name;
			public string DefaultBinding { get; } = defaultBinding;
			public bool NeedsSync { get; } = needsSync;
		}
	}
	public struct AutoKeybind {
		public readonly ModKeybind keybind;
		bool current;
		bool old;
		internal AutoKeybind(Mod mod, string name, string defaultBinding) : this(KeybindLoader.RegisterKeybind(mod, name, defaultBinding)) { }
		internal AutoKeybind(ModKeybind keybind) {
			this.keybind = keybind;
		}
		/// <summary>
		/// Returns true if this keybind is pressed currently. Useful for creating a behavior that relies on the keybind being held down.
		/// </summary>
		public bool Current {
			readonly get => current;
			set => current = value;
		}

		/// <summary>
		/// Returns true if this keybind has just been pressed this update. This is a fire-once-per-press behavior.
		/// </summary>
		public readonly bool JustPressed => current && !old;

		/// <summary>
		/// Returns true if this keybind has just been released this update.
		/// </summary>
		public readonly bool JustReleased => !current && old;

		/// <summary>
		/// Returns true if this keybind has been pressed during the previous update.
		/// </summary>
		public bool Old {
			readonly get => old;
			set => old = value;
		}
		internal void UpdateOld() {
			old = current;
		}
		internal void UpdateCurrent() {
			current = keybind.Current;
		}
	}
}
