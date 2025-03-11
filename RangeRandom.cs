using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Terraria;
using Terraria.Utilities;

namespace PegasusLib {
	public class RangeRandom {
		private readonly UnifiedRandom random;
		readonly double[] weights;
		//readonly double[] weightPositions;
		double totalWeight = 0;
		public bool AnyWeight => totalWeight != 0;
		public RangeRandom(UnifiedRandom random, int start, int end) {
			this.random = random;
			weights = new double[end - start];
			Reset();
			//weightPositions = new double[end - start];
			Start = start;
			End = end;
		}

		public int Start { get; }
		public int End { get; }
		public void Reset() {
			totalWeight = 0;
			for (int i = 0; i < weights.Length; i++) {
				weights[i] = 1;
				totalWeight += 1;
			}
		}
		public void Multiply(int start, int end, double weight) {
			for (int i = start; i < end; i++) {
				int index = i - Start;
				if (!weights.IndexInRange(index)) continue;
				double oldWeight = weights[i - Start];
				weights[index] *= weight;
				totalWeight += weights[i - Start] - oldWeight;
			}
			//Recalculate();
		}
		void Recalculate() {
			totalWeight = 0;
			for (int i = 0; i < weights.Length; i++) {
				totalWeight += weights[i];
				//weightPositions[i] = totalWeight;
			}
		}
		public int Get() {
			double num = random.NextDouble();
			num *= totalWeight;
			int i;
			for (i = 0; i < weights.Length; i++) {
				if (num > weights[i]) {
					num -= weights[i];
					continue;
				}
				return i + Start;
			}
			return i + Start;
		}
		public double GetWeight(int position) => weights[position - Start];
	}
}
