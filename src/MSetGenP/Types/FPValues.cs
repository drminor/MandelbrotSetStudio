using MSS.Types.DataTransferObjects;
using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGenP
{
	public class FPValues : ICloneable
	{
		private const ulong ALL_BITS_SET = 0xFFFFFFFFFFFFFFFF;

		#region Constructors

		public FPValues(int limbCount, int valueCount) 
			: this(Enumerable.Repeat(true, valueCount).ToArray(), BuildLimbs(limbCount, valueCount))
		{ }

		public FPValues(bool[] signs, ulong[][] mantissas)
		{
			// ValueArrays[0] = Least Significant Limb (Number of 1's)
			// ValuesArray[1] = Number of 2^64s
			// ValuesArray[2] = Number of 2^128s
			// ValuesArray[n] = Most Significant Limb

			var len = signs.Length;	

			for(int i = 0; i < mantissas.Length; i++)
			{
				if (mantissas[i].Length != len)
				{
					throw new ArgumentException($"The number of values in the {i}th array of mantissas is differnt from the number of exponents.");
				}
			}

			_signsBackingArray = signs.Select(x => x ? ALL_BITS_SET : 0L).ToArray();
			SignsMemory = new Memory<ulong>(_signsBackingArray);
			Mantissas = mantissas;

			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(FPValuesDto fPValuesDto)
		{
			Mantissas = fPValuesDto.GetValues(out var signs, out var exponents);
			//Signs = signs;

			_signsBackingArray = signs.Select(x => x ? ALL_BITS_SET : 0L).ToArray();
			SignsMemory = new Memory<ulong>(_signsBackingArray);

			MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		}

		public FPValues(Smx smx, int count) : this(Enumerable.Repeat(smx, count).ToArray())
		{ }

		//public FPValues(Smx smx, int count)
		//{
		//	var smxes = new Smx[count];

		//	for (int i = 0; i < count; i++)
		//	{
		//		smxes[i] = smx;
		//	}

		//	_signsBackingArray = smxes.Select(x => x.Sign ? ALL_BITS_SET : 0L).ToArray();
		//	SignsMemory = new Memory<ulong>(_signsBackingArray);

		//	var numberOfLimbs = smxes[0].Mantissa.Length;
		//	Mantissas = new ulong[numberOfLimbs][];

		//	for (var j = 0; j < numberOfLimbs; j++)
		//	{
		//		Mantissas[j] = new ulong[smxes.Length];

		//		for (var i = 0; i < smxes.Length; i++)
		//		{
		//			Mantissas[j][i] = smxes[i].Mantissa[j];
		//		}
		//	}

		//	MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		//}

		public FPValues(Smx[] smxes)
		{
			_signsBackingArray = smxes.Select(x => x.Sign ? ALL_BITS_SET : 0L).ToArray();
			SignsMemory = new Memory<ulong>(_signsBackingArray);

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

		public FPValues(Smx2C smx2C, int count) : this(Enumerable.Repeat(smx2C, count).ToArray())
		{ }
		
		//public FPValues(Smx2C smx, int count)
		//{
		//	var smxes = new Smx2C[count];

		//	for (int i = 0; i < count; i++)
		//	{
		//		smxes[i] = smx;
		//	}

		//	_signsBackingArray = smxes.Select(x => x.Sign ? ALL_BITS_SET : 0L).ToArray();
		//	SignsMemory = new Memory<ulong>(_signsBackingArray);

		//	var numberOfLimbs = smxes[0].Mantissa.Length;
		//	Mantissas = new ulong[numberOfLimbs][];

		//	for (var j = 0; j < numberOfLimbs; j++)
		//	{
		//		Mantissas[j] = new ulong[smxes.Length];

		//		for (var i = 0; i < smxes.Length; i++)
		//		{
		//			Mantissas[j][i] = smxes[i].Mantissa[j];
		//		}
		//	}

		//	MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
		//}

		public FPValues(Smx2C[] smxes)
		{
			_signsBackingArray = smxes.Select(x => x.Sign ? ALL_BITS_SET : 0L).ToArray();
			SignsMemory = new Memory<ulong>(_signsBackingArray);

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

		#endregion

		#region Public Properties

		public int Length => Mantissas[0].Length;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Length / Vector<ulong>.Count;

		public ulong[][] Mantissas { get; init; } 

		private ulong[] _signsBackingArray;
		public Memory<ulong> SignsMemory { get; init; }
		public Memory<ulong>[] MantissaMemories { get; init; }

		public byte BitsBeforeBP { get; init; }

		#endregion

		#region Public Methods

		public bool[] GetSigns()
		{
			var result = _signsBackingArray.Select(x => x == ALL_BITS_SET).ToArray();
			return result;
		}

		public bool GetSign(int index)
		{
			var result = _signsBackingArray[index] == ALL_BITS_SET;
			return result;
		}

		public void SetSign(int index, bool value)
		{
			_signsBackingArray[index] = value ? ALL_BITS_SET : 0L;
		}

		public Span<Vector256<ulong>> GetLimbVectorsUL(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(x.Span);

			return result;
		}

		public Span<Vector256<long>> GetLimbVectorsL(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<long>> result = MemoryMarshal.Cast<ulong, Vector256<long>>(x.Span);

			return result;
		}

		public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		{
			var x = MantissaMemories[limbIndex];
			Span<Vector256<uint>> result = MemoryMarshal.Cast<ulong, Vector256<uint>>(x.Span);

			return result;
		}

		public Span<Vector256<ulong>> GetSignVectorsUL()
		{
			Span<Vector256<ulong>> result = MemoryMarshal.Cast<ulong, Vector256<ulong>>(SignsMemory.Span);
			return result;
		}

		public FPValues Negate()
		{
			var signs = GetSigns().Select(x => !x).ToArray();

			var result = new FPValues(signs, CloneMantissas());

			return result;
		}

		public FPValues Negate2C()
		{
			var result = Clone2C(out var signs);

			for (var i = 0; i < Mantissas.Length; i++)
			{
				var partialWordLimbs = GetMantissa(i);

				var newSign = !signs[i];

				//var non2CPWLimbs = ScalarMathHelper.ConvertFrom2C(partialWordLimbs, newSign);
				var non2CPWLimbs = ScalarMathHelper.Toggle2C(partialWordLimbs, signExtendIntoHiWord: true);

				SetSign(i, newSign);
				SetMantissa(result.Mantissas, i, non2CPWLimbs);
			}

			return result;
		}

		//public ulong[] GetNon2CMantissa(int index)
		//{
		//	var partialWordLimbs = GetMantissa(index);

		//	var lzcMsl = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);
		//	var currentSign = !(lzcMsl == 0);

		//	var newSign = !currentSign;
		//	var result = ScalarMathHelper.ConvertFrom2C(partialWordLimbs, newSign);

		//	return result;
		//}

		public FPValues ConvertFrom2C(int[] inPlayList, int numberOfLanes)
		{
			var result = Clone2C(out var signs);

			var anyNegativeValues = !signs.Any(x => !x);

			if (!anyNegativeValues)
			{
				// all signs are plus
				return result;
			}

			var indexes = inPlayList;
			for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
			{
				var idx = indexes[idxPtr];
				var resultPtr = idx * numberOfLanes;

				for (var i = 0; i < numberOfLanes; i++)
				{
					var valPtr = resultPtr + i;

					if (!signs[valPtr])
					{
						var partialWordLimbs = GetMantissa(valPtr);

						var non2CPWLimbs = ScalarMathHelper.Toggle2C(partialWordLimbs, signExtendIntoHiWord: true);

						SetMantissa(result.Mantissas, valPtr, non2CPWLimbs);
					}
				}
			}

			return result;
		}

		//public bool[] GetSigns2C(int[] inPlayList, int numberOfLanes)
		//{
		//	bool[] signs = new bool[Mantissas.Length];

		//	var indexes = inPlayList;

		//	for (var idxPtr = 0; idxPtr < indexes.Length; idxPtr++)
		//	{
		//		var idx = indexes[idxPtr];
		//		var resultPtr = idx * numberOfLanes;

		//		for (var i = 0; i < numberOfLanes; i++)
		//		{
		//			var valPtr = resultPtr + i;
		//			var msLimb = Mantissas[0][valPtr];

		//			var lzcMsl = BitOperations.LeadingZeroCount(msLimb);

		//			signs[valPtr] = !(lzcMsl == 0);
		//		}
		//	}

		//	return signs;
		//}

		//public FPValues ConvertFrom2C()
		//{
		//	ulong[][] mArrays = CloneMantissas();

		//	bool[] signs = new bool[Mantissas.Length];

		//	for (var i = 0; i < Mantissas.Length; i++)
		//	{
		//		var partialWordLimbs = GetMantissa(i);

		//		var lzcMsl = BitOperations.LeadingZeroCount(partialWordLimbs[^1]);
		//		var sign = !(lzcMsl == 0);

		//		var non2CPWLimbs = ScalarMathHelper.ConvertFrom2C(partialWordLimbs, sign);

		//		SetMantissa(mArrays, i, non2CPWLimbs);
		//	}

		//	var result = new FPValues(signs, mArrays);

		//	return result;
		//}

		//public bool[] GetSigns2C()
		//{
		//	bool[] signs = Mantissas[0].Select(x => BitOperations.LeadingZeroCount(x) != 0).ToArray();

		//	return signs;
		//}

		private ulong[] GetMantissa(ulong[][] mArrays, int index)
		{
			var result = mArrays.Select(x => x[index]).ToArray();
			return result;
		}

		public void SetMantissa(ulong[][] mArrays, int index, ulong[] values)
		{
			for (var i = 0; i < values.Length; i++)
			{
				mArrays[i][index] = values[i];
			}
		}

		public ulong[] GetMantissa(int index)
		{
			var result = Mantissas.Select(x => x[index]).ToArray();
			return result;
		}

		public void SetMantissa(int index, ulong[] values)
		{
			for(var i = 0; i < values.Length; i++)
			{
				Mantissas[i][index] = values[i];
			}
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

		private static Memory<ulong>[] BuildMantissaMemoryVectors(ulong[][] mantissas)
		{
			var result = new Memory<ulong>[mantissas.Length];

			for (var i = 0; i < mantissas.Length; i++)
			{
				result[i] = new Memory<ulong>(mantissas[i]);
			}

			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FPValues Clone()
		{
			var result = new FPValues(GetSigns(), CloneMantissas());

			return result;
		}

		//private const ulong TEST_BIT_32 = 0x0000000100000000; // bit 32 is set.
		private const ulong TEST_BIT_31 = 0x0000000080000000; // bit 31 is set.

		public FPValues Clone2C(out bool[] signs)
		{
			//signs = Mantissas[^1].Select(x => BitOperations.LeadingZeroCount(x) != 0).ToArray();

			signs = Mantissas[^1].Select(x => (x & TEST_BIT_31) > 0).ToArray();


			var result = new FPValues(signs, CloneMantissas());

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
