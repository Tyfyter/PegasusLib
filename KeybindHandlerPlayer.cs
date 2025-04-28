using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PegasusLib {

	public class KeybindHandlerPlayer : ModPlayer {
		public sealed override void Load() {
			foreach (FieldInfo item in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(f => f.Name, StringComparer.InvariantCulture)) {
				if (item.GetCustomAttribute<KeybindAttribute>() is not KeybindAttribute attribute) continue;
				item.SetValue(this, new AutoKeybind(Mod, attribute.Name, attribute.DefaultBinding));
			}
			
		}
		public virtual void OnLoad() { }
		protected override bool CloneNewInstances => true;
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
		protected sealed class KeybindAttribute(string name, string defaultBinding = "None") : Attribute {
			public KeybindAttribute(string name, Keys key) : this(name, key.ToString()) { }
			public string Name { get; } = name;
			public string DefaultBinding { get; } = defaultBinding;
		}
	}
	public struct AutoKeybind {
		public readonly ModKeybind keybind;
		bool current;
		bool old;
		internal AutoKeybind(Mod mod, string name, string defaultBinding) {
			keybind = KeybindLoader.RegisterKeybind(mod, name, defaultBinding);
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
	}
}
