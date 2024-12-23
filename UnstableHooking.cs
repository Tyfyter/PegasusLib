using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace PegasusLib {
	/// <summary>
	/// For hooking methods which 
	/// </summary>
	public static class UnstableHooking {
		public static event ILContext.Manipulator IL_Main_DoDraw {
			add => Main.QueueMainThreadAction(() => IL_Main.DoDraw += value);
			remove => Main.QueueMainThreadAction(() => IL_Main.DoDraw -= value);
		}
	}
}
