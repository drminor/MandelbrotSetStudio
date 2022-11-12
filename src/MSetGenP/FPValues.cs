using MSS.Common;
using MSS.Types.DataTransferObjects;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MSetGenP
{
	public class FPValues
	{
		#region Constructors

		public FPValues(int valuesCount, int limbsCount)
		{
			Signs = Enumerable.Repeat(true, valuesCount).ToArray();

			Exponents = new short[valuesCount];
			Mantissas = new ulong[limbsCount][];

			for (var i = 0; i < limbsCount; i++)
			{
				Mantissas[i] = new ulong[valuesCount];
			}

			SignsMemory = new Memory<bool>(Signs);
			ExponentsMemory = new Memory<short>(Exponents);
			MantissaMemoryVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(FPValuesDto fPValuesDto)
		{
			Mantissas = fPValuesDto.GetValues(out var signs, out var exponents);
			Signs = signs;
			Exponents = exponents;

			SignsMemory = new Memory<bool>(Signs);
			ExponentsMemory = new Memory<short>(Exponents);
			MantissaMemoryVectors = BuildMantissaMemoryVectors(Mantissas);
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

			SignsMemory = new Memory<bool>(Signs);
			ExponentsMemory = new Memory<short>(Exponents);
			MantissaMemoryVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(Smx[] smxes)
		{
			Signs = new bool[smxes.Length];
			Exponents = new short[smxes.Length];

			SignsMemory = new Memory<bool>(Signs);
			ExponentsMemory = new Memory<short>(Exponents);

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

			MantissaMemoryVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		private Memory<Vector<ulong>>[] BuildMantissaMemoryVectors(ulong[][] mantissas)
		{
			var numberOfLimbs = mantissas.Length;

			var result = new Memory<Vector<ulong>>[numberOfLimbs];

			for (var j = 0; j < numberOfLimbs; j++)
			{
				var vecArray = MemoryMarshal.Cast<ulong, Vector<ulong>>(Mantissas[j]);
				result[j] = new Memory<Vector<ulong>>(vecArray.ToArray());
			}

			return result;
		}

		#endregion

		#region Public Properties

		public bool[] Signs { get; init; }

		public ulong[][] Mantissas { get; init; } 

		public short[] Exponents { get; init; }


		public Memory<bool> SignsMemory { get; init; }

		public Memory<Vector<ulong>>[] MantissaMemoryVectors { get; init; }

		public Memory<short> ExponentsMemory { get; init; }

		#endregion

		#region Public Methods

		public SequenceReader<Vector<ulong>> GetSequenceReader(int limbIndex)
		{
			var ros = new ReadOnlySequence<Vector<ulong>>(MantissaMemoryVectors[limbIndex]);
			var result = new SequenceReader<Vector<ulong>>(ros);

			return result;
		}

		public Smx CreateSmx(int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(Signs[index], GetMantissa(index), Exponents[index], precision);
			return result;
		}

		//public ushort Iterate(FPValues cRs, int rIndex, FPValues cIs, int iIndex)
		//{
		//	var cR = new Smx(cRs.Signs[rIndex], GetMantissa(cRs, rIndex), cRs.Exponents[rIndex], 55);
		//	var cI = new Smx(cIs.Signs[iIndex], GetMantissa(cIs, iIndex), cIs.Exponents[iIndex], 55);

		//	var result = Iterate(cR, cI);

		//	return result;
		//}

		private ulong[] GetMantissa(int index)
		{
			var numberOfLimbs = Mantissas.Length;
			var result = new ulong[numberOfLimbs];

			for (var i = 0; i < numberOfLimbs; i++)
			{
				result[i] = Mantissas[i][index];
			}

			return result;
		}

		#endregion
	}
}
