using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PegasusLib.Sets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
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
	public readonly Options options;
	public bool DisplayHint { get; set; } = true;
	public Buff_Hint_Snippet(string text, int type, Color color = default, Options options = default) : base() {
		Text = text;
		Color = color;
		buffType = type;
		this.options = options;
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
			case BuffID.Midas:
			case BuffID.DryadsWardDebuff:
			debuff = true;
			break;
		}
		if (options.Hide) DisplayHint = false;
		if (PegasusConfig.Instance.invertOnHitBuffHintColor && !options.OnSelf) debuff = !debuff;
		if (PegasusConfig.Instance.colorBuffHints) Color = Color.Lerp(color, debuff ? Color.Red : Color.Lime, 0.5f);
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
			$"{GenerateBuffIcon(buffType)} [c/{Color.Hex3()}:{Lang.GetBuffName(buffType)}]"
		];
		Buff_Hint_Handler.BuffHintModifiers[buffType].ModifyBuffTip?.Invoke(lines, BuffHintItem.currentItem, options.OnSelf);
		return lines.SelectMany(l => l.Split('\n')).ToArray();
	}
	public static string GenerateBuffIcon(int buffType) => $"[sprite:{BuffLoader.GetBuff(buffType)?.Texture ?? $"Terraria/Images/Buff_{buffType}"}]";
	public record struct Options(bool OnSelf = false, bool Hide = false);
}
class BuffHintItem : GlobalItem {
	public static Item currentItem;
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
		if (PegasusConfig.Instance.showAdditionalDebuffsTooltip && ItemSets.InflictsExtraDebuffs[item.type] is int[] extraBuffs) {

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
		HashSet<(int type, string text)> mentionedBuffs = [];
		for (int i = 0; i < lines.Count; i++) {
			List<TextSnippet> textSnippets = ChatManager.ParseMessage(lines[i].Text, lines[i].Color);
			buffs.AddRange(textSnippets.TryCast<Buff_Hint_Snippet>().Where(l => l.DisplayHint && mentionedBuffs.Add((l.buffType, string.Concat(l.GetBuffText().Skip(1))))));
		}
		if (!PegasusConfig.Instance.showAdditionalDebuffsTooltip && ItemSets.InflictsExtraDebuffs[item.type] is int[] extraBuffs) {
			buffs.AddRange(extraBuffs.Select(static buff => new Buff_Hint_Snippet(null, buff, Color.White)));
		}
		if (buffs.Count > 0) {
			Vector2 scale = new(PegasusConfig.Instance.buffHintListScale);
			const int marginX = 14;
			const int marginY = 9;
			Vector2 pos = default;
			switch (PegasusConfig.Instance.buffHintListPosition) {
				case BuffListPosition.Side:
				pos = new(lines[0].X + marginX * 2 + 1 + lines.Max(line => ChatManager.GetStringSize(FontAssets.MouseText.Value, line.Text, line.BaseScale).X), lines[0].Y);
				pos.Y -= marginY + 5;
				break;

				case BuffListPosition.Below:
				pos = new(lines[^1].X, lines[^1].Y + ChatManager.GetStringSize(FontAssets.MouseText.Value, lines[^1].Text, lines[^1].BaseScale).Y);
				break;
			}
			float tooltipOpacity = Main.mouseTextColor / 255f;
			DrawableBuffHint[] boxen = new DrawableBuffHint[buffs.Count];
			bool doBGBox = Main.SettingsEnabled_OpaqueBoxBehindTooltips;

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
				Rectangle checkBox;
				if (doBGBox) {
					pos.Y += marginY + 5;
					checkBox = new Rectangle((int)(pos.X - marginX), (int)pos.Y - marginY, (int)size.X + marginX * 2, (int)size.Y + marginY + marginY / 2);
					startPos.Y += marginY * 1.5f;
				} else {
					checkBox = new Rectangle((int)(pos.X), (int)pos.Y, (int)size.X, (int)size.Y);
				}
				boxen[i] = new(startPos, size, buffLines, checkBox);
				pos.Y += size.Y;
			}
			if (boxen[^1].Box.Bottom > Main.screenHeight) {
				int diff = 0;
				switch (PegasusConfig.Instance.buffHintListPosition) {
					case BuffListPosition.Below:
					diff = boxen[^1].Box.Bottom - lines[0].Y;
					diff += marginY + 1;
					break;

					case BuffListPosition.Side:
					diff = boxen[^1].Box.Bottom - Main.screenHeight;
					break;
				}
				for (int i = 0; i < boxen.Length; i++) {
					boxen[i].TextPos = boxen[i].TextPos - diff * Vector2.UnitY;
					boxen[i].Box = boxen[i].Box with { Y = boxen[i].Box.Y - diff };
				}
			}
			switch (PegasusConfig.Instance.buffHintListPosition) {
				case BuffListPosition.Side: {
					bool overflowing = false;
					for (int i = 0; i < boxen.Length && !overflowing; i++) {
						overflowing = boxen[i].Box.Right > Main.screenWidth;
					}
					if (overflowing) {
						for (int i = 0; i < boxen.Length; i++) {
							int diff = (boxen[i].Box.Right - lines[0].X) + 1;
							diff += marginX;
							boxen[i].TextPos = boxen[i].TextPos - diff * Vector2.UnitX;
							boxen[i].Box = boxen[i].Box with { X = boxen[i].Box.X - diff };
						}
					}
					break;
				}
			}

			for (int i = 0; i < boxen.Length; i++) {
				(Vector2 startPos, Vector2 size, string[] buffLines, Rectangle box) = boxen[i];
				if (doBGBox) {
					tooltipOpacity = MathHelper.Lerp(tooltipOpacity, 1f, 1f);
					Utils.DrawInvBG(
						Main.spriteBatch,
						box,
						new Color(23, 25, 81, 255) * 0.925f
					);
				}
				float yOffset = 0;
				for (int j = 0; j < buffLines.Length; j++) {
					Vector2 lineSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, buffLines[j], Vector2.One) * scale;
					ChatManager.DrawColorCodedStringWithShadow(
						Main.spriteBatch,
						FontAssets.MouseText.Value,
						buffLines[j],
						startPos + yOffset * Vector2.UnitY,
						Color.White * tooltipOpacity,
						0,
						new(0, -4),
						scale,
						size.X + 1
					);
					yOffset += lineSize.Y;
				}
			}
		}
	}

	public record struct DrawableBuffHint(Vector2 TextPos, Vector2 TextSize, string[] Lines, Rectangle Box);
}
[ReinitializeDuringResizeArrays]
public class Buff_Hint_Handler : ITagHandler {
	public static (Action<TextSnippet, Color> ModifyBuffSnippet, Action<List<string>, Item, bool> ModifyBuffTip)[] BuffHintModifiers = BuffID.Sets.Factory.CreateNamedSet(nameof(BuffHintModifiers))
	.RegisterCustomSet<(Action<TextSnippet, Color> ModifyBuffSnippet, Action<List<string>, Item, bool> ModifyBuffTip)>(default);
	public static void CombineBuffHintModifiers(int buffType, Action<TextSnippet, Color> modifyBuffSnippet = null, Action<List<string>, Item, bool> modifyBuffTip = null) {
		BuffHintModifiers[buffType] = (BuffHintModifiers[buffType].ModifyBuffSnippet + modifyBuffSnippet, BuffHintModifiers[buffType].ModifyBuffTip + modifyBuffTip);
	}
	public static void ModifyTip(int id, float dps, params string[] infoKeys) {
		const string loc = "Mods.PegasusLib.BuffTooltip.";
		for (int i = 0; i < infoKeys.Length; i++) Language.GetOrRegister(infoKeys[i]);
		CombineBuffHintModifiers(id, modifyBuffTip: (lines, _, _) => {
			if (dps > 0) lines.Add(Language.GetTextValue(loc + "DOT", dps));
			for (int i = 0; i < infoKeys.Length; i++) {
				lines.Add(Language.GetTextValue(infoKeys[i]));
			}
		});
	}
	public static void RemoveIcon(int id) {
		CombineBuffHintModifiers(id, modifyBuffTip: (lines, _, _) => {
			lines[0] = lines[0].Replace($"{Buff_Hint_Snippet.GenerateBuffIcon(id)} ", "");
		});
	}
	public static void BuffDescription(int id, bool onlyPlayer = false) {
		CombineBuffHintModifiers(id, modifyBuffTip:(lines, _, player) => {
			if (!onlyPlayer || player) lines.Add(Lang.GetBuffDescription(id));
		});
	}
	public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
		if ((int.TryParse(text, out int buffType) && buffType < BuffLoader.BuffCount) || BuffID.Search.TryGetId(text, out buffType)) {
			text = null;
			Buff_Hint_Snippet.Options snoptions = new(false);
			SnippetHelper.ParseOptions(options,
				SnippetOption.CreateStringOption("dn", value => text = value, '|'),
				SnippetOption.CreateFlagOption("self", () => snoptions.OnSelf = true),
				SnippetOption.CreateFlagOption("hide", () => snoptions.Hide = true)
			);
			return new Buff_Hint_Snippet(text, buffType, baseColor, snoptions);
		}
		return new Buff_Hint_Snippet(text, -1, baseColor);
	}
	public static string GenerateTag(int buffID) => $"[buffhint:{(BuffID.Search.TryGetName(buffID, out string name) ? name : buffID)}]";
	public static string GenerateTag<TBuff>() where TBuff : ModBuff => GenerateTag(ModContent.BuffType<TBuff>());
}
class DefaultHintModifiers : ModSystem {
	public override void PostSetupContent() {
		const string loc = "Mods.PegasusLib.BuffTooltip.";
		Buff_Hint_Handler.ModifyTip(BuffID.Poisoned, 6);
		Buff_Hint_Handler.ModifyTip(BuffID.OnFire, 4);
		Buff_Hint_Handler.ModifyTip(BuffID.OnFire3, 15);
		Buff_Hint_Handler.ModifyTip(BuffID.ShadowFlame, 15);
		Buff_Hint_Handler.ModifyTip(BuffID.Venom, 30);
		Buff_Hint_Handler.ModifyTip(BuffID.CursedInferno, 24, loc + nameof(BuffID.CursedInferno));
		Buff_Hint_Handler.ModifyTip(BuffID.Ichor, 0, loc + nameof(BuffID.Ichor));
		Buff_Hint_Handler.ModifyTip(BuffID.Midas, 0, loc + nameof(BuffID.Midas));
		Buff_Hint_Handler.ModifyTip(BuffID.Frostburn, 8);
		Buff_Hint_Handler.ModifyTip(BuffID.Frostburn2, 25);
		Buff_Hint_Handler.ModifyTip(BuffID.Suffocation, 20);
		Buff_Hint_Handler.ModifyTip(BuffID.Oiled, 0, loc + nameof(BuffID.Oiled));
		Buff_Hint_Handler.RemoveIcon(BuffID.Oiled);
		Buff_Hint_Handler.ModifyTip(BuffID.Burning, 30, "BuffDescription.Slow");
		Buff_Hint_Handler.CombineBuffHintModifiers(BuffID.Daybreak, modifyBuffTip: (lines, item, _) => {
			if (item.type == ItemID.DayBreak) {
				lines.Add(Language.GetTextValue(loc + "Daybreak"));
			} else {
				lines.Add(Language.GetTextValue(loc + "DOT", 25));
			}
		});
		Buff_Hint_Handler.RemoveIcon(BuffID.Daybreak);
		Buff_Hint_Handler.BuffDescription(BuffID.Confused);
		Buff_Hint_Handler.BuffDescription(BuffID.Bleeding);
		Buff_Hint_Handler.BuffDescription(BuffID.Silenced, true);
		Buff_Hint_Handler.BuffDescription(BuffID.Cursed, true);
		Buff_Hint_Handler.BuffDescription(BuffID.Darkness, true);
		Buff_Hint_Handler.BuffDescription(BuffID.Blackout, true);
		Buff_Hint_Handler.BuffDescription(BuffID.Obstructed, true);
		Buff_Hint_Handler.BuffDescription(BuffID.BrokenArmor, true);
		Buff_Hint_Handler.ModifyTip(BuffID.WitheredArmor, 0, $"BuffDescription.{nameof(BuffID.BrokenArmor)}");
		Buff_Hint_Handler.ModifyTip(BuffID.WitheredWeapon, 0, loc + nameof(BuffID.WitheredWeapon));
		Buff_Hint_Handler.BuffDescription(BuffID.Rabies, true);
		Buff_Hint_Handler.BuffDescription(BuffID.MoonLeech, true);
		Buff_Hint_Handler.BuffDescription(BuffID.PotionSickness, true);
		Buff_Hint_Handler.BuffDescription(BuffID.Tipsy, true);
		Buff_Hint_Handler.BuffDescription(BuffID.ChaosState, true);
		Buff_Hint_Handler.BuffDescription(BuffID.WaterCandle, true);
		Buff_Hint_Handler.BuffDescription(BuffID.ShadowCandle, true);
		Buff_Hint_Handler.BuffDescription(BuffID.BrainOfConfusionBuff, true);
		Buff_Hint_Handler.ModifyTip(BuffID.Slow, 0, "BuffDescription.Slow");
		Buff_Hint_Handler.ModifyTip(BuffID.Chilled, 0, "BuffDescription.Slow");
		Buff_Hint_Handler.ModifyTip(BuffID.Weak, 0, loc + nameof(BuffID.Weak));
		Buff_Hint_Handler.BuffDescription(BuffID.OgreSpit);
		Buff_Hint_Handler.ModifyTip(BuffID.Frozen, 0, loc + nameof(BuffID.Frozen));
		Buff_Hint_Handler.ModifyTip(BuffID.Webbed, 0, loc + nameof(BuffID.Webbed));
		Buff_Hint_Handler.ModifyTip(BuffID.Stoned, 0, loc + nameof(BuffID.Stoned));
		Buff_Hint_Handler.ModifyTip(BuffID.Shimmer, 0, loc + nameof(BuffID.Shimmer));
		Buff_Hint_Handler.ModifyTip(BuffID.DryadsWardDebuff, 0, loc + nameof(BuffID.DryadsWardDebuff));
		Buff_Hint_Handler.CombineBuffHintModifiers(BuffID.DryadsWardDebuff, modifyBuffTip: (lines, item, _) => {
			if (DryadWardDamage.Get() is float value) {
				lines.Add(Language.GetTextValue(loc + "DOT", -value));
			}
		});
	}
	public static int dryadWardNum = 0;
}
public class DryadWardDamage : ILoadable {
	public static float? Get() {
		if (doApplyDOT is null) return null;
		fakeForDryadsWard.lifeRegen = 0;
		doApplyDOT();
		return result;
	}
	static NPC fakeForDryadsWard;
	static Action doApplyDOT;
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable CS0649 // Field is never assigned to
	static int result;
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CS0649 // Field is never assigned to
	public void Load(Mod mod) {
		bool failedSetup = false;
		IL_NPC.UpdateNPC_BuffApplyDOTs += (il) => {
			ILCursor c = new(il);
			if (c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(NPCLoader), nameof(NPCLoader.UpdateLifeRegen)))) {
				c.EmitLdarg0();
				c.EmitLdfld(typeof(NPC).GetField(nameof(NPC.lifeRegen)));
				c.EmitStsfld(typeof(DryadWardDamage).GetField(nameof(result), BindingFlags.NonPublic | BindingFlags.Static));
			} else {
				failedSetup = true;
			}
		};
		if (failedSetup) return;
		fakeForDryadsWard = new() {
			dryadBane = true,
			immortal = true
		};
		doApplyDOT = typeof(NPC).GetMethod("UpdateNPC_BuffApplyDOTs", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).CreateDelegate<Action>(fakeForDryadsWard);
	}
	public void Unload() {
		fakeForDryadsWard = null;
		doApplyDOT = null;
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
[ReinitializeDuringResizeArrays]
public class Fallback_Buff_Hint_Handler : ITagHandler, ILoadable {
   public static (Action<TextSnippet> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)[] BuffHintModifiers = BuffID.Sets.Factory.CreateNamedSet("PegasusLib", nameof(BuffHintModifiers))
   .RegisterCustomSet<(Action<TextSnippet> ModifyBuffSnippet, Action<List<string>, Item> ModifyBuffTip)>(default);
   public TextSnippet Parse(string text, Color baseColor = default, string options = null) {
	   if ((int.TryParse(text, out int buffType) && buffType < BuffLoader.BuffCount) || BuffID.Search.TryGetId(text, out buffType)) {
		   text = Lang.GetBuffName(buffType);
		   Regex regex = new("dn([^\\/\\|]+)");
		   string displayName = regex.Match(options).Groups[1].Value;
		   if (!string.IsNullOrEmpty(displayName)) text = displayName;
		   return new TextSnippet(text, baseColor);
	   }
	   return new TextSnippet(text, baseColor);
   }

   public static string GenerateTag(int buffID) => $"[buffhint:{(BuffID.Search.TryGetName(buffID, out string name) ? name : buffID)}]";
   public static string GenerateTag<TBuff>() where TBuff : ModBuff => GenerateTag(ModContent.BuffType<TBuff>());

   public void Load(Mod mod) {
	   if (ModLoader.HasMod("PegasusLib")) return;
	   ChatManager.Register<Fallback_Buff_Hint_Handler>([
		   "buffhint",
		   "bufftip"
	   ]);
   }
   public void Unload() {}
}
*/