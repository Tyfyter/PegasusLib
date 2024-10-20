using ReLogic.Content;
using Terraria.ModLoader;
using Terraria;

namespace PegasusLib {
	public readonly struct AutoCastingAsset<T> where T : class {
		public bool HasValue => asset is not null;
		public bool IsLoaded => asset?.IsLoaded ?? false;
		public T Value => asset.Value;
		readonly Asset<T> asset;
		AutoCastingAsset(Asset<T> asset) {
			this.asset = asset;
		}
		public static implicit operator AutoCastingAsset<T>(Asset<T> asset) => new(asset);
		public static implicit operator T(AutoCastingAsset<T> asset) => asset.Value;
	}
	public struct AutoLoadingAsset<T> : IUnloadable where T : class {
		public readonly bool IsLoaded => asset.Value?.IsLoaded ?? false;
		public T Value {
			get {
				LoadAsset();
				return asset.Value?.Value;
			}
		}
		public bool Exists {
			get {
				LoadAsset();
				return exists;
			}
		}
		bool exists;
		bool triedLoading;
		string assetPath;
		Ref<Asset<T>> asset;
		AutoLoadingAsset(Asset<T> asset) {
			triedLoading = false;
			assetPath = "";
			this.asset = new(asset);
			exists = false;
			this.RegisterForUnload();
		}
		AutoLoadingAsset(string asset) {
			triedLoading = false;
			assetPath = asset;
			this.asset = new(null);
			exists = false;
			this.RegisterForUnload();
		}
		public void Unload() {
			assetPath = null;
			asset = null;
		}
		public void LoadAsset() {
			if (!triedLoading) {
				triedLoading = true;
				if (assetPath is null) {
					asset.Value = Asset<T>.Empty;
				} else {
					if (!Main.dedServ) {
						exists = ModContent.RequestIfExists(assetPath, out Asset<T> foundAsset);
						asset.Value = exists ? foundAsset : Asset<T>.Empty;
					} else {
						asset.Value = Asset<T>.Empty;
					}
				}
			}
		}
		public static implicit operator AutoLoadingAsset<T>(Asset<T> asset) => new(asset);
		public static implicit operator AutoLoadingAsset<T>(string asset) => new(asset);
		public static implicit operator T(AutoLoadingAsset<T> asset) => asset.Value;
		public static implicit operator AutoCastingAsset<T>(AutoLoadingAsset<T> asset) {
			asset.LoadAsset();
			return asset.asset.Value;
		}
		public static implicit operator Asset<T>(AutoLoadingAsset<T> asset) {
			asset.LoadAsset();
			return asset.asset.Value;
		}
	}
}
