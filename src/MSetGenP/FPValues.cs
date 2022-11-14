using MSS.Common;
using MSS.Types.DataTransferObjects;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MSetGenP
{
	public class FPValues : ICloneable
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

			//SignsMemory = new Memory<bool>(Signs);
			//ExponentsMemory = new Memory<short>(Exponents);
			MantissaVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(FPValuesDto fPValuesDto)
		{
			Mantissas = fPValuesDto.GetValues(out var signs, out var exponents);
			Signs = signs;
			Exponents = exponents;

			//SignsMemory = new Memory<bool>(Signs);
			//ExponentsMemory = new Memory<short>(Exponents);
			MantissaVectors = BuildMantissaMemoryVectors(Mantissas);
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
			MantissaVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(bool[] signs, Vector<ulong>[][] mantissVectors, short[] exponents)
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

			var mantissas = new ulong[mantissVectors.Length][];

			for(var i = 0; i < mantissVectors.Length; i++)
			{
				mantissas[i] = MemoryMarshal.Cast<Vector<ulong>, ulong>(mantissVectors[i]).ToArray();
			}

			for (int i = 0; i < mantissas.Length; i++)
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
			MantissaVectors = BuildMantissaMemoryVectors(Mantissas);
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

			MantissaVectors = BuildMantissaMemoryVectors(Mantissas);
		}

		private Vector<ulong>[][] BuildMantissaMemoryVectors(ulong[][] mantissas)
		{
			var result = new Vector<ulong>[LimbCount][];

			for (var j = 0; j < LimbCount; j++)
			{
				var vecArray = MemoryMarshal.Cast<ulong, Vector<ulong>>(mantissas[j]);
				result[j] = vecArray.ToArray();
			}

			return result;
		}

		#endregion

		#region Public Properties

		public int Length => Signs.Length;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Length == 0 ? 0 : MantissaVectors[0].Length;

		public bool[] Signs { get; init; }
		public ulong[][] Mantissas { get; init; } 
		public short[] Exponents { get; init; }

		//public Memory<bool> SignsMemory { get; init; }
		public Vector<ulong>[][] MantissaVectors { get; init; }
		//public Memory<short> ExponentsMemory { get; init; }

		#endregion

		#region Public Methods

		public IEnumerator<Vector<ulong>> GetInPlayEnumerator(int limbIndex)
		{
			return new InPlayEnumerator<Vector<ulong>>(MantissaVectors[limbIndex]);
		}

		public IEnumerator<ValueTuple<Vector<ulong>, Vector<ulong>>> GetInPlayEnumerator(int limbIndexA, int limbIndexB)
		{
			var result = new InPlayPairsEnumerator<Vector<ulong>>(MantissaVectors[limbIndexA], MantissaVectors[limbIndexB]);

			return result;
		}


		public SequenceReader<Vector<ulong>> GetSequenceReader(int limbIndex)
		{
			var ros = new ReadOnlySequence<Vector<ulong>>(MantissaVectors[limbIndex]);

			//var seg1 = 
			//var xx = new ReadOnlySequence<Vector<ulong>>()

			var result = new SequenceReader<Vector<ulong>>(ros);

			return result;
		}

		public Smx CreateSmx(int index, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var result = new Smx(Signs[index], GetMantissa(index), Exponents[index], precision);
			return result;
		}

		private ulong[] GetMantissa(int index)
		{
			//var numberOfLimbs = Mantissas.Length;
			//var result = new ulong[numberOfLimbs];

			//for (var i = 0; i < numberOfLimbs; i++)
			//{
			//	result[i] = Mantissas[i][index];
			//}

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
