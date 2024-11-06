using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Mono.Cecil.Cil;

namespace PegasusLib.Reflection {
	public class MonoModMethods : ILoadable {
		public delegate void _ComputeStackDelta(Instruction instruction, ref int stack_size);
		public static _ComputeStackDelta ComputeStackDelta { get; private set; }
		public void Load(Mod mod) {
			ComputeStackDelta = typeof(Code).Assembly.GetType("Mono.Cecil.Cil.CodeWriter").GetMethod(nameof(ComputeStackDelta), BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate<_ComputeStackDelta>();
		}
		public void Unload() {
			ComputeStackDelta = null;
		}
		public static int SkipPrevArgument(ILCursor c) {
			int count = 0;
			int delta = 0;
			do {
				count++;
				ComputeStackDelta(c.Prev, ref delta);
				c.Index--;
			} while (delta != 1);
			return count;
		}
	}
}