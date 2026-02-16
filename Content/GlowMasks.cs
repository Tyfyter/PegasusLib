using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PegasusLib.Content;
[ReinitializeDuringResizeArrays]
public class GlowMasks : GlobalItem {
	//Stores the glowmasks added to items by this system
	static readonly short[] itemGlowmasks = ItemID.Sets.Factory.CreateCustomSet<short>(-1);
	//Adds a texture to the array that vanilla Terraria uses for item glowmasks
	public static short AddGlowMask(string texture) {
		if (Main.netMode != NetmodeID.Server) {
			string name = texture;
			if (ModContent.RequestIfExists(name, out Asset<Texture2D> asset)) {
				int index = TextureAssets.GlowMask.Length;
				Array.Resize(ref TextureAssets.GlowMask, index + 1);
				TextureAssets.GlowMask[^1] = asset;
				return (short)index;
			}
		}
		return -1;
	}
	//Calls the above method and stores the index of the glowmask to be retrieved using the item's type
	public static short AddGlowMask(ModItem item, string suffix = "_Glow") {
		short slot = AddGlowMask(item.Texture + suffix);
		itemGlowmasks[item.Type] = slot;
		return slot;
	}
	public static short AddGlowMask(ModTexturedType content, string suffix = "_Glow") {
		return AddGlowMask(content.Texture + suffix);
	}
	//Sets items to use the glowmasks that were added to them by this system
	//The actual rendering is handled entirely by vanilla code, all we have to do is tell it what to use
	public override void SetDefaults(Item item) {
		if (itemGlowmasks is null) return; //Just in case it can ever be null, skip everything if it is
		if (itemGlowmasks.IndexInRange(item.type) && itemGlowmasks[item.type] != -1) item.glowMask = itemGlowmasks[item.type];
	}
	//Might not be necessary, either removes all modded glowmasks during unloading or does nothing
	public override void Unload() => Array.Resize(ref TextureAssets.GlowMask, GlowMaskID.Count);
}
