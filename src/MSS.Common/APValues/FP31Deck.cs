using MSS.Common.APValSupport;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Common.APValues
{
	public class FP31Deck : ICloneable
	{
		private const ulong TEST_BIT_30 = 0x0000000040000000; // bit 30 is set.

		#region Constructors

		public FP31Deck(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
		{ }

		public FP31Deck(uint[][] mantissas)
		{
			// ValueArrays[0] = Least Significant Limb (Number of 1's)
			// ValuesArray[1] = Number of 2^31
			// ValuesArray[2] = Number of 2^62
			// ValuesArray[n] = Most Significant Limb

			var len = mantissas[0].Length;

			for(int i = 1; i < mantissas.Length; i++)
			{
				if (mantissas[i].Length != len)
				{
					throw new ArgumentException($"The number of values in the {i}th array of mantissas is differnt from the number of exponents.");
				}
			}

			Mantissas = mantissas;
			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		//public FP31Deck(Smx[] smxes)
		//{
		//	var numberOfLimbs = smxes[0].Mantissa.Length;
		//	Mantissas = new uint[numberOfLimbs][];

		//	for (var i = 0; i < smxes.Length; i++)
		//	{
		//		if (smxes[i].Sign)
		//		{
		//			var lows = FP31ValHelper.TakeLowerHalves(smxes[i].Mantissa);
		//			SetMantissa(i, lows);
		//		}
		//		else
		//		{
		//			var non2CPWLimbs = ScalarMathHelper.Toggle2C(smxes[i].Mantissa, includeTopHalves: false);

		//			var lows = FP31ValHelper.TakeLowerHalves(non2CPWLimbs);

		//			SetMantissa(i, lows);
		//		}
		//	}

		//	MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		//}

		public FP31Deck(Smx2C[] smxes)
		{
			var numberOfLimbs = smxes[0].LimbCount;
			Mantissas = new uint[numberOfLimbs][];

			for (var j = 0; j < numberOfLimbs; j++)
			{
				Mantissas[j] = new uint[smxes.Length];

				for (var i = 0; i < smxes.Length; i++)
				{
					Mantissas[j][i] = (uint) smxes[i].Mantissa[j];
				}
			}

			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		#endregion

		#region Public Properties

		public readonly int Lanes = Vector256<uint>.Count;

		public int Length => Mantissas[0].Length;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Length / Lanes;

		public uint[][] Mantissas { get; init; } 
		public Memory<uint>[] MantissaMemories { get; init; }

		#endregion

		#region Public Methods

		public bool[] GetSigns()
		{
			var result = Mantissas[^1].Select(x => (x & TEST_BIT_30) == 0).ToArray();
			return result;
		}

		//public bool GetSign(int index)
		//{
		//	var result = (Mantissas[^1][index] & TEST_BIT_30) == 0;
		//	return result;
		//}

		public FP31Deck Negate(int[] inPlayList)
		{
			var result = Clone();

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var resultPtr = idx * Lanes;

				for (var i = 0; i < Lanes; i++)
				{
					var valPtr = resultPtr + i;
					var limbs = result.GetMantissa(valPtr);

					var extendedLimbs = FP31ValHelper.ExtendToPartialWords(limbs);

					var non2CPWLimbs = ScalarMathHelper.Toggle2C(extendedLimbs, includeTopHalves: false);

					var lows = FP31ValHelper.TakeLowerHalves(non2CPWLimbs);

					result.SetMantissa(valPtr, lows);
				}
			}

			return result;
		}

		public FPValues ConvertFrom2C(int[] inPlayList, int numberOfLanes)
		{
			var result = new FPValues(LimbCount, Length);

			var signs = GetSigns();

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var resultPtr = idx * numberOfLanes;

				for (var i = 0; i < numberOfLanes; i++)
				{
					var valPtr = resultPtr + i;

					var limbs = GetMantissa(valPtr);
					var partialWordLimbs = FP31ValHelper.ExtendToPartialWords(limbs);

					if (!signs[valPtr])
					{
						var non2CPWLimbs = ScalarMathHelper.Toggle2C(partialWordLimbs, includeTopHalves: false);
						result.SetMantissa(valPtr, non2CPWLimbs);
					}
					else
					{
						result.SetMantissa(valPtr, partialWordLimbs);
					}
				}
			}

			return result;
		}

		public void UpdateFrom(FPValues fPValues, int[] inPlayList, int numberOfLanes)
		{
			var indexes = inPlayList;

			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var resultPtr = idx * numberOfLanes;

				for (var i = 0; i < numberOfLanes; i++)
				{
					var valPtr = resultPtr + i;

					var limbs = fPValues.GetMantissa(valPtr);

					var lows = FP31ValHelper.TakeLowerHalves(limbs);

					SetMantissa(valPtr, lows);
				}
			}
		}

		private uint[] GetMantissa(int index)
		{
			var result = Mantissas.Select(x => x[index]).ToArray();
			return result;
		}

		private void SetMantissa(int index, uint[] values)
		{
			for(var i = 0; i < values.Length; i++)
			{
				Mantissas[i][index] = values[i];
			}
		}

		#endregion

		#region Mantissa Support

		public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<uint>> result = MemoryMarshal.Cast<uint, Vector256<uint>>(x.Span);

			return result;
		}

		//public Span<Vector256<uint>> GetLimbVectorsUWExpanded(int limbIndex)
		//{
		//	var limb = Mantissas[limbIndex];
		//	var partialWordLimb = limb.Select(i => (ulong)i).ToArray();
		//	var x = new Memory<ulong>(partialWordLimb);

		//	Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(x.Span);

		//	Debug.Assert(result.Length == 2 * limb.Length, "GetLimbUWExpanded did not double the number of elements.");

		//	return result;
		//}

		private static uint[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new uint[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new uint[valueCount];
			}

			return result;
		}

		private static Memory<uint>[] BuildMantissaMemoryVectors(uint[][] mantissas)
		{
			var result = new Memory<uint>[mantissas.Length];

			for (var i = 0; i < mantissas.Length; i++)
			{
				result[i] = new Memory<uint>(mantissas[i]);
			}

			return result;
		}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FP31Deck Clone()
		{
			var result = new FP31Deck(CloneMantissas());
			return result;
		}

		private uint[][] CloneMantissas()
		{
			uint[][] result = new uint[Mantissas.Length][];
			
			for(var i = 0; i < Mantissas.Length; i++)
			{
				var a = new uint[Mantissas[i].Length];

				Array.Copy(Mantissas[i], a, a.Length);

				result[i] = a;
			}

			return result;
		}

		#endregion
	}
}
