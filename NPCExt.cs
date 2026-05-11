using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Graphics;
using PegasusLib.Networking;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PegasusLib {
	public static class NPCExt {
		public static NPCID.Sets.NPCBestiaryDrawModifiers HideInBestiary => new() {
			Hide = true
		};
		public static NPCID.Sets.NPCBestiaryDrawModifiers BestiaryWalkLeft => new() {
			Velocity = 1f
		};
		static RenderTarget2D renderTarget;
		public static void DrawBestiaryIcon(SpriteBatch spriteBatch, int type, Rectangle within, bool hovering = false, DrawData? stencil = null, Blend stencilColorBlend = Blend.SourceAlpha) {
			BestiaryEntry bestiaryEntry = BestiaryDatabaseNPCsPopulator.FindEntryByNPCID(type);
			if (bestiaryEntry?.Icon is not null) {
				if (renderTarget is not null && (renderTarget.Width != Main.screenWidth || renderTarget.Height != Main.screenHeight)) {
					renderTarget.Dispose();
					renderTarget = null;
				}
				renderTarget ??= new(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
				Rectangle screenPos = new((Main.screenWidth) / 2, (Main.screenHeight) / 2, (int)(within.Width), (int)(within.Height));
				screenPos.X -= screenPos.Width / 2;
				screenPos.Y -= screenPos.Height / 2;
				//within.Width 
				BestiaryUICollectionInfo info = new() {
					OwnerEntry = bestiaryEntry,
					UnlockState = BestiaryEntryUnlockState.CanShowDropsWithDropRates_4
				};
				EntryIconDrawSettings settings = new() {
					iconbox = screenPos,
					IsHovered = hovering,
					IsPortrait = false
				};
				bestiaryEntry.Icon.Update(info, screenPos, settings);
				SpriteBatchState state = spriteBatch.GetState();
				spriteBatch.Restart(SpriteBatchState.Default, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, Main.Rasterizer, null, Main.UIScaleMatrix);
				Main.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
				Main.graphics.GraphicsDevice.Clear(Color.Transparent);
				bestiaryEntry.Icon.Draw(info, spriteBatch, settings);
				if (stencil is not null) {
					DrawData stencilValue = stencil.Value;
					spriteBatch.Restart(spriteBatch.GetState(), blendState: new BlendState() {
						ColorSourceBlend = Blend.One,
						AlphaSourceBlend = Blend.One,
						ColorDestinationBlend = stencilColorBlend,
						AlphaDestinationBlend = Blend.SourceAlpha
					});
					stencilValue.position += screenPos.Center() / Main.UIScale;
					stencilValue.scale *= 2 / Main.UIScale;
					stencilValue.Draw(spriteBatch);
				}
				spriteBatch.Restart(state);
				RenderTargetUsage renderTargetUsage = Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage;
				Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
				Main.graphics.GraphicsDevice.SetRenderTarget(null);
				Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = renderTargetUsage;
				screenPos = new(
					(int)((screenPos.X - screenPos.Width * 0.5f)),
					(int)((screenPos.Y - screenPos.Height * 0.5f)),
					(screenPos.Width * 2),
					(screenPos.Height * 2)
				);
				within.Width = (int)(within.Width / Main.UIScale);
				within.Height = (int)(within.Height / Main.UIScale);
				spriteBatch.Draw(renderTarget, within, screenPos, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
			}
		}
		public static bool HasRightDungeonWall(this NPCSpawnInfo spawnInfo, DungeonWallType wallType) {
			if (spawnInfo.Player.RollLuck(7) == 0) {
				return Main.rand.NextBool(3);
			} else {
				ushort wall = Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType;
				ushort wall2 = Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].WallType;
				switch (wallType) {
					case DungeonWallType.Brick:
					return wall is 7 or 8 or 9 || wall2 is 7 or 8 or 9;
					case DungeonWallType.Slab:
					return wall is 94 or 96 or 98 || wall2 is 94 or 96 or 98;
					case DungeonWallType.Tile:
					return wall is 95 or 97 or 99 || wall2 is 95 or 97 or 99;
				}
			}
			return true;
		}
		public enum DungeonWallType {
			Brick,
			Slab,
			Tile
		}
		public static FlavorTextBestiaryInfoElement GetOrRegisterBestiaryFlavorText(this ModNPC npc) {
			Language.GetOrRegister($"Mods.{npc.Mod.Name}.Bestiary.{npc.Name}", () => "bestiary text here");
			return new FlavorTextBestiaryInfoElement($"Mods.{npc.Mod.Name}.Bestiary.{npc.Name}");
		}
		static readonly FastStaticFieldInfo<NPC, bool> noSpawnCycle = "noSpawnCycle";
		public static void Despawn(this NPC npc) {
			if (!NetmodeActive.MultiplayerClient && !NPCLoader.SavesAndLoads(npc)) {
				noSpawnCycle.Value = true;
				npc.active = false;
				if (NetmodeActive.Server) {
					npc.netSkip = -1;
					npc.life = 0;
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
				}
				if (npc.extraValue > 0) NPC.RevengeManager.CacheEnemy(npc);
			}
		}
	}
}
