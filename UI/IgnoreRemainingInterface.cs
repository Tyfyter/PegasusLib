using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PegasusLib.UI {
	public class IgnoreRemainingInterface : ModSystem {
		public static void Activate() => active = true;
		static bool active;
		public override void Load() {
			try {
				MonoModHooks.Add(typeof(PlayerInput).GetProperty(nameof(PlayerInput.IgnoreMouseInterface)).GetGetMethod(), (orig_IgnoreMouseInterface orig) => {
					return active || orig();
				});
			} catch (Exception e) {
				PegasusLib.FeatureError(LibFeature.IgnoreRemainingInterface, e);
			}
		}
		public override void PostDrawInterface(SpriteBatch spriteBatch) {
			active = false;
		}
		delegate bool orig_IgnoreMouseInterface();
	}
}
