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
	public static class GraphicsDeviceExtensions {
		/// <summary>
		/// Sets the spritebatch to use some list of old render targets without clearing them
		/// </summary>
		public static void UseOldRenderTargets(this GraphicsDevice graphicsDevice, RenderTargetBinding[] oldRenderTargets) {
			bool anyOldTargets = (oldRenderTargets?.Length ?? 0) != 0;
			RenderTargetUsage[] renderTargetUsage = [];
			try {
				if (anyOldTargets) {
					renderTargetUsage = new RenderTargetUsage[oldRenderTargets.Length];
					for (int i = 0; i < oldRenderTargets.Length; i++) {
						RenderTarget2D renderTarget = (RenderTarget2D)oldRenderTargets[i].RenderTarget;
						renderTargetUsage[i] = renderTarget.RenderTargetUsage;
						GraphicsMethods.SetRenderTargetUsage(renderTarget, RenderTargetUsage.PreserveContents);
					}
				} else {
					renderTargetUsage = [graphicsDevice.PresentationParameters.RenderTargetUsage];
					graphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
				}
				graphicsDevice.SetRenderTargets(oldRenderTargets);
			} finally {
				if (anyOldTargets) {
					for (int i = 0; i < oldRenderTargets.Length; i++) {
						GraphicsMethods.SetRenderTargetUsage((RenderTarget2D)oldRenderTargets[i].RenderTarget, renderTargetUsage[i]);
					}
				} else {
					graphicsDevice.PresentationParameters.RenderTargetUsage = renderTargetUsage[0];
				}
			}
		}
	}
}
