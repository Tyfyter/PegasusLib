using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Reflection;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PegasusLib.UI; 
public class KeybindSnippetHandler : AdvancedTextSnippetHandler<InputMode?> {
	static readonly ITagHandler glyphs = new GlyphTagHandler();
	public override IEnumerable<string> Names => ["key", "keybind"];
	public class Keybind_Snippet : WrappingTextSnippet {
		string lastAssignedKey = nameof(Keybind_Snippet);
		TextSnippet[] snippets = null;
		private readonly ModKeybind keybind;
		private readonly InputMode? mode;
		bool bound;
		public Keybind_Snippet(ModKeybind keybind, Color color = default, InputMode? mode = default) : base($"[key{(mode.HasValue?"/":"")}{mode}:{keybind.FullName()}]", color) {
			this.keybind = keybind;
			this.mode = mode;
			CheckForHover = true;
		}

		public override void Update() {
			InputMode inputMode = mode ?? PlayerInput.CurrentInputMode;
			switch (inputMode) {
				case InputMode.Mouse:
				inputMode = InputMode.Keyboard;
				break;
			}
			string assignedKey = keybind.GetAssignedKeys(inputMode).FirstOrDefault();
			if (lastAssignedKey != assignedKey) {
				lastAssignedKey = assignedKey;
				if (assignedKey is null) {
					bound = false;
					snippets = ChatManager.ParseMessage(Language.GetOrRegister("Mods.Origins.Generic.UnboundKey").Format(keybind.DisplayName.Value), Color).ToArray();
				} else {
					bound = true;
					snippets = FormatKeys(assignedKey);
				}
			}
			if (!KeybindHintItem.UpdateSelected(keybind) && IsHovered) {
				KeybindHintItem.UpdateHovered(keybind);
			}
		}
		TextSnippet[] FormatKeys(string keyname) {
			string[] parts = keyname.Split('+');
			List<TextSnippet> snippets = [];
			for (int i = 0; i < parts.Length; i++) {
				string key = parts[i];
				if (key.Length > 1 && key[0] == '-') snippets.Add(new("-", Color));
				else if (i > 0) snippets.Add(new("+", Color));
				snippets.Add(FormatKey(key));
			}
			return snippets.ToArray();
		}
		TextSnippet FormatKey(string key) {
			if (key.Length > 1 && key[0] == '-') key = key[1..];
			string glyph = GlyphTagHandler.GenerateTag(key);
			if (glyph != key) return glyphs.Parse(glyph[3..^1]);
			return new TextSnippet(key, Color);
		}
		public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1) {
			if (snippets is not null) {
				size = ChatManager.GetStringSize(FontAssets.MouseText.Value, snippets, new(scale), -1);
				if (justCheckingString || spriteBatch is null) return true;
				if (IsHovered || KeybindHintItem.IsSelected(keybind)) spriteBatch.DrawString(
					FontAssets.MouseText.Value,
					"_",
					position + Vector2.UnitY * 4,
					color,
					0,
					new(0, 0),
					new Vector2(size.X / FontAssets.MouseText.Value.MeasureString("_").X, 1),
					0,
				0);
				DrawSnippetArray(snippets, spriteBatch, position, color, scale, out int hovered, true);
				if (!TextUtils.DrawingShadows) {
					if (KeybindHintItem.Enabled && !KeybindHintItem.DrawingTooltip) IsHovered = hovered != -1;
					if (IsHovered) {
						Main.LocalPlayer.mouseInterface = true;
						if (bound) Main.instance.MouseText(keybind.DisplayName.Value);
					}
				}
				return true;
			}
			return base.UniqueDraw(justCheckingString, out size, spriteBatch, position, color, scale);
		}
	}
	public override IEnumerable<(string, UIElement)> GetSuggestions(string typed) {
		if (!typed.Contains(':') && typed.Contains('/')) return SuggestOptions(typed);
		return GetKeybindSuggestions(typed);
	}
	new IEnumerable<(string, UIElement)> SuggestOptions(string typed) {
		if (typed.Contains(':')) yield break;
		if (!typed.StartsWith('/')) typed = "/" + typed;
		InputMode[] modes = Enum.GetValues<InputMode>();
		for (int i = 0; i < modes.Length; i++) {
			string mode = modes[i].ToString();
			if (!mode.StartsWith(typed[1..], StringComparison.InvariantCultureIgnoreCase)) continue;
			yield return (typed + mode[(typed.Length - 1)..], CreateText(this.GetLocalization("Option_" + mode)));
		}
	}
	public static IEnumerable<(string, UIElement)> GetKeybindSuggestions(string typed) {
		if (typed.StartsWith(':')) typed = typed[1..];
		HashSet<ModKeybind> displayed = [];
		foreach (ModKeybind keybind in KeybindLoaderMethods.Keybinds) {
			string key = keybind.DisplayName.Value;
			if (!key.StartsWith(typed, StringComparison.CurrentCultureIgnoreCase)) continue;
			yield return (":" + keybind.FullName() + "]", new UIText(key));
			displayed.Add(keybind);
		}
		foreach (ModKeybind keybind in KeybindLoaderMethods.Keybinds) {
			if (displayed.Contains(keybind)) continue;
			string key = keybind.DisplayName.Value;
			if (!key.Contains(typed, StringComparison.CurrentCultureIgnoreCase)) continue;
			yield return (":" + keybind.FullName() + "]", new UIText(key));
		}
	}
	public override IEnumerable<SnippetOption> GetOptions() => Enum.GetValues<InputMode>().Select(mode => new SnippetOption(mode.ToString(), "", _ => options = mode));
	public override TextSnippet Parse(string text, Color baseColor, InputMode? options) {
		if (!KeybindLoaderMethods.TryGet(text, out ModKeybind keybind)) return new TextSnippet($"Invalid keybind {text}, must be formatted as Mod/Name");
		return new Keybind_Snippet(keybind, baseColor, options);
	}
}
class KeybindHintItem : GlobalItem {
	static Mod ControllerConfigurator;
	static ModKeybind searchKeybind;
	public static bool Enabled { get; private set; }
	public static bool DrawingTooltip { get; private set; } = false;

	public override bool IsLoadingEnabled(Mod mod) {
		return Enabled = ModLoader.TryGetMod(nameof(ControllerConfigurator), out ControllerConfigurator);
	}
	public override void SetStaticDefaults() {
		Enabled = KeybindLoaderMethods.TryGet($"{nameof(ControllerConfigurator)}/GoToKeybind", out searchKeybind);
	}
	static int lastType = ItemID.None;
	static int scrollIndex = 0;
	static readonly Dictionary<ModKeybind, int> indexByKey = [];

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
		DrawingTooltip = true;
		indexByKey.Clear();
	}
	public override void PostDrawTooltip(Item item, ReadOnlyCollection<DrawableTooltipLine> lines) {
		DrawingTooltip = false;
		if (item.type != lastType) {
			scrollIndex = 0;
			lastType = item.type;
		}
		if (indexByKey.Count <= 0) return;
		if (PlayerInput.ScrollWheelDelta >= 120) scrollIndex++;
		if (PlayerInput.ScrollWheelDelta <= -120) scrollIndex--;
		if (scrollIndex >= indexByKey.Count) scrollIndex = 0;
		else if (scrollIndex < 0) scrollIndex = indexByKey.Count - 1;
	}
	public static bool UpdateSelected(ModKeybind keybind) {
		if (!DrawingTooltip) return false;
		int keybindCount = indexByKey.Count;
		if (!Enabled || !indexByKey.TryAdd(keybind, keybindCount)) return true;
		if (keybindCount == scrollIndex && searchKeybind.JustPressed) ControllerConfigurator.Call("OPENKEYBINDSTOSEARCH", keybind);
		return true;
	}
	public static void UpdateHovered(ModKeybind keybind) {
		if (!Enabled) return;
		if ((Main.mouseLeft && Main.mouseLeftRelease) || searchKeybind.JustPressed) ControllerConfigurator.Call("OPENKEYBINDSTOSEARCH", keybind);
	}
	public static bool IsSelected(ModKeybind keybind) => Enabled && indexByKey.TryGetValue(keybind, out int selectionIndex) && selectionIndex == scrollIndex;
}