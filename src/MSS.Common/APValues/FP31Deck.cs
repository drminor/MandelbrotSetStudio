﻿using System;
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

		private static uint[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new uint[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new uint[valueCount];
			}

			return result;
		}

		private Memory<uint>[] BuildMantissaMemoryVectors(uint[][] mantissas)
		{
			var result = new Memory<uint>[mantissas.Length];

			for (var i = 0; i < mantissas.Length; i++)
			{
				result[i] = new Memory<uint>(mantissas[i]);
			}

			return result;
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

		//private uint[] GetMantissa(int index)
		//{
		//	var result = Mantissas.Select(x => x[index]).ToArray();
		//	return result;
		//}

		//private void SetMantissa(int index, uint[] values)
		//{
		//	for(var i = 0; i < values.Length; i++)
		//	{
		//		Mantissas[i][index] = values[i];
		//	}
		//}

		public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<uint>> result = MemoryMarshal.Cast<uint, Vector256<uint>>(x.Span);

			return result;
		}

		public void ClearManatissMems(int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var j = 0; j < MantissaMemories.Length; j++)
			{
				var vectors = GetLimbVectorsUW(j);

				for (var i = 0; i < indexes.Length; i++)
				{
					vectors[indexes[i]] = Vector256<uint>.Zero;
				}
			}
		}

		public void ClearManatissMems()
		{
			for (var j = 0; j < MantissaMemories.Length; j++)
			{
				var vectors = GetLimbVectorsUW(j);

				for (var i = 0; i < vectors.Length; i++)
				{
					vectors[i] = Vector256<uint>.Zero;
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
