using MSS.Common;
using MSS.Types.DataTransferObjects;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGenP
{
	public class FPValues : ICloneable
	{
		#region Constructors

		public FPValues(int limbCount, int valueCount) 
			: this(Enumerable.Repeat(true, valueCount).ToArray(), BuildLimbs(limbCount, valueCount), new short[valueCount])
		{ }

		public FPValues(bool[] signs, short[] exponents, int limbCount) 
			: this(signs, BuildLimbs(limbCount, signs.Length), exponents)
		{ }

		private static ulong[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new ulong[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new ulong[valueCount];
			}

			return result;
		}

		public FPValues(bool[] signs, ulong[][] mantissas, short[] exponents)
		{
			// ValueArrays[0] = Least Significant Limb (Number of 1's)
			// ValuesArray[1] = Number of 2^64s
			// ValuesArray[2] = Number of 2^128s
			// ValuesArray[n] = Most Significant Limb

			var len = exponents.Length;	
			if (signs.Length != len)
			{
				throw new ArgumentException("The number of sign values is different from the number of exponentsl.");
			}

			for(int i = 0; i < mantissas.Length; i++)
			{
				if (mantissas[i].Length != len)
				{
					throw new ArgumentException($"The number of values in the {i}th array of mantissas is differnt from the number of exponents.");
				}
			}

			Signs = signs;
			Mantissas = mantissas;
			Exponents = exponents;

			//SignsMemory = new Memory<bool>(Signs);
			//ExponentsMemory = new Memory<short>(Exponents);
			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		//public FPValues(bool[] signs, Vector<ulong>[][] mantissaVectors, short[] exponents)
		//{
		//	// ValueArrays[0] = Least Significant Limb (Number of 1's)
		//	// ValuesArray[1] = Number of 2^64s
		//	// ValuesArray[2] = Number of 2^128s
		//	// ValuesArray[n] = Most Significant Limb

		//	var len = exponents.Length;
		//	if (signs.Length != len)
		//	{
		//		throw new ArgumentException("The number of sign values is different from the number of exponentsl.");
		//	}

		//	Signs = signs;

		//	Mantissas = GetMantissasFromVectors(mantissaVectors);

		//	for(var i = 0; i < Mantissas.Length; i++)
		//	{
		//		if (Mantissas[i].Length != len)
		//		{
		//			throw new ArgumentException($"The number of values in the {i}th array of mantissas is differnt from the number of exponents.");
		//		}
		//	}

		//	Exponents = exponents;

		//	//SignsMemory = new Memory<bool>(Signs);
		//	//ExponentsMemory = new Memory<short>(Exponents);
		//	MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		//}

		public FPValues(FPValuesDto fPValuesDto)
		{
			Mantissas = fPValuesDto.GetValues(out var signs, out var exponents);
			Signs = signs;
			Exponents = exponents;

			//SignsMemory = new Memory<bool>(Signs);
			//ExponentsMemory = new Memory<short>(Exponents);
			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}
		
		public FPValues(Smx[] smxes)
		{
			Signs = new bool[smxes.Length];
			Exponents = new short[smxes.Length];

			//SignsMemory = new Memory<bool>(Signs);
			//ExponentsMemory = new Memory<short>(Exponents);

			for (var i = 0; i < smxes.Length; i++)
			{
				Signs[i] = smxes[i].Sign;
				Exponents[i] = (short)smxes[i].Exponent;
			}

			var numberOfLimbs = smxes[0].Mantissa.Length;
			Mantissas = new ulong[numberOfLimbs][];

			for (var j = 0; j < numberOfLimbs; j++)
			{
				Mantissas[j] = new ulong[smxes.Length];

				for (var i = 0; i < smxes.Length; i++)
				{
					Mantissas[j][i] = smxes[i].Mantissa[j];
				}
			}

			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		//public static ulong[][] GetMantissasFromVectors(Vector<ulong>[][] mantissaVectors)
		//{
		//	var result = new ulong[mantissaVectors.Length][];

		//	for (var i = 0; i < mantissaVectors.Length; i++)
		//	{
		//		result[i] = MemoryMarshal.Cast<Vector<ulong>, ulong>(mantissaVectors[i]).ToArray();
		//	}

		//	return result;
		//}

		//ReadOnlySpan<Vector<float>> rightVecArray = MemoryMarshal.Cast<float, Vector<float>>(rightMemory.Span);

		public static Memory<ulong>[] BuildMantissaMemoryVectors(ulong[][] mantissas)
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

		public int Length => Signs.Length;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Length / Vector<ulong>.Count;

		public bool[] Signs { get; init; }
		public ulong[][] Mantissas { get; init; } 
		public short[] Exponents { get; init; }

		//public Memory<bool> SignsMemory { get; init; }
		public Memory<ulong>[] MantissaMemories { get; init; }
		//public Memory<short> ExponentsMemory { get; init; }

		public int BitsBeforeBP { get; init; }

		#endregion

		#region Public Methods

		//public InPlayEnumerator<Vector<ulong>> GetInPlayEnumerator(int limbIndex)
		//{
		//	var mantissaVectors = GetLimbVectors(limbIndex);
		//	return new InPlayEnumerator<Vector<ulong>>(mantissaVectors);
		//}

		//public InPlayPairsEnumerator<Vector<ulong>> GetInPlayEnumerator(int limbIndexA, int limbIndexB)
		//{
		//	var mantissaVectorsA = GetLimbVectors(limbIndexA);
		//	var mantissaVectorsB = GetLimbVectors(limbIndexB);

		//	var result = new InPlayPairsEnumerator<Vector<ulong>>(mantissaVectorsA, mantissaVectorsB);

		//	return result;
		//}

		//public static Span<Vector<ulong>> GetLimbVectors(Memory<ulong>[] mantissaMemories, int limbIndex)
		//{
		//	var x = mantissaMemories[limbIndex];
		//	Span<Vector<ulong>> result = MemoryMarshal.Cast<ulong, Vector<ulong>>(x.Span);

		//	return result;
		//}

		public Span<Vector<ulong>> GetLimbVectors(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector<ulong>> result = MemoryMarshal.Cast<ulong, Vector<ulong>>(x.Span);

			return result;
		}

		public Span<Vector256<ulong>> GetLimbVectors2L(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(x.Span);

			return result;
		}

		public Span<Vector256<uint>> GetLimbVectors2S(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(x.Span);

			return result;
		}


		//public SequenceReader<Vector<ulong>> GetSequenceReader(int limbIndex)
		//{
		//	var ros = new ReadOnlySequence<Vector<ulong>>(MantissaMemories[limbIndex]);

		//	//var seg1 = 
		//	//var xx = new ReadOnlySequence<Vector<ulong>>()

		//	var result = new SequenceReader<Vector<ulong>>(ros);

		//	return result;
		//}

		public Smx CreateSmx(int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(Signs[index], GetMantissa(index), Exponents[index], precision, BitsBeforeBP);
			return result;
		}

		private ulong[] GetMantissa(int index)
		{
			var result = Mantissas.Select(x => x[index]).ToArray();
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FPValues Clone()
		{
			var result = new FPValues(
				(bool[])Signs.Clone(), 
				(ulong[][])Mantissas.Clone(), 
				(short[])Exponents.Clone()
				);

			return result;
		}

		#endregion
	}
}
