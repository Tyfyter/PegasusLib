using Terraria;
using Terraria.ModLoader;

namespace PegasusLib {
	public interface IShadedProjectile {
		public int Shader { get; }
	}
	public class ShadedProjectile : ILoadable {
		public void Load(Mod mod) {
			On_Main.GetProjectileDesiredShader += (orig, projectile) => {
				if (projectile.ModProjectile is IShadedProjectile shadedProjectile) {
					return shadedProjectile.Shader;
				}
				return orig(projectile);
			};
		}
		public void Unload() { }
	}
}
