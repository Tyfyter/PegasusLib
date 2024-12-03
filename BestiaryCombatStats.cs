using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PegasusLib {
	public class BestiaryCombatStatLoader : ILoadable {
		internal static List<BestiaryCombatStat> bestiaryCombatStats = [];
		public void Load(Mod mod) {
			On_NPCStatsReportInfoElement.ProvideUIElement += On_NPCStatsReportInfoElement_ProvideUIElement;
			bestiaryStatBackground.LoadAsset();
		}
		static AutoLoadingAsset<Texture2D> bestiaryStatBackground = "PegasusLib/Textures/Bestiary_Stat_Background";
		private static UIElement On_NPCStatsReportInfoElement_ProvideUIElement(On_NPCStatsReportInfoElement.orig_ProvideUIElement orig, NPCStatsReportInfoElement self, BestiaryUICollectionInfo info) {
			UIElement element = orig(self, info);
			if (info.UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0) {
				int statCount = 0;
				foreach (BestiaryCombatStat stat in bestiaryCombatStats) {
					string modName = (stat as ModType)?.Mod?.Name ?? "Origins";
					NPC npc = ContentSamples.NpcsByNetId[self.NpcId];
					if (stat.ShouldDisplay(npc)) {
						if (statCount % 2 == 0) {
							element.Height.Pixels += 35;
							bool foundSeparator = false;
							foreach (UIElement child in element.Children) {
								if (foundSeparator || child is UIHorizontalSeparator) {
									foundSeparator = true;
									child.Top.Pixels += 35;
								}
							}
						}
						bestiaryStatBackground.LoadAsset();
						UIImage uIImage = new((Asset<Texture2D>)bestiaryStatBackground) {
							Top = new StyleDimension(70, 0f),
							Left = new StyleDimension(3 + 99 * (statCount % 2), 0f)
						};
						stat.TextureValue.LoadAsset();
						uIImage.Append(new UIImageFramed(stat.TextureValue, stat.TextureFrame) {
							HAlign = 0f,
							VAlign = 0.5f,
							Left = new StyleDimension(2, 0f),
							Top = new StyleDimension(0, 0f),
							IgnoresMouseInteraction = true
						});
						uIImage.Append(new UIText(stat.GetDisplayText(npc)) {
							HAlign = 1f,
							VAlign = 0.5f,
							Left = new StyleDimension(-10, 0f),
							Top = new StyleDimension(0, 0f),
							IgnoresMouseInteraction = true
						});
						uIImage.OnUpdate += (element) => {
							if (element.IsMouseHovering) {
								Main.instance.MouseText(stat.DisplayName.Value, 0, 0);
							}
						};
						element.Append(uIImage);
						statCount++;
					}
				}
			}
			return element;
		}
		public void Unload() {
			bestiaryCombatStats.Clear();
		}
	}
	public abstract class BestiaryCombatStat : ModTexturedType, ILocalizedModType {
		public int Type { get; private set; }
		public AutoLoadingAsset<Texture2D> TextureValue { get; private set; }
		public string LocalizationCategory => "BestiaryCombatStat";
		public virtual Rectangle TextureFrame => TextureValue.Value.Bounds;
		public virtual LocalizedText DisplayName => this.GetLocalization("DisplayName");
		protected sealed override void Register() {
			Type = BestiaryCombatStatLoader.bestiaryCombatStats.Count;
			BestiaryCombatStatLoader.bestiaryCombatStats.Add(this);
			TextureValue = Texture;
			_ = DisplayName.Value;
		}
		public abstract bool ShouldDisplay(NPC npc);
		public abstract string GetDisplayText(NPC npc);
	}
}
