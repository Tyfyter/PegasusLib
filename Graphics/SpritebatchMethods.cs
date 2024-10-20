using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PegasusLib.Reflection;
using System;

namespace PegasusLib.Graphics {
	internal class SpritebatchMethods : ReflectionLoader {
		public override Type HostType => GetType();
		public static FastFieldInfo<SpriteBatch, bool> beginCalled { get; private set; }
		public static FastFieldInfo<SpriteBatch, SpriteSortMode> sortMode { get; private set; }
		public static FastFieldInfo<SpriteBatch, Effect> customEffect { get; private set; }
		public static FastFieldInfo<SpriteBatch, Matrix> transformMatrix { get; private set; }
	}
	public static class SpritebatchExt {
		public static SpriteBatchState GetState(this SpriteBatch spriteBatch) {
			return new SpriteBatchState(
				SpritebatchMethods.sortMode.GetValue(spriteBatch),
				spriteBatch.GraphicsDevice.BlendState,
				spriteBatch.GraphicsDevice.SamplerStates[0],
				spriteBatch.GraphicsDevice.DepthStencilState,
				spriteBatch.GraphicsDevice.RasterizerState,
				SpritebatchMethods.customEffect.GetValue(spriteBatch),
				SpritebatchMethods.transformMatrix.GetValue(spriteBatch)
			);
		}
		public static void Restart(this SpriteBatch spriteBatch, SpriteBatchState spriteBatchState, SpriteSortMode? sortMode = null, BlendState blendState = null, SamplerState samplerState = null, RasterizerState rasterizerState = null, Effect effect = null, Matrix? transformMatrix = null) {
			spriteBatch.End();
			spriteBatch.Begin(
				sortMode ?? spriteBatchState.sortMode,
				blendState ?? spriteBatchState.blendState,
				samplerState ?? spriteBatchState.samplerState,
				spriteBatchState.depthStencilState,
				rasterizerState ?? spriteBatchState.rasterizerState,
				effect ?? spriteBatchState.effect,
				transformMatrix ?? spriteBatchState.transformMatrix
			);
		}
	}
	public record SpriteBatchState(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState blendState = null, SamplerState samplerState = null, DepthStencilState depthStencilState = null, RasterizerState rasterizerState = null, Effect effect = null, Matrix transformMatrix = default);
}
