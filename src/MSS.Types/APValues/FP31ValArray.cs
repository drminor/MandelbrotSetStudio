using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Types.APValues
{
	public class FP31ValArray //: ICloneable
	{
		private readonly int Lanes;

		#region Constructors

		public FP31ValArray(FP31Val fp31Val, int valueCount) : this(fp31Val.LimbCount, valueCount)
		{
			for (var vectorIndex = 0; vectorIndex < VectorCount; vectorIndex++)
			{
				var vecPtr = vectorIndex * LimbCount;

				for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					Mantissas[vecPtr + limbPtr] = Vector256.Create(fp31Val.Mantissa[limbPtr]);
				}
			}
		}

		public FP31ValArray(FP31Val[] fp31Vals) : this(fp31Vals[0].LimbCount, fp31Vals.Length)
		{
			//var mem = new Memory<Vector256<uint>>(Mantissas);

			// Convert the array of Vector<uint> into an array of uints. (Back has a length = to the # of limbs * the # of values (i.e., the # of items in the fp31Vals array.)
			Span<uint> back = MemoryMarshal.Cast<Vector256<uint>, uint>(Mantissas);

			// We will use a ptr into the backing array for each limb.
			var backPtrs = new int[LimbCount];

			// We will process the values in sets, where each set has Lane # of values.

			// As we advance forward by a 'LimbSet'
			//		We move through the array of raw uint's that back the array of Vectors, in increments of limbCount * Lanes.
			// and	We move through the source fp31Vals array, in increments of Lanes. If each vector holds 8 uints, we process 8 fp31Vals at a time.
			
			// For example with: 2 limbs and 8 lanes (8 values fit in a single Vector256 item) We will move
			// through the backing array 16 uint values at a time
			// and move through the fp31Vals 8 at a time.

			for (var vecPtr = 0; vecPtr < VectorCount; vecPtr++)
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

		public FP31ValArray(int limbCount, int valueCount)
		{
			LimbCount = limbCount;
			ValueCount = valueCount;
			Lanes = Vector256<uint>.Count;
			VectorCount = valueCount / Lanes;

			Mantissas = new Vector256<uint>[VectorCount * limbCount];
		}

		//private static Vector256<uint>[] BuildLimbs(int limbCount, int valueCount)
		//{
		//	var result = new Vector256<uint>[valueCount * limbCount];
		//	return result;
		//}

		#endregion

		#region Public Properties

		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int VectorCount { get; init; }

		public Vector256<uint>[] Mantissas { get; init; } 

		#endregion

		#region Public Methods

		public void UpdateFrom(FP31Val fp31Val)
		{
			if (fp31Val.Mantissa.Length != LimbCount)
			{
				throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
			}

			for (var vectorIndex = 0; vectorIndex < VectorCount; vectorIndex++)
			{
				var vecPtr = vectorIndex * LimbCount;
				for (var limbPtr = 0; limbPtr < LimbCount; limbPtr++)
				{
					Mantissas[vecPtr + limbPtr] = Vector256.Create(fp31Val.Mantissa[limbPtr]);
				}
			}
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

			// Convert the array of Vector<uint> into an array of uints. (Back has a length = to the # of limbs * the # of values (i.e., the # of items in the fp31Vals array.)
			Span<uint> back = MemoryMarshal.Cast<Vector256<uint>, uint>(Mantissas);

			// We will use a ptr into the backing array for each limb.
			var backPtrs = new int[LimbCount];

			// We will process the values in sets, where each set has Lane # of values.

			// As we advance forward by a 'LimbSet'
			//		We move through the array of raw uint's that back the array of Vectors, in increments of limbCount * Lanes.
			// and	We move through the source fp31Vals array, in increments of Lanes. If each vector holds 8 uints, we process 8 fp31Vals at a time.

			// For example with: 2 limbs and 8 lanes (8 values fit in a single Vector256 item) We will move
			// through the backing array 16 uint values at a time
			// and move through the fp31Vals 8 at a time.

			for (var vecPtr = 0; vecPtr < VectorCount; vecPtr++)
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

		//public void UpdateFrom(byte[] mantissas, int startIndex, int length)
		//{
		//	//var x = new Memory<Vector256<uint>>(Mantissas);

		//	var source = new Span<byte>(mantissas, startIndex, length);

		//	Span<byte> back = MemoryMarshal.Cast<Vector256<uint>, byte>(Mantissas);

		//	source.CopyTo(back);
		//}

		public void FillLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				limbSet[i] = Mantissas[vecPtr + i];
			}
		}

		public void UpdateFromLimbSet(int vectorIndex, Vector256<uint>[] limbSet)
		{
			var vecPtr = vectorIndex * LimbCount;

			for (var i = 0; i < LimbCount; i++)
			{
				Mantissas[vecPtr + i] = limbSet[i];
			}
		}

		public void ClearManatissMems()
		{
			Array.Clear(Mantissas);
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
