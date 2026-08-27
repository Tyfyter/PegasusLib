using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Reflection;
using Terraria;
using Terraria.Graphics;
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
		public static SpritebatchOverride OverrideState(this SpriteBatch spriteBatch, SpriteSortMode? sortMode = null, BlendState blendState = null, SamplerState samplerState = null, RasterizerState rasterizerState = null, Effect effect = null, Matrix? transformMatrix = null) {
			if (spriteBatch.IsRunning()) {
				SpritebatchOverride @override = new(spriteBatch, true);
				spriteBatch.Restart(@override.state, sortMode, blendState, samplerState, rasterizerState, effect, transformMatrix);
				return @override;
			} else {
				SpritebatchOverride @override = new(spriteBatch, false);
				spriteBatch.Begin(
					sortMode ?? SpriteSortMode.Deferred,
					blendState ?? BlendState.AlphaBlend,
					samplerState ?? SamplerState.PointClamp,
					DepthStencilState.None,
					rasterizerState ?? RasterizerState.CullNone,
					effect,
					transformMatrix ?? Main.Transform
				);
				return @override;
			}
		}
		static readonly FastFieldInfo<TileBatch, SpriteBatch> _spriteBatch = "_spriteBatch";
		public static TilebatchOverride OverrideState(this TileBatch tileBatch, SpriteSortMode? sortMode = null, BlendState blendState = null, SamplerState samplerState = null, RasterizerState rasterizerState = null, Effect effect = null, Matrix? transformMatrix = null) {
			tileBatch.End();
			TilebatchOverride @override = new(tileBatch);
			tileBatch.Begin(
				sortMode ?? @override.state.sortMode,
				blendState ?? @override.state.blendState,
				samplerState ?? @override.state.samplerState,
				@override.state.depthStencilState,
				rasterizerState ?? @override.state.rasterizerState,
				effect ?? @override.state.effect,
				transformMatrix ?? @override.state.transformMatrix
			);
			return @override;
		}
		public readonly ref struct SpritebatchOverride(SpriteBatch spriteBatch, bool wasRunning) {
			public readonly SpriteBatchState state = spriteBatch.GetState();
			public void Dispose() {
				if (wasRunning) spriteBatch.Restart(state);
				else spriteBatch.End();
			}
		}
		public readonly ref struct TilebatchOverride(TileBatch tileBatch) {
			public readonly SpriteBatchState state = _spriteBatch.GetValue(tileBatch).GetState();
			public void Dispose() {
				tileBatch.End();
				tileBatch.Begin(state.sortMode, state.blendState, state.samplerState, state.depthStencilState, state.rasterizerState, state.effect, state.transformMatrix);
			}
		}
	}
}
