using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace PegasusLib.Content {
	[Autoload(Side = ModSide.Client)]
	public class PerlinNoise : ILoadable {
		static Asset<Texture2D> perlin;
		static float[,] odds;
		void ILoadable.Load(Mod mod) {
			perlin = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin");
			Main.QueueMainThreadAction(EnsureLoaded);
		}
		void ILoadable.Unload() { }
		static void EnsureLoaded() {
			if (odds is null) {
				Texture2D perlin = PerlinNoise.perlin.Value;
				Color[] color = new Color[perlin.Width * perlin.Height];
				perlin.GetData(color);
				odds = new float[perlin.Width, perlin.Height];
				for (int k = 0; k < color.Length; k++) {
					odds[k % perlin.Width, k / perlin.Width] = color[k].R / 255f;
				}
			}
		}
		public static float Get(int x, int y) => odds[x, y];
		public static int Width => odds.GetLength(0);
		public static int Height => odds.GetLength(1);
	}
}
