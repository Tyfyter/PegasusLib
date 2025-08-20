using Microsoft.Xna.Framework;
using PegasusLib.Sets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PegasusLib.UI; 
public class Buff_Hint_Snippet : TextSnippet {
	public readonly int buffType;
	public readonly ModBuff buff;
	public bool DisplayHint { get; set; } = true;
	public Buff_Hint_Snippet(string text, int type, Color color = default) : base() {
		Text = text;
		Color = color;
		buffType = type;
		if (type <= 0) {
			Text = "Invalid Buff type";
			return;
		}
		Text ??= Lang.GetBuffName(buffType);
		bool debuff = Main.debuff[buffType];
		switch (buffType) {
			case BuffID.Frostburn2:
			case BuffID.OnFire3:
			case BuffID.Oiled:
			case BuffID.Daybreak:
			debuff = true;
			break;
		}
		Color = Color.Lerp(color, debuff ? Color.Red : Color.Lime, 0.5f);
		buff = BuffLoader.GetBuff(type);
		Buff_Hint_Handler.BuffHintModifiers[buffType].ModifyBuffSnippet?.Invoke(this, color);
	}
	public override void OnHover() {
		Main.LocalPlayer.mouseInterface = true;
		UICommon.TooltipMouseText(string.Join('\n', GetBuffText()));
	}
	public string[] GetBuffText() {
		if (buffType <= 0) return [];
		List<string> lines = [
			$"[sprite:{buff?.Texture ?? $"Terraria/Images/Buff_{buffType}"}] [c/{Color.Hex3()}:{Lang.GetBuffName(buffType)}]"
		];
		Buff_Hint_Handler.BuffHintModifiers[buffType].ModifyBuffTip?.Invoke(lines, BuffHintItem.currentItem);
		return lines.SelectMany(l => l.Split('\n')).ToArray();
	}
}
class BuffHintItem : GlobalItem {
	public static Item currentItem;
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
		if (PegasusConfig.Instance.showAdditionalDebuffsTooltip && ItemSets.InflictsDebuffs[item.type] is int[] extraBuffs) {

			int index = tooltips.FindLastIndex(line => line.Name.StartsWith("Tooltip"));
			TooltipLine line = new(ModContent.GetInstance<PegasusLib>(), "BuffsTooltip", 
				Language.GetOrRegister("Mods.PegasusLib.InflictsDebuffs")
				.Format(TextUtils.Format("Mods.PegasusLib.ListAll", extraBuffs.Select(Buff_Hint_Handler.GenerateTag)))
			);
			if (index == -1) {
				tooltips.Add(line);
				return;
			}
			tooltips.Insert(index + 1, line);
		}
	}
	public override void PostDrawTooltip(Item item, ReadOnlyCollection<DrawableTooltipLine> lines) {
		switch (PegasusConfig.Instance.buffHintListCondition) {
			case BuffListControl.Shift:
			if (!ItemSlot.ShiftInUse) return;
			break;
			case BuffListControl.Control:
			if (!ItemSlot.ControlInUse) return;
			break;
			case BuffListControl.Alt:
			if (!Main.keyState.IsKeyDown(Main.FavoriteKey)) return;
			break;
			case BuffListControl.Never:
			return;
		}
		currentItem = item;
		List<Buff_Hint_Snippet> buffs = [];
		for (int i = 0; i < lines.Count; i++) {
			List<TextSnippet> textSnippets = ChatManager.ParseMessage(lines[i].Text, lines[i].Color);
			buffs.AddRange(textSnippets.TryCast<Buff_Hint_Snippet>().Where(l => l.DisplayHint));
		}
		if (!PegasusConfig.Instance.showAdditionalDebuffsTooltip && ItemSets.InflictsDebuffs[item.type] is int[] extraBuffs) {
			buffs.AddRange(extraBuffs.Select(static buff => new Buff_Hint_Snippet(null, buff, Color.White)));
		}
		if (buffs.Count > 0) {
			Vector2 scale = new(PegasusConfig.Instance.buffHintListScale);
			const int marginX = 14;
			const int marginY = 9;
			Vector2 pos = default;
			bool left = false;
			switch (PegasusConfig.Instance.buffHintListPosition) {
				case BuffListPosition.Side:
				pos = new(lines[0].X + marginX * 2 + 1 + lines.Max(line => ChatManager.GetStringSize(FontAssets.MouseText.Value, line.Text, line.BaseScale).X), lines[0].Y);
				pos.Y -= marginY + 5;
				left = pos.X >= Main.screenWidth;
				if (left) {
					pos.X = lines[0].X - (marginX * 2 + 1);
				}
				break;

				case BuffListPosition.Below:
				pos = new(lines[^1].X, lines[^1].Y + ChatManager.GetStringSize(FontAssets.MouseText.Value, lines[^1].Text, lines[^1].BaseScale).Y);
				break;
			}
			float tooltipOpacity = Main.mouseTextColor / 255f;

			for (int i = 0; i < buffs.Count; i++) {
				Vector2 startPos = pos;
				string[] buffLines = buffs[i].GetBuffText();
				Vector2 size = Vector2.Zero;
				for (int j = 0; j < buffLines.Length; j++) {
					Vector2 stringSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, buffLines[j], Vector2.One) * scale;
					if (stringSize.X > size.X) {
						size.X = stringSize.X;
					}
					size.Y += stringSize.Y;
				}
				if (Main.SettingsEnabled_OpaqueBoxBehindTooltips) {
					pos.Y += marginY + 5;
					tooltipOpacity = MathHelper.Lerp(tooltipOpacity, 1f, 1f);
					Utils.DrawInvBG(
						Main.spriteBatch,
						new Rectangle((int)(pos.X - marginX - left.ToInt() * size.X), (int)pos.Y - marginY, (int)size.X + marginX * 2, (int)size.Y + marginY + marginY / 2),
						new Color(23, 25, 81, 255) * 0.925f
					);
					startPos.Y += marginY * 1.5f;
				}
				pos.Y += size.Y;
				float yOffset = 0;
				for (int j = 0; j < buffLines.Length; j++) {
					Vector2 lineSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, buffLines[j], Vector2.One) * scale;
					ChatManager.DrawColorCodedStringWithShadow(
						Main.spriteBatch,
						FontAssets.MouseText.Value,
						buffLines[j],
						startPos + new Vector2(left.ToInt() * -size.X, yOffset),
						Color.White * tooltipOpacity,
						0,
						new(0, -4),
						scale,
						size.X
					);
					yOffset += lineSize.Y;
				}
			}
		}
	}
}
[ReinitializeDuringResizeArrays]
public class Buff_Hint_Handler : ITagHandler {
	public static (Action<TextSnippet, Color> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)[] BuffHintModifiers = BuffID.Sets.Factory.CreateNamedSet(nameof(BuffHintModifiers))
	.RegisterCustomSet<(Action<TextSnippet, Color> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)>(default);
	public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
		if ((int.TryParse(text, out int buffType) && buffType < BuffLoader.BuffCount) || BuffID.Search.TryGetId(text, out buffType)) {
			text = null;
			SnippetHelper.ParseOptions(options,
				SnippetOption.CreateStringOption("dn", value => text = value)
			);
			return new Buff_Hint_Snippet(text, buffType, baseColor);
		}
		return new Buff_Hint_Snippet(text, -1, baseColor);
	}

	public static string GenerateTag(int buffID) => $"[bufftip:{buffID}]";
}
class DefaultHintModifiers : ModSystem {
	public override void PostSetupContent() {
		(Action<TextSnippet, Color> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip) ModifyTip(float dps, params string[] infoKeys) {
			return (null, (lines, _) => {
				if (dps > 0) lines.Add(Language.GetTextValue("Mods.PegasusLib.BuffTooltip.DOT", dps));
				for (int i = 0; i < infoKeys.Length; i++) {
					lines.Add(Language.GetTextValue("Mods.PegasusLib.BuffTooltip." + infoKeys[i]));
				}
			});
		}
		static void BuffDescription(int id) {
			Buff_Hint_Handler.BuffHintModifiers[id] = (Buff_Hint_Handler.BuffHintModifiers[id].ModifyBuffSnippet, Buff_Hint_Handler.BuffHintModifiers[id].ModifyBuffTip + ((lines, _) => {
				lines.Add(Lang.GetBuffDescription(id));
			}));
		}
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Poisoned] = ModifyTip(6);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.OnFire] = ModifyTip(4);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.OnFire3] = ModifyTip(15);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.ShadowFlame] = ModifyTip(15);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Venom] = ModifyTip(30);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.CursedInferno] = ModifyTip(24, nameof(BuffID.CursedInferno));
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Ichor] = ModifyTip(0, nameof(BuffID.Ichor));
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Frostburn] = ModifyTip(8);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Frostburn2] = ModifyTip(25);
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Oiled] = (null, (lines, _) => {
			lines[0] = lines[0].Replace($"[sprite:Terraria/Images/Buff_{BuffID.Oiled}] ", "");
			lines.Add(Language.GetTextValue("Mods.PegasusLib.BuffTooltip.Oiled"));
		});
		Buff_Hint_Handler.BuffHintModifiers[BuffID.Daybreak] = (null, (lines, item) => {
			lines[0] = lines[0].Replace($"[sprite:Terraria/Images/Buff_{BuffID.Daybreak}] ", "");
			if (item.type == ItemID.DayBreak) {
				lines.Add(Language.GetTextValue("Mods.PegasusLib.BuffTooltip.Daybreak"));
			} else {
				lines.Add(Language.GetTextValue("Mods.PegasusLib.BuffTooltip.DOT", 25));
			}
		});
		BuffDescription(BuffID.Confused);
	}
}
public enum BuffListPosition {
	Side,
	Below
}
public enum BuffListControl {
	Shift,
	Control,
	Alt,
	Always,
	Never
}
/* "But what if I want to use this feature when it's available without requiring PegasusLib?" you might ask
//Behold:
public class Fallback_Buff_Hint_Handler : ITagHandler, ILoadable {
	public static (Action<TextSnippet> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)[] BuffHintModifiers = BuffID.Sets.Factory.CreateNamedSet(nameof(BuffHintModifiers))
	.RegisterCustomSet<(Action<TextSnippet> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)>(default);
	public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
		if ((int.TryParse(text, out int buffType) && buffType < BuffLoader.BuffCount) || BuffID.Search.TryGetId(text, out buffType)) {
			text = Lang.GetBuffName(buffType);
			Regex regex = new("dn([^/]+)");
			string displayName = regex.Match(options).Groups[1].Value;
			if (!string.IsNullOrEmpty(displayName)) text = displayName;
			return new TextSnippet(text, baseColor);
		}
		return new TextSnippet(text, baseColor);
	}

	public static string GenerateTag(int buffID) => $"[bufftip:{buffID}]";

	public void Load(Mod mod) {
		if (ModLoader.HasMod("PegasusLib")) return;
		ChatManager.Register<Fallback_Buff_Hint_Handler>([
			"bufftip"
		]);
	}
	public void Unload() {}
}
*/