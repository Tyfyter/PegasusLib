using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.Graphics.Shaders;

namespace PegasusLib.Graphics {
	public static class GraphicsExt {
		public static void UseNonVanillaImage(this ArmorShaderData shaderData, Asset<Texture2D> texture) {
			(GraphicsMethods._uImage_Armor ??= new("_uImage", BindingFlags.NonPublic, true)).SetValue(shaderData, texture);
		}
		public static void UseNonVanillaImage(this HairShaderData shaderData, Asset<Texture2D> texture) {
			(GraphicsMethods._uImage_Hair ??= new("_uImage", BindingFlags.NonPublic, true)).SetValue(shaderData, texture);
		}
		public static void UseNonVanillaImage(this MiscShaderData shaderData, Asset<Texture2D> texture) {
			(GraphicsMethods._uImage_Misc ??= new("_uImage", BindingFlags.NonPublic, true)).SetValue(shaderData, texture);
		}
	}
}
