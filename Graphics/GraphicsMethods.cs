using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace PegasusLib.Graphics {
	public class GraphicsMethods : ILoadable {
		public static FastFieldInfo<ArmorShaderData, Asset<Texture2D>> _uImage_Armor { get; internal set; }
		public static FastFieldInfo<HairShaderData, Asset<Texture2D>> _uImage_Hair { get; internal set; }
		public static FastFieldInfo<MiscShaderData, Asset<Texture2D>> _uImage_Misc { get; internal set; }
		public void Load(Mod mod) {
			DynamicMethod getterMethod = new($"{nameof(RenderTarget2D)}.set_{nameof(RenderTarget2D.RenderTargetUsage)}", typeof(void), [typeof(RenderTarget2D), typeof(RenderTargetUsage)], true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.EmitCall(OpCodes.Call, typeof(RenderTarget2D).GetProperty(nameof(RenderTarget2D.RenderTargetUsage)).SetMethod, null);
			gen.Emit(OpCodes.Ret);

			setRenderTargetUsage = getterMethod.CreateDelegate<Action<RenderTarget2D, RenderTargetUsage>>();
		}
		public void Unload() {
			setRenderTargetUsage = null;
			_uImage_Armor = null;
			_uImage_Hair = null;
			_uImage_Misc = null;
		}
		static Action<RenderTarget2D, RenderTargetUsage> setRenderTargetUsage;
		public static void SetRenderTargetUsage(RenderTarget2D self, RenderTargetUsage renderTargetUsage) => setRenderTargetUsage(self, renderTargetUsage);
	}
}
