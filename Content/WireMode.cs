using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Networking;
using PegasusLib.UI;
using ReLogic.Content;
using ReLogic.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Content;
public static class WireModeLoader {
	public static int WireModeCount => wireModes.Count;
	static readonly List<WireMode> wireModes = [];
	static readonly List<WireMode> sortedWireModes = [];
	public static WireMode Get(int type) => wireModes.GetIfInRange(type);
	public static WireMode GetSorted(int index) => sortedWireModes.GetIfInRange(index);
	public static IEnumerable<WireMode> GetSorted(bool[] set) {
		for (int i = 0; i < sortedWireModes.Count; i++) {
			if (set[sortedWireModes[i].Type]) yield return sortedWireModes[i];
		}
	}
	public static IEnumerable<WireMode> GetSorted(BitArray set) {
		for (int i = 0; i < sortedWireModes.Count; i++) {
			if (set[sortedWireModes[i].Type]) yield return sortedWireModes[i];
		}
	}
	public static IEnumerable<WireMode> GetSorted() => sortedWireModes;
	internal static void Register(WireMode mode) {
		mode.Type = WireModeCount;
		wireModes.Add(mode);
	}
	internal static void Sort() {
		sortedWireModes.Clear();
		sortedWireModes.AddRange(new TopoSort<WireMode>(wireModes,
			mode => mode.SortAfter(),
			mode => mode.SortBefore()
		).Sort());
	}
}
[Flags]
public enum WirePetalData {
	Enabled = 1 << 0,
	Cutter = 1 << 1
}
public abstract class WireMode : ModTexturedType, IFlowerMenuItem<WirePetalData> {
	public int Type { get; internal set; }
	public virtual int ItemType { get; } = ItemID.Wire;
	public virtual bool IsExtra => false;
	public virtual Color? WireKiteColor => null;
	public virtual Color MiniWireMenuColor => WireKiteColor ?? Color.White;
	public Asset<Texture2D> Texture2D { get; private set; }
	protected sealed override void Register() {
		if (Mod.Side != ModSide.Both && Mod is not PegasusLib) throw new InvalidOperationException("WireModes can only be added by Both-side mods");
		WireModeLoader.Register(this);
		ModTypeLookup<WireMode>.Register(this);
	}
	public sealed override void SetupContent() {
		if (!Main.dedServ) Texture2D = ModContent.Request<Texture2D>(Texture);
		SetStaticDefaults();
	}
	public virtual void SetupPreSort() { }
	public virtual void SetupSets() { }
	public abstract bool GetWire(int x, int y);
	public abstract bool SetWire(int x, int y, bool value);
	public static void DrawIcon(Texture2D texture, Vector2 position, Color tint) {
		Main.spriteBatch.Draw(
			texture,
			position,
			null,
			tint,
			0f,
			texture.Size() * 0.5f,
			1,
			SpriteEffects.None,
		0f);
	}
	public static void GetTints(bool hovered, bool enabled, out Color backTint, out Color iconTint) {
		if (enabled) {
			backTint = Color.White;
			iconTint = Color.White;
		} else if (hovered) {
			backTint = new Color(200, 200, 200);
			iconTint = new Color(120, 120, 120);
		} else {
			backTint = new Color(100, 100, 100);
			iconTint = new Color(80, 80, 80);
		}
	}
	public virtual void Draw(Vector2 position, bool hovered, WirePetalData data) {
		GetTints(hovered, data.HasFlag(WirePetalData.Enabled), out Color backTint, out Color iconTint);
		DrawIcon(TextureAssets.WireUi[hovered.ToInt() + data.HasFlag(WirePetalData.Cutter).ToInt() * 8].Value, position, backTint);
		DrawIcon(Texture2D.Value, position, iconTint);
	}
	public virtual IEnumerable<WireMode> SortAfter() => [];
	public virtual IEnumerable<WireMode> SortBefore() => [];
	public bool IsHovered(Vector2 position) => Main.MouseScreen.WithinRange(position, 20);

	[ReinitializeDuringResizeArrays]
	public static class Sets {
		public static SetFactory Factory = new(WireModeLoader.WireModeCount, $"{nameof(PegasusLib)}/{nameof(WireModeID)}", WireModeID.Search);
		public static BitArray NormalWires = new(Factory.CreateBoolSet());
		public static BitArray AshenWires = new(Factory.CreateBoolSet());
		static Sets() {
			foreach (WireMode mode in ModContent.GetContent<WireMode>()) mode.SetupPreSort();
			WireModeLoader.Sort();
			foreach (WireMode mode in ModContent.GetContent<WireMode>()) mode.SetupSets();
		}
	}
	private class WireModeID {
		/// <inheritdoc cref="IdDictionary"/>
		public static readonly IdDictionary Search = IdDictionary.Create<WireModeID, byte>();
	}
}
public class Actuator_Wire_Mode : WireMode {
	public override string Texture => "Terraria/Images/UI/Wires_10";
	public override int ItemType => ItemID.Actuator;
	public override Color MiniWireMenuColor => Color.WhiteSmoke;
	public override bool IsExtra => true;
	public override void SetupSets() {
		Sets.NormalWires[Type] = true;
	}
	public override bool GetWire(int x, int y) => Main.tile[x, y].HasActuator;
	public override bool SetWire(int x, int y, bool value) {
		Tile tile = Main.tile[x, y];
		if (tile.HasActuator != value) {
			tile.HasActuator = value;
			NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 8 + (!value).ToInt(), x, y);
			return true;
		}
		return false;
	}
}
public class Red_Wire_Mode : WireMode {
	public override string Texture => "Terraria/Images/UI/Wires_2";
	public override Color? WireKiteColor => new Color(253, 58, 61);
	public override void SetupSets() {
		Sets.NormalWires[Type] = true;
	}
	public override bool GetWire(int x, int y) => Main.tile[x, y].RedWire;
	public override bool SetWire(int x, int y, bool value) {
		Tile tile = Main.tile[x, y];
		if (tile.RedWire != value) {
			tile.RedWire = value;
			NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5 + (!value).ToInt(), x, y);
			return true;
		}
		return false;
	}
}
public class Blue_Wire_Mode : WireMode {
	public override string Texture => "Terraria/Images/UI/Wires_4";
	public override Color? WireKiteColor => new Color(83, 180, 253);
	public override void SetupSets() {
		Sets.NormalWires[Type] = true;
	}
	public override bool GetWire(int x, int y) => Main.tile[x, y].BlueWire;
	public override bool SetWire(int x, int y, bool value) {
		Tile tile = Main.tile[x, y];
		if (tile.BlueWire != value) {
			tile.BlueWire = value;
			NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 10 + (!value).ToInt(), x, y);
			return true;
		}
		return false;
	}
	public override IEnumerable<WireMode> SortAfter() => [ModContent.GetInstance<Red_Wire_Mode>()];
}
public class Green_Wire_Mode : WireMode {
	public override string Texture => "Terraria/Images/UI/Wires_3";
	public override Color? WireKiteColor => new Color(83, 253, 153);
	public override void SetupSets() {
		Sets.NormalWires[Type] = true;
	}
	public override bool GetWire(int x, int y) => Main.tile[x, y].GreenWire;
	public override bool SetWire(int x, int y, bool value) {
		Tile tile = Main.tile[x, y];
		if (tile.GreenWire != value) {
			tile.GreenWire = value;
			NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 12 + (!value).ToInt(), x, y);
			return true;
		}
		return false;
	}
	public override IEnumerable<WireMode> SortAfter() => [ModContent.GetInstance<Blue_Wire_Mode>()];
}
public class Yellow_Wire_Mode : WireMode {
	public override string Texture => "Terraria/Images/UI/Wires_5";
	public override Color? WireKiteColor => new Color(253, 254, 83);
	public override void SetupSets() {
		Sets.NormalWires[Type] = true;
	}
	public override bool GetWire(int x, int y) => Main.tile[x, y].YellowWire;
	public override bool SetWire(int x, int y, bool value) {
		Tile tile = Main.tile[x, y];
		if (tile.YellowWire != value) {
			tile.YellowWire = value;
			NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 16 + (!value).ToInt(), x, y);
			return true;
		}
		return false;
	}
	public override IEnumerable<WireMode> SortAfter() => [ModContent.GetInstance<Green_Wire_Mode>()];
}
public interface IWireTool {
	public IEnumerable<WireMode> Modes { get; }
}
[ReinitializeDuringResizeArrays]
public class WireModeKite : ItemModeFlowerMenu<WireMode, WirePetalData> {
	public static bool Cutter { get; set; }
	public static bool[] EnabledWires { get; } = WireMode.Sets.Factory.CreateBoolSet();
	public static IWireTool WireTool => Main.LocalPlayer.HeldItem.ModItem as IWireTool;
	public override bool Toggle => RightClicked;
	public override bool IsActive() => WireTool is not null;
	AutoLoadingAsset<Texture2D> wireMiniIcons = "Origins/Items/Tools/Wiring/Mini_Wire_Icons";
	AutoLoadingAsset<Texture2D> extraMiniIcons = "Origins/Items/Tools/Wiring/Mini_Wire_Extra_Icons";
	public override float DrawCenter() {
		bool hovered = Main.MouseScreen.WithinRange(activationPosition, 20);
		int cutter = Cutter.ToInt();
		WireMode.GetTints(hovered, true, out Color backTint, out Color iconTint);
		WireMode.DrawIcon(TextureAssets.WireUi[hovered.ToInt() + cutter * 8].Value, activationPosition, backTint);
		WireMode.DrawIcon(TextureAssets.WireUi[6 + cutter].Value, activationPosition, iconTint);
		if (hovered) {
			Main.LocalPlayer.mouseInterface = true;
			if (Main.mouseLeft && Main.mouseLeftRelease) Cutter = !Cutter;
		}
		return 44;
	}
	public override WirePetalData GetData(WireMode mode) {
		WirePetalData data = 0;
		if (EnabledWires[mode.Type]) data |= WirePetalData.Enabled;
		if (Cutter) data |= WirePetalData.Cutter;
		return data;
	}
	public override bool GetCursorAreaTexture(WireMode mode, out Texture2D texture, out Rectangle? frame, out Color color) {
		texture = mode.IsExtra ? extraMiniIcons : wireMiniIcons;
		frame = new Rectangle(12 * (1 + Cutter.ToInt()), 0, 10, 10);
		color = Color.Lerp(mode.MiniWireMenuColor, mode.MiniWireMenuColor == Color.Black ? new(50, 50, 50) : Color.Black, (!EnabledWires[mode.Type]).ToInt() * 0.65f);
		return true;
	}
	public override void Click(WireMode mode) {
		if (RightClicked) return;
		EnabledWires[mode.Type] ^= true;
	}
	public override IEnumerable<WireMode> GetModes() => WireTool.Modes;
}
[Autoload(false)]
public class ModWireChannel : ModProjectile {
	public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WireKite;
	public override void SetDefaults() {
		Projectile.width = 0;
		Projectile.height = 0;
		Projectile.tileCollide = false;
		Projectile.hide = true;
	}
	public override bool ShouldUpdatePosition() => false;
	public override void AI() {
		if (!Projectile.IsLocallyOwned()) {
			Projectile.timeLeft = 5;
			return;
		}
		Player player = Main.player[Projectile.owner];
		if (player.HeldItem?.ModItem is not IWireTool wireTool) {
			Projectile.Kill();
			return;
		}
		if (player.channel) {
			Projectile.timeLeft = 5;
		} else {
			new Mass_Wire_Action(
				player,
				new((int)Projectile.ai[0], (int)Projectile.ai[1]),
				new(Player.tileTargetX, Player.tileTargetY),
				WireModeKite.Cutter,
				wireTool.Modes.Where(i => WireModeKite.EnabledWires[i.Type]).ToArray()
			).Perform();
			Projectile.Kill();
		}
	}
	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overWiresUI.Add(index);
	public override bool PreDraw(ref Color lightColor) {
		if (!Projectile.IsLocallyOwned()) return false;
		Player player = Main.player[Projectile.owner];
		if (player.HeldItem?.ModItem is not IWireTool wireTool) {
			Projectile.Kill();
			return false;
		}
		Rectangle screen = new(-16 * 5, -16 * 5, Main.screenWidth + 16 * 10, Main.screenHeight + 16 * 10);
		bool hasDrawn = false;
		Point[] positions = GetWirePositions(player, new((int)Projectile.ai[0], (int)Projectile.ai[1]), new(Player.tileTargetX, Player.tileTargetY));
		Color color = new(127, 127, 127, 0);
		Color outerColor = WireModeKite.Cutter ? new(50, 50, 50, 255) : new(255, 255, 255, 0);
		int colorsCount = 0;
		bool hasExtra = false;
		foreach (WireMode mode in wireTool.Modes) {
			if (!WireModeKite.EnabledWires[mode.Type]) continue;
			if (mode.WireKiteColor is Color innerColor) color = Color.Lerp(color, innerColor, 1f / ++colorsCount);
			hasExtra |= mode.IsExtra;
		}
		color *= 2;
		color.A = (byte)((255 - Math.Min(color.R + color.G + color.B, 255)) / 2);
		for (int i = 0; i < positions.Length; i++) {
			Vector2 screenPos = positions[i].ToWorldCoordinates(0, 0) - Main.screenPosition;
			if (hasDrawn.TrySet(screen.Contains(screenPos)) && !hasDrawn) {
				break;
			}
			if (!hasDrawn) continue;
			Rectangle wireFrame = new(0, 0, 16, 16);
			void ModifyFrame(int index) {
				if (!positions.IndexInRange(index)) return;
				wireFrame.X += (positions[index] - positions[i]) switch {
					(0, -1) => 18,
					(1, 0) => 36,
					(0, 1) => 72,
					(-1, 0) => 144,
					_ => 0
				};
			}
			ModifyFrame(i - 1);
			ModifyFrame(i + 1);
			if (hasExtra)
				Main.EntitySpriteDraw(TextureAssets.WireUi[11].Value, screenPos, null, outerColor, 0f, Vector2.Zero, 1f, SpriteEffects.None);
			Main.EntitySpriteDraw(
				TextureAssets.Projectile[Type].Value,
				screenPos,
				wireFrame,
				color,
				0,
				Vector2.Zero,
				1,
				SpriteEffects.None
			);
			wireFrame.Y += 18;
			Main.EntitySpriteDraw(
				TextureAssets.Projectile[Type].Value,
				screenPos,
				wireFrame,
				outerColor,
				0,
				Vector2.Zero,
				1,
				SpriteEffects.None
			);
		}
		return false;
	}
	public static Point[] GetWirePositions(Player player, Point start, Point end) {
		Point pos = start;
		Point diff = new(end.X - start.X, end.Y - start.Y);
		Point dir = new(int.Sign(diff.X), int.Sign(diff.Y));
		diff *= dir;
		bool yFirst = player.direction == 1;
		int index = 1;
		Point[] positions = new Point[diff.X + diff.Y + 1];
		positions[0] = start;
		for (int i = yFirst ? diff.Y : diff.X; i > 0; i--) {
			if (yFirst) pos.Y += dir.Y;
			else pos.X += dir.X;
			if (pos != positions[index - 1]) positions[index++] = pos;
		}
		for (int i = !yFirst ? diff.Y : diff.X; i > 0; i--) {
			if (!yFirst) pos.Y += dir.Y;
			else pos.X += dir.X;
			if (pos != positions[index - 1]) positions[index++] = pos;
		}
		return positions;
	}
}
public record class Mass_Wire_Action(Player Player, Point Start, Point End, bool Cut, WireMode[] Modes) : SyncedAction {
	public override bool ServerOnly => true;
	public Mass_Wire_Action() : this(default, default, default, default, default) { }
	public override SyncedAction NetReceive(BinaryReader reader) => this with {
		Player = Main.player[reader.ReadByte()],
		Start = new(reader.ReadInt32(), reader.ReadInt32()),
		End = new(reader.ReadInt32(), reader.ReadInt32()),
		Cut = reader.ReadBoolean(),
		Modes = ReadUnorderedSet(reader, WireModeLoader.GetSorted()).ToArray()
	};
	public override void NetSend(BinaryWriter writer) {
		writer.Write((byte)Player.whoAmI);
		writer.Write(Start.X);
		writer.Write(Start.Y);
		writer.Write(End.X);
		writer.Write(End.Y);
		writer.Write(Cut);
		WriteUnorderedSet(writer, WireModeLoader.GetSorted(), Modes);
	}
	static bool HasResearched(int type, Player player) => CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId.TryGetValue(type, out int needed) && player.creativeTracker.ItemSacrifices.GetSacrificeCount(type) >= needed;
	protected override void Perform() {
		Dictionary<int, int> costs = [];
		WireMode[] modes = ReorderSet(WireModeLoader.GetSorted(), Modes);
		for (int i = 0; i < modes.Length; i++) {
			if (HasResearched(modes[i].ItemType, Player)) continue;
			costs[modes[i].ItemType] = 0;
		}
		foreach (Point pos in ModWireChannel.GetWirePositions(Player, Start, End)) {
			bool didAny = false;
			Vector2 worldPos = pos.ToWorldCoordinates(0, 0);
			for (int i = 0; i < modes.Length; i++) {
				if (!Cut) {
					if (costs.ContainsKey(modes[i].ItemType) && !Player.ConsumeItem(modes[i].ItemType)) continue;
				}
				if (modes[i].SetWire(pos.X, pos.Y, !Cut)) {
					didAny |= true;
					SoundEngine.PlaySound(SoundID.Dig, worldPos);
					if (costs.TryGetValue(modes[i].ItemType, out int cost)) costs[modes[i].ItemType] = cost + 1;
				}
			}
			if (didAny && Cut) {
				for (int k = 0; k < 5; k++) {
					Dust.NewDust(worldPos, 16, 16, DustID.Adamantite);
				}
			}
		}

		IEntitySource source = Player.GetSource_Misc("GrandDesignOrMultiColorWrench");
		foreach (KeyValuePair<int, int> cost in costs) {
			if (Cut) {
				if (cost.Value > 0) {
					Item item = new(cost.Key);
					int num = cost.Value;
					while (num > 0) {
						int num2 = item.maxStack;
						if (num < num2) {
							num2 = num;
						}
						Item.NewItem(source, Player.Center, cost.Key, num2);
						num -= num2;
					}
				}
			} else {
				if (NetmodeActive.Server) {
					NetMessage.SendData(MessageID.MassWireOperationPay, Player.whoAmI, -1, null, cost.Key, cost.Value, Player.whoAmI);
				}
			}
		}
	}
}