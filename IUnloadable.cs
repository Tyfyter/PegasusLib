namespace PegasusLib {
	public interface IUnloadable {
		void Unload();
	}
	public static class UnloadableExt {
		public static void RegisterForUnload(this IUnloadable unloadable) {
			PegasusLib.unloadables.Add(unloadable);
		}
	}
}
