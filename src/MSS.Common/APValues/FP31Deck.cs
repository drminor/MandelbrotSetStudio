using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Common.APValues
{
	public class FP31Deck : ICloneable
	{
		#region Constructors

		public FP31Deck(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
		{ }

		private FP31Deck(uint[][] mantissas)
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

		public FP31Deck(FP31Val[] fp31Vals)
		{
			var numberOfLimbs = fp31Vals[0].LimbCount;
			Mantissas = new uint[numberOfLimbs][];

			for (var j = 0; j < numberOfLimbs; j++)
			{
				Mantissas[j] = new uint[fp31Vals.Length];

				for (var i = 0; i < fp31Vals.Length; i++)
				{
					Mantissas[j][i] = fp31Vals[i].Mantissa[j];
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

		public uint[] GetMantissa(int index)
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
