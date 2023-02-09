using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types.APValues
{
	public class FP31ValArray //: ICloneable
	{
		#region Constructors

		public FP31ValArray(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount), limbCount)
		{
			IsZero = true;
		}

		public FP31ValArray(Vector256<uint>[] mantissas, int limbCount)
		{
			Mantissas = mantissas;
			LimbCount = limbCount;
			ValueCount = mantissas.Length / LimbCount;
		}

		public FP31ValArray(FP31Val fp31Val, int valueCount) 
		{
			Mantissas = new Vector256<uint>[valueCount * LimbCount];
			LimbCount = fp31Val.LimbCount;
			ValueCount = valueCount;

			var vectorCount = ValueCount / Lanes;

			for (var valuePtr = 0; valuePtr < vectorCount; valuePtr++)
			{
				var vectorPtr = valuePtr * LimbCount;
				for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					Mantissas[vectorPtr + limbPtr] = Vector256.Create(fp31Val.Mantissa[limbPtr]);
				}
			}
		}

		public FP31ValArray(FP31Val[] fp31Vals)
		{
			LimbCount = fp31Vals[0].LimbCount;
			ValueCount = fp31Vals.Length;
			Mantissas = new Vector256<uint>[ValueCount * LimbCount];

			//var mem = new Memory<Vector256<uint>>(Mantissas);

			// Conver the array of Vector<uint> into an array of uints. (Back has a length = to the # of limbs * the # of values (i.e., the # of items in the fp31Vals array.)
			Span<uint> back = MemoryMarshal.Cast<Vector256<uint>, uint>(Mantissas);

			// We will use a ptr into the backing array for each limb.
			var backPtrs = new int[LimbCount];

			// We will process the values in sets, where each set has Lane # of values.
			var vectorCount = ValueCount / Lanes;

			// As we advance forward by a 'LimbSet'
			//		We move through the array of raw uint's that back the array of Vectors, in increments of limbCount * Lanes.
			// and	We move through the source fp31Vals array, in increments of Lanes. If each vector holds 8 uints, we process 8 fp31Vals at a time.
			
			// For example with: 2 limbs and 8 lanes (8 values fit in a single Vector256 item) We will move
			// through the backing array 16 uint values at a time
			// and move through the fp31Vals 8 at a time.

			for (var vecPtr = 0; vecPtr < vectorCount; vecPtr++)
			{
				var backPtrBase = vecPtr * LimbCount * Lanes;

				for (var i = 0; i < LimbCount; i++)
				{
					backPtrs[i] = backPtrBase + (i * Lanes); // + 0, 8, 16, 24, 32, etc., for as many limbs present.
				}

				var valPtr = vecPtr * Lanes;
				for (var lanePtr = 0; lanePtr < Lanes; lanePtr++)
				{
					var fp31Val = fp31Vals[valPtr + lanePtr];

					for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
					{
						back[backPtrs[limbPtr] + lanePtr] = fp31Val.Mantissa[limbPtr];
					}
				}
			}
		}

		private static Vector256<uint>[] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new Vector256<uint>[valueCount * limbCount];
			return result;
		}

		#endregion

		#region Public Properties

		public int ValueCount { get; init; }
		public int LimbCount { get; init; }

		public int Lanes => Vector256<uint>.Count;
		//public int VectorCount => ValueCount * LimbCount;

		public Vector256<uint>[] Mantissas { get; init; } 

		public bool IsZero { get; private set; }

		#endregion

		#region Public Methods

		public Vector256<uint>[] GetLimbVectorsUW(int limbIndex)
		{
			throw new NotImplementedException();
		}

		public void FillLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for(var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = Mantissas[vecPtr++];
			}
		}

		public void UpdateFrom(FP31Val fp31Val)
		{
			if (fp31Val.Mantissa.Length != LimbCount)
			{
				throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
			}

			var vectorCount = ValueCount / Lanes;

			for (var valuePtr = 0; valuePtr < vectorCount; valuePtr++)
			{
				var vectorPtr = valuePtr * LimbCount;
				for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					Mantissas[vectorPtr + limbPtr] = Vector256.Create(fp31Val.Mantissa[limbPtr]);
				}
			}

			IsZero = false;
		}

		public void UpdateFrom(FP31Val[] fp31Vals)
		{
			if (fp31Vals[0].Mantissa.Length != LimbCount)
			{
				throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
			}

			if (fp31Vals.Length != ValueCount)
			{
				throw new ArgumentException("The number of FP31Vals is different for the FP31Deck's ValueCount.");
			}

			//LimbCount = fp31Vals[0].LimbCount;
			//ValueCount = fp31Vals.Length;
			//Mantissas = new Vector256<uint>[ValueCount * LimbCount];

			//var mem = new Memory<Vector256<uint>>(Mantissas);

			// Conver the array of Vector<uint> into an array of uints. (Back has a length = to the # of limbs * the # of values (i.e., the # of items in the fp31Vals array.)
			Span<uint> back = MemoryMarshal.Cast<Vector256<uint>, uint>(Mantissas);

			// We will use a ptr into the backing array for each limb.
			var backPtrs = new int[LimbCount];

			// We will process the values in sets, where each set has Lane # of values.
			var vectorCount = ValueCount / Lanes;

			// As we advance forward by a 'LimbSet'
			//		We move through the array of raw uint's that back the array of Vectors, in increments of limbCount * Lanes.
			// and	We move through the source fp31Vals array, in increments of Lanes. If each vector holds 8 uints, we process 8 fp31Vals at a time.

			// For example with: 2 limbs and 8 lanes (8 values fit in a single Vector256 item) We will move
			// through the backing array 16 uint values at a time
			// and move through the fp31Vals 8 at a time.

			for (var vecPtr = 0; vecPtr < vectorCount; vecPtr++)
			{
				var backPtrBase = vecPtr * LimbCount * Lanes;

				for (var i = 0; i < LimbCount; i++)
				{
					backPtrs[i] = backPtrBase + (i * Lanes); // + 0, 8, 16, 24, 32, etc., for as many limbs present.
				}

				var valPtr = vecPtr * Lanes;
				for (var lanePtr = 0; lanePtr < Lanes; lanePtr++)
				{
					var fp31Val = fp31Vals[valPtr + lanePtr];

					for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
					{
						back[backPtrs[limbPtr] + lanePtr] = fp31Val.Mantissa[limbPtr];
					}
				}
			}

			IsZero = false;
		}

		public void UpdateFrom(byte[] mantissas, int startIndex, int length)
		{
			//var x = new Memory<Vector256<uint>>(Mantissas);

			var source = new Span<byte>(mantissas, startIndex, length);

			Span<byte> back = MemoryMarshal.Cast<Vector256<uint>, byte>(Mantissas);

			source.CopyTo(back);
		}

		public void UpdateFromLimbSet(int valueIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = valueIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				Mantissas[vecPtr++] = limbSet[i];
			}
		}

		public void ClearManatissMems()
		{
			Array.Clear(Mantissas);

			IsZero = true;
		}

		#endregion

		#region ICloneable Support

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		//public FP31Vectors Clone()
		//{
		//	var result = new FP31Vectors(CloneMantissas());
		//	result.IsZero = IsZero;
		//	return result;
		//}

		//private Vector256<uint>[][] CloneMantissas()
		//{
		//	Vector256<uint>[][] result = new Vector256<uint>[Mantissas.Length][];
			
		//	for(var i = 0; i < Mantissas.Length; i++)
		//	{
		//		var a = new Vector256<uint>[Mantissas[i].Length];

		//		Array.Copy(Mantissas[i], a, a.Length);

		//		result[i] = a;
		//	}

		//	return result;
		//}

		#endregion
	}
}
