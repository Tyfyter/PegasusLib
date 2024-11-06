using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;

namespace PegasusLib.Graphics {
	public class ShaderLayerTargetHandler : IUnloadable {
		internal RenderTarget2D renderTarget;
		internal RenderTarget2D oldRenderTarget;
		SpriteBatchState spriteBatchState;
		SpriteBatch spriteBatch;
		bool capturing = false;
		bool spriteBatchWasRunning = false;
		RenderTargetBinding[] oldRenderTargets = [];
		Rectangle oldScissorRectangle;
		public bool Capturing {
			get => capturing;
			private set {
				if (value == capturing) return;
				if (value) {
					Main.OnPostDraw += Reset;
				} else {
					Main.OnPostDraw -= Reset;
				}
				capturing = value;
			}
		}
		/// <summary>
		/// Begins capturing whatever is drawn to the spritebatch
		/// When used in a global, should be placed in the respective IDraw____Effect.PrepareToDraw____ hook, not PreDraw
		/// </summary>
		/// <param name="spriteBatch">the SpriteBatch to be used, leave as null to use <see cref="Main.spriteBatch"/></param>
		public void Capture(SpriteBatch spriteBatch = null) {
			if (Main.dedServ) return;
			SetupRenderTargets();
			Capturing = true;
			this.spriteBatch = spriteBatch ??= Main.spriteBatch;
			oldScissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;
			if (SpritebatchMethods.beginCalled.GetValue(this.spriteBatch)) {
				spriteBatchWasRunning = true;
				spriteBatchState = this.spriteBatch.GetState();
				this.spriteBatch.Restart(spriteBatchState, SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, Main.Rasterizer, null, Main.Transform);
			} else {
				spriteBatchWasRunning = false;
				spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
				spriteBatchState = this.spriteBatch.GetState();
			}
			oldRenderTargets = Main.graphics.GraphicsDevice.GetRenderTargets();
			Main.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
			Main.graphics.GraphicsDevice.Clear(Color.Transparent);
		}
		/// <summary>
		/// Applies the shader to everything captured
		/// </summary>
		/// <param name="shader"></param>
		/// <param name="entity">the Entity to be used in <see cref="ArmorShaderData.Apply"/></param>
		public void Stack(ArmorShaderData shader, Entity entity = null) {
			if (Main.dedServ) return;
			Utils.Swap(ref renderTarget, ref oldRenderTarget);
			Main.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
			Main.graphics.GraphicsDevice.Clear(Color.Transparent);
			spriteBatch.Restart(spriteBatchState, SpriteSortMode.Immediate, transformMatrix: Matrix.Identity);
			DrawData data = new(oldRenderTarget, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None);
			shader.Apply(entity, data);
			data.Draw(spriteBatch);
		}
		/// <summary>
		/// Sets the spritebatch to its state before <see cref="Capture"/> was called, then draws everything that was captured
		/// When used in a global, should be placed in the respective IDraw____Effect.FinishDrawing____ hook, not PostDraw
		/// </summary>
		public void Release() {
			if (Main.dedServ) return;
			Capturing = false;
			spriteBatch.Restart(spriteBatchState, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, RasterizerState.CullNone, null, Matrix.Identity);
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
					renderTargetUsage = [Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage];
					Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
				}
				Main.graphics.GraphicsDevice.SetRenderTargets(oldRenderTargets);
			} finally {
				if (anyOldTargets) {
					for (int i = 0; i < oldRenderTargets.Length; i++) {
						GraphicsMethods.SetRenderTargetUsage((RenderTarget2D)oldRenderTargets[i].RenderTarget, renderTargetUsage[i]);
					}
				} else {
					Main.graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = renderTargetUsage[0];
				}
			}
			spriteBatch.GraphicsDevice.ScissorRectangle = oldScissorRectangle;
			spriteBatch.Draw(renderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0);
			spriteBatch.Restart(spriteBatchState);
			if (!spriteBatchWasRunning) spriteBatch.End();
		}
		public void Reset(GameTime _) {
			if (Main.dedServ) return;
			Capturing = false;
			if (spriteBatchWasRunning) {
				spriteBatch.Restart(spriteBatchState);
			} else {
				spriteBatch.End();
			}
			Main.graphics.GraphicsDevice.SetRenderTargets(oldRenderTargets);
		}
		public ShaderLayerTargetHandler() {
			if (Main.dedServ) return;
			this.RegisterForUnload();
			Main.QueueMainThreadAction(SetupRenderTargets);
			Main.OnResolutionChanged += Resize;
		}
		public void Resize(Vector2 _) {
			if (Main.dedServ) return;
			renderTarget.Dispose();
			oldRenderTarget.Dispose();
			SetupRenderTargets();
		}
		void SetupRenderTargets() {
			if (renderTarget is not null && !renderTarget.IsDisposed) return;
			renderTarget = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
			oldRenderTarget = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		}
		public void Unload() {
			Main.QueueMainThreadAction(() => {
				renderTarget.Dispose();
				oldRenderTarget.Dispose();
			});
			Main.OnResolutionChanged -= Resize;
		}
	}
}
