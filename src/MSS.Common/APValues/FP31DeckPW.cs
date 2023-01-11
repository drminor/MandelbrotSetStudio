using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Common.APValues
{
	public class FP31DeckPW : ICloneable
	{
		#region Constructors

		public FP31DeckPW(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
		{ }

		private FP31DeckPW(ulong[][] mantissas)
		{
			Mantissas = mantissas;
			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		public FP31DeckPW(FP31Val[] fp31Vals)
		{
			var numberOfLimbs = fp31Vals[0].LimbCount;
			Mantissas = new ulong[numberOfLimbs][];

			for (var j = 0; j < numberOfLimbs; j++)
			{
				Mantissas[j] = new ulong[fp31Vals.Length];

				for (var i = 0; i < fp31Vals.Length; i++)
				{
					Mantissas[j][i] = fp31Vals[i].Mantissa[j];
				}
			}

			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		private static ulong[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new ulong[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new ulong[valueCount];
			}

			return result;
		}

		private Memory<ulong>[] BuildMantissaMemoryVectors(ulong[][] mantissas)
		{
			var result = new Memory<ulong>[mantissas.Length];

			for (var i = 0; i < mantissas.Length; i++)
			{
				result[i] = new Memory<ulong>(mantissas[i]);
			}

			return result;
		}

		#endregion

		#region Public Properties

		public readonly int Lanes = Vector256<ulong>.Count;

		public int Length => Mantissas[0].Length;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Length / Lanes;

		public ulong[][] Mantissas { get; init; } 
		public Memory<ulong>[] MantissaMemories { get; init; }

		#endregion

		#region Public Methods

		//private ulong[] GetMantissa(int index)
		//{
		//	var result = Mantissas.Select(x => x[index]).ToArray();
		//	return result;
		//}

		//private void SetMantissa(int index, ulong[] values)
		//{
		//	for(var i = 0; i < values.Length; i++)
		//	{
		//		Mantissas[i][index] = values[i];
		//	}
		//}

		public Span<Vector256<ulong>> GetLimbVectorsUL(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(x.Span);

			return result;
		}

		public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(x.Span);

			return result;
		}

		public void ClearManatissMems(int[] inPlayListNarrow)
		{
			var indexes = inPlayListNarrow;

			for (var j = 0; j < MantissaMemories.Length; j++)
			{
				var vectors = GetLimbVectorsUL(j);

				for (var i = 0; i < indexes.Length; i++)
				{
					vectors[indexes[i]] = Vector256<ulong>.Zero;
				}
			}
		}

		public void ClearManatissMems()
		{
			for (var j = 0; j < MantissaMemories.Length; j++)
			{
				var vectors = GetLimbVectorsUL(j);

				for (var i = 0; i < vectors.Length; i++)
				{
					vectors[i] = Vector256<ulong>.Zero;
				}
			}
		}

		//private void ClearBackingArray(ulong[][] backingArray, bool onlyInPlayItems)
		//{
		//	if (onlyInPlayItems)
		//	{
		//		var template = new ulong[_lanes];

		//		var indexes = InPlayListNarrow;

		//		for (var j = 0; j < backingArray.Length; j++)
		//		{
		//			for (var i = 0; i < indexes.Length; i++)
		//			{
		//				Array.Copy(template, 0, backingArray[j], indexes[i] * _lanes, _lanes);
		//			}
		//		}
		//	}
		//	else
		//	{
		//		var vc = backingArray[0].Length;

		//		for (var j = 0; j < backingArray.Length; j++)
		//		{
		//			for (var i = 0; i < vc; i++)
		//			{
		//				backingArray[j][i] = 0;
		//			}
		//		}
		//	}
		//}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FP31DeckPW Clone()
		{
			var result = new FP31DeckPW(CloneMantissas());
			return result;
		}

		private ulong[][] CloneMantissas()
		{
			ulong[][] result = new ulong[Mantissas.Length][];
			
			for(var i = 0; i < Mantissas.Length; i++)
			{
				var a = new ulong[Mantissas[i].Length];

				Array.Copy(Mantissas[i], a, a.Length);

				result[i] = a;
			}

			return result;
		}

		#endregion
	}
}
