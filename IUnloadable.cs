namespace PegasusLib {
	public interface IUnloadable {
		void Unload();
	}
	public static class UnloadableExt {
		public static void RegisterForUnload(this IUnloadable unloadable) {
			if (PegasusLib.unloadables is null) return;
			PegasusLib.unloadables.Add(unloadable);
		}
	}
}
