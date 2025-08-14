using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;

namespace PegasusLib {
	internal class ExtraDyeSlots : ILoadable {
		static AutoLoadingAsset<Texture2D> buttonTexture = "PegasusLib/Textures/Extra_Dye_Menu";
		static FastFieldInfo<AccessorySlotLoader, int> slotDrawLoopCounter = "slotDrawLoopCounter";
		internal static FastFieldInfo<ModAccessorySlotPlayer, Item[]> exAccessorySlot = "exAccessorySlot";
		internal static FastFieldInfo<ModAccessorySlotPlayer, Item[]> exDyesAccessory = "exDyesAccessory";
		internal static FastFieldInfo<ModAccessorySlotPlayer, bool[]> exHideAccessory = "exHideAccessory";
		internal static List<ExtraDyeSlot> extraDyeSlots = [];
		internal static bool drawingExtraDyeSlots = false;
		internal static bool drewAnyExtraDyeSlots = false;
		static bool? enabled;
		static bool Enabled => enabled ??= PegasusLib.requiredFeatures.ContainsKey(LibFeature.ExtraDyeSlots);
		public void Load(Mod mod) {
			try {
				IL_Main.DrawInventory += IL_Main_DrawInventory;
				MonoModHooks.Add(typeof(AccessorySlotLoader).GetMethod("DrawSlot", BindingFlags.NonPublic | BindingFlags.Instance), On_DrawSlot);
				On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += On_ItemSlot_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color;
				On_Main.DrawPageIcons += On_Main_DrawPageIcons;
			} catch (Exception exception) {
				PegasusLib.FeatureError(LibFeature.ExtraDyeSlots, exception);
#if DEBUG
				throw;
#endif
			}
		}

		private int On_Main_DrawPageIcons(On_Main.orig_DrawPageIcons orig, int yPos) {
			int ret = orig(yPos);
			if (!Enabled) return ret;
			if (!drewAnyExtraDyeSlots || PegasusConfig.Instance.alwaysShowDyeSlotList) {
				bool shouldDraw = false;
				for (int i = 0; i < extraDyeSlots.Count && !shouldDraw; i++) {
					shouldDraw = extraDyeSlots[i].Item?.IsAir == false;
				}
				if (shouldDraw) {
					Rectangle frame = buttonTexture.Value.Frame(2);
					Rectangle rect = new(Main.screenWidth - 162 - 48 - 6, yPos + 4, frame.Width, frame.Height);
					if (rect.Contains(Main.mouseX, Main.mouseY)) {
						frame.X += frame.Width;
						Main.LocalPlayer.mouseInterface = true;
						if (Main.mouseLeftRelease && Main.mouseLeft) {
							ExtraDyeSlotSystem.SetUI(new ExtraDyeSlotUI(
								rect.X, rect.Y,
								extraDyeSlots.ToArray()
							));
						}
					}
					Main.spriteBatch.Draw(buttonTexture, rect, frame, Color.White);
				}
			}
			drewAnyExtraDyeSlots = false;
			return ret;
		}
		static Rectangle GetPosition(Vector2 position) {
			Vector2 size = buttonTexture.Value.Frame(2).Size();
			position += new Vector2(39, 5);

			return new((int)(position.X - size.X * 0.5f), (int)(position.Y - size.Y * 0.5f), (int)size.X, (int)size.Y);
		}
		static void On_ItemSlot_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color(On_ItemSlot.orig_Draw_SpriteBatch_ItemArray_int_int_Vector2_Color orig, SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color lightColor) {
			orig(spriteBatch, inv, context, slot, position, lightColor);
			if (!Enabled) return;
			if (drawingExtraDyeSlots) return;
			if (showButton && context is ItemSlot.Context.EquipDye or ItemSlot.Context.ModdedDyeSlot) {
				spriteBatch.Draw(buttonTexture, GetPosition(position), buttonTexture.Value.Frame(2), Color.White * (hovered ? 1f : 0.7f));
				drewAnyExtraDyeSlots = true;
				if (hovered && Main.mouseLeftRelease && Main.mouseLeft) {
					(int vanityOffset, Item[] equippedItems, bool[] equipHidden) = GetEquippedItems(inv == Main.LocalPlayer.dye);
					ExtraDyeSlotSystem.SetUI(new ExtraDyeSlotUI(
						(int)position.X, (int)position.Y,
						extraDyeSlots.Where(s => s.UseForSlot(equippedItems[slot], equippedItems[slot + vanityOffset], equipHidden[slot])).ToArray()
					));
				}
			}
		}
		static bool hovered = false;
		static bool showButton = false;
		static void IL_Main_DrawInventory(ILContext il) {
			ILCursor cur = new(il);
			int slotId = -1;
			cur.GotoNext(
				i => i.MatchLdcI4(ItemSlot.Context.EquipDye),
				i => i.MatchLdloc(out slotId),
				il => il.MatchCall<ItemSlot>(nameof(ItemSlot.OverrideHover))
			);
			ILLabel skip = default;
			int yPos = -1;
			cur.GotoPrev(MoveType.AfterLabel,
				i => i.MatchLdsfld<Main>(nameof(Main.mouseY)),
				i => i.MatchLdloc(out yPos),
				il => il.MatchBlt(out skip)
			);
			int xPos = -1;
			cur.GotoPrev(MoveType.AfterLabel,
				i => i.MatchLdsfld<Main>(nameof(Main.mouseX)),
				i => i.MatchLdloc(out xPos),
				il => il.MatchBlt(out ILLabel _skip) && _skip.Target == skip.Target
			);
			cur.EmitLdloc(xPos);
			cur.EmitLdloc(yPos);
			cur.EmitLdloc(slotId);
			cur.EmitDelegate((int x, int y, int slot) => {
				return !HandleButtonInteraction(x, y, Main.LocalPlayer.dye, 12, slot);
			});
			cur.EmitBrfalse(skip);
		}
		static bool HandleButtonInteraction(int x, int y, Item[] items, int context, int slot) {
			showButton = false;
			hovered = false;
			if (!Enabled) return false;
			if (drawingExtraDyeSlots) return false;
			if (context is ItemSlot.Context.EquipDye or ItemSlot.Context.ModdedDyeSlot) {
				(int vanityOffset, Item[] equippedItems, bool[] equipHidden) = GetEquippedItems(items == Main.LocalPlayer.dye);
				if (!extraDyeSlots.Any(s => s.UseForSlot(equippedItems[slot], equippedItems[slot + vanityOffset], equipHidden[slot]))) return false;
				showButton = true;
				if (ButtonHovered(x, y)) {
					Main.LocalPlayer.mouseInterface = true;
					Main.instance.MouseText(Language.GetTextValue("Mods.PegasusLib.ExtraDyeSlotsButton"));
					hovered = true;
					return true;
				}
			}
			return false;
		}
		internal static (int vanityOffset, Item[] equippedItems, bool[] equipHidden) GetEquippedItems(bool vanilla) {
			if (vanilla) {
				return (3 + AccessorySlotLoader.MaxVanillaSlotCount, Main.LocalPlayer.armor, Main.LocalPlayer.hideVisibleAccessory);
			} else {
				ModAccessorySlotPlayer asp = Main.LocalPlayer.GetModPlayer<ModAccessorySlotPlayer>();
				return (asp.SlotCount, exAccessorySlot.GetValue(asp), exHideAccessory.GetValue(asp));
			}
		}
		static void On_DrawSlot(orig_DrawSlot orig, AccessorySlotLoader self, Item[] items, int context, int slot, bool flag3, int xLoc, int yLoc, bool skipCheck = false) {
			if (HandleButtonInteraction(xLoc - 47 * slotDrawLoopCounter.GetValue(self), yLoc, items, context, slot)) {
				skipCheck = true;
			}
			orig(self, items, context, slot, flag3, xLoc, yLoc, skipCheck);
		}
		static bool ButtonHovered(int x, int y) {
			const int deflate = 1;
			Rectangle value = GetPosition(new(x, y));
			value.Inflate(-deflate, -deflate);
			return value.Contains(Main.mouseX, Main.mouseY);
		}
		delegate void orig_DrawSlot(AccessorySlotLoader self, Item[] items, int context, int slot, bool flag3, int xLoc, int yLoc, bool skipCheck = false);
		delegate void hook_DrawSlot(orig_DrawSlot orig, AccessorySlotLoader self, Item[] items, int context, int slot, bool flag3, int xLoc, int yLoc, bool skipCheck = false);
		public void Unload() => extraDyeSlots.Clear();
	}
	public abstract class ExtraDyeSlot : ModType, ILocalizedModType {
		public string LocalizationCategory => "ExtraDyeSlots";
		public int Type { get; private set; }
		public virtual LocalizedText DisplayText => this.GetLocalization(nameof(DisplayText), () => "{$LegacyInterface.57}");
		FakeDyeSlot fakeDyeSlot;
		protected sealed override void Register() {
			PegasusLib.Require(Mod, LibFeature.ExtraDyeSlots);
			ModTypeLookup<ExtraDyeSlot>.Register(this);
			Type = ExtraDyeSlots.extraDyeSlots.Count;
			ExtraDyeSlots.extraDyeSlots.Add(this);
			Mod.AddContent(fakeDyeSlot = new FakeDyeSlot(this));
		}
		public sealed override void SetupContent() {
			_ = DisplayText;
			SetStaticDefaults();
		}
		public abstract bool UseForSlot(Item equipped, Item vanity, bool equipHidden);
		public abstract void ApplyDye(Player player, [NotNull] Item dye);
		internal (Item[] slots, int index) ItemData => (ExtraDyeSlots.exDyesAccessory.GetValue(fakeDyeSlot.ModSlotPlayer), fakeDyeSlot.Type);
		public ref Item Item {
			get {
				(Item[] slots, int index) = ItemData;
				return ref slots[index];
			}
		}
		public class FakeDyeSlot(ExtraDyeSlot slot) : ModAccessorySlot {
			public override string Name => slot.Name + "_Slot";
			public override bool IsHidden() => DrawFunctionalSlot || DrawVanitySlot;
			public override bool DrawFunctionalSlot => !(FunctionalItem?.IsAir ?? true);
			public override bool DrawVanitySlot => !(VanityItem?.IsAir ?? true);
			public override bool DrawDyeSlot => false;
			public override void ApplyEquipEffects() {
				if (DyeItem?.IsAir == false) slot.ApplyDye(Player, DyeItem);
			}
		}
	}
	internal class ExtraDyeSlotUI : UIState {
		private readonly int x;
		private readonly int y;
		private readonly ExtraDyeSlot[] slots;
		readonly int invalidSlotIndex;
		UIPanel panel;

		public ExtraDyeSlotUI(int x, int y, ExtraDyeSlot[] slots) {
			this.x = x;
			this.y = y;
			invalidSlotIndex = slots.Length;
			static bool IsValidAnywhere(ExtraDyeSlot slot, int vanityOffset, Item[] equippedItems, bool[] equipHidden) {
				for (int j = 0; j < vanityOffset; j++) {
					if (slot.UseForSlot(equippedItems[j], equippedItems[j + vanityOffset], equipHidden[j])) return true;
				}
				return false;
			}
			List<ExtraDyeSlot> invalidSlots = [];
			for (int i = 0; i < ExtraDyeSlots.extraDyeSlots.Count; i++) {
				ExtraDyeSlot slot = ExtraDyeSlots.extraDyeSlots[i];
				if (slots.Contains(slot)) continue;
				(Item[] inv, int j) = slot.ItemData;
				if (inv[j]?.IsAir ?? true) continue;

				(int vanityOffset, Item[] equippedItems, bool[] equipHidden) = ExtraDyeSlots.GetEquippedItems(true);
				if (IsValidAnywhere(slot, vanityOffset, equippedItems, equipHidden)) continue;

				(vanityOffset, equippedItems, equipHidden) = ExtraDyeSlots.GetEquippedItems(false);
				if (IsValidAnywhere(slot, vanityOffset, equippedItems, equipHidden)) continue;

				invalidSlots.Add(slot);
			}
			Array.Resize(ref slots, slots.Length + invalidSlots.Count);
			invalidSlots.CopyTo(slots, invalidSlotIndex);
			this.slots = slots;
		}

		public bool Matches(ExtraDyeSlotUI other) {
			if (other is null) return false;
			if (this.x != other.x) return false;
			if (this.y != other.y) return false;
			if (!this.slots.SequenceEqual(other.slots)) return false;
			return true;
		}
		public const int width_per_slot = 50;
		public override void OnInitialize() {
			HAlign = 1;
			panel = new();
			panel.Left.Set(x - Main.screenWidth - 2 - width_per_slot * slots.Length, 1);
			panel.Top.Set(y - 3, 0);
			panel.Width.Set(width_per_slot * slots.Length + 2, 0);
			panel.Height.Set(50, 0);
			panel.PaddingLeft = 0;
			panel.PaddingRight = 0;
			panel.PaddingTop = 0;
			panel.PaddingBottom = 0;
			if (slots.Length == 1) {
				panel.BackgroundColor = default;
				panel.BorderColor = default;
			}
			for (int i = 0; i < slots.Length; i++) {
				panel.Append(new UIExtraDyeSlot(slots[i], i));
			}
			Append(panel);
		}
		public override void Update(GameTime gameTime) {
			bool recalculate = false;
			while (removeStack.TryPop(out UIExtraDyeSlot removeSlot)) {
				if (removeSlot.Parent != panel) continue;
				foreach (UIElement child in panel.Children) {
					if (child.Left.Pixels < removeSlot.Left.Pixels) {
						child.Left.Pixels += width_per_slot;
					}
				}
				panel.RemoveChild(removeSlot);
				panel.Width.Pixels -= width_per_slot;
				panel.Left.Pixels += width_per_slot;
				recalculate = true;
			}
			if (recalculate) {
				if (panel.Children.Count() == 1) {
					panel.BackgroundColor = default;
					panel.BorderColor = default;
				}
				Recalculate();
			}
			base.Update(gameTime);
		}
		protected override void DrawChildren(SpriteBatch spriteBatch) {
			try {
				ExtraDyeSlots.drawingExtraDyeSlots = true;
				base.DrawChildren(spriteBatch);
			} finally {
				ExtraDyeSlots.drawingExtraDyeSlots = false;
			}
		}
		readonly Stack<UIExtraDyeSlot> removeStack = new();
		class UIExtraDyeSlot(ExtraDyeSlot slot, int slotIndex) : UIElement {
			public override void OnInitialize() {
				HAlign = 1;
				Left.Set((slotIndex + 1) * -50 + 2, 0);
				Top.Set(3, 0);
			}
			public override void Draw(SpriteBatch spriteBatch) {
				(Item[] items, int index) = slot.ItemData;
				Vector2 position = GetDimensions().Position();
				int context = 12;
				if (Parent.Parent is ExtraDyeSlotUI parent && slotIndex >= parent.invalidSlotIndex) {
					context = ItemSlot.Context.VoidItem;
					if (items[index]?.IsAir ?? true) {
						parent.removeStack.Push(this);
						return;
					}
				}
				ItemSlot.OverrideHover(items, context, index);
				if (Main.MouseScreen.Between(position, position + new Vector2(TextureAssets.InventoryBack.Width() * Main.inventoryScale))) {
					Main.LocalPlayer.mouseInterface = true;
					if (Main.mouseRightRelease && Main.mouseRight) {
						ItemSlot.RightClick(items, context, index);
					}
					ItemSlot.LeftClick(items, context, index);
					ItemSlot.MouseHover(items, context, index);
					Main.hoverItemName = slot.DisplayText.Value;
					if (Main.HoverItem?.TryGetGlobalItem(out ShowDyeSlotNames showNames) ?? false) showNames.text = slot.DisplayText;
				}
				ItemSlot.Draw(spriteBatch, items, context, index, position);
			}
		}
	}
	internal class ShowDyeSlotNames : GlobalItem {
		internal LocalizedText text;
		public override bool InstancePerEntity => true;
		public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.dye != 0;
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (text is not null) {
				tooltips.Insert(0, new(Mod, "SlotName", text.Value));
			}
		}
	}
	internal class ExtraDyeSlotSystem : ModSystem {
		readonly UserInterface UI = new();
		public static void SetUI(ExtraDyeSlotUI state) {
			UserInterface ui = ModContent.GetInstance<ExtraDyeSlotSystem>().UI;
			if (state.Matches(ui.CurrentState as ExtraDyeSlotUI)) {
				ui.SetState(null);
				return;
			}
			state.Activate();
			ui.SetState(state);
		}
		public override void UpdateUI(GameTime gameTime) {
			UI?.Update(gameTime);
		}
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (inventoryIndex != -1) {
				if (!Main.playerInventory) UI.SetState(null);
				layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
					"PegasusLib: Extra Dye Slots",
					delegate {
						// If the current UIState of the UserInterface is null, nothing will draw. We don't need to track a separate .visible value.
						UI.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}
	}
}
