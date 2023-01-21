using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Common.APValues
{
	public class FP31VectorsPW : ICloneable
	{
		#region Constructors

		public FP31VectorsPW(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
		{
			IsZero = true;
		}

		public FP31VectorsPW(Vector256<ulong>[][] mantissas)
		{
			Mantissas = mantissas;
		}

		public FP31VectorsPW(Vector256<ulong>[] mantissa, int valueCount)
		{
			var limbCount = mantissa.Length;
			var vectorCount = valueCount / Lanes;

			Mantissas = new Vector256<ulong>[limbCount][];

			for (var j = 0; j < limbCount; j++)
			{
				Mantissas[j] = Enumerable.Repeat(mantissa[j], vectorCount).ToArray();
			}
		}

		//public FP31VectorsPW(FP31DeckPW fP31Deck)
		//{
		//	var limbCount = fP31Deck.LimbCount;
		//	var valueCount = fP31Deck.ValueCount;
		//	var vectorCount = fP31Deck.VectorCount;

		//	Mantissas = new Vector256<ulong>[limbCount][];

		//	for (var j = 0; j < limbCount; j++)
		//	{
		//		var destVectors = new Vector256<ulong>[valueCount];
		//		Mantissas[j] = destVectors;

		//		var sourceVectors = fP31Deck.GetLimbVectorsUL(j);

		//		for (var i = 0; i < vectorCount; i++)
		//		{
		//			destVectors[i] = sourceVectors[i];
		//		}
		//	}
		//}

		public FP31VectorsPW(FP31Val fp31Val, int valueCount) : this(CreateASingleVector(fp31Val), valueCount)
		{ }

		private static Vector256<ulong>[] CreateASingleVector(FP31Val fp31Val)
		{
			var limbCount = fp31Val.LimbCount;

			var single = new Vector256<ulong>[limbCount];

			for (var j = 0; j < limbCount; j++)
			{
				single[j] = Vector256.Create((ulong)fp31Val.Mantissa[j]);
			}

			return single;
		}

		public FP31VectorsPW(FP31Val[] fp31Vals)
		{
			var limbCount = fp31Vals[0].LimbCount;
			var valueCount = fp31Vals.Length;
			var vectorCount = valueCount / Lanes;

			Mantissas = new Vector256<ulong>[limbCount][];

			for (var j = 0; j < limbCount; j++)
			{
				var limbs = new Vector256<ulong>[vectorCount];
				var elements = MemoryMarshal.Cast<Vector256<ulong>, ulong>(limbs);

				for (var i = 0; i < valueCount; i++)
				{
					elements[i] = fp31Vals[i].Mantissa[j];
				}

				Mantissas[j] = limbs;
			}
		}

		private static Vector256<ulong>[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new Vector256<ulong>[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = Enumerable.Repeat(Vector256<ulong>.Zero, valueCount).ToArray();
			}

			return result;
		}

		#endregion

		#region Public Properties

		public static readonly int Lanes = Vector256<ulong>.Count;

		public int ValueCount => Mantissas[0].Length * Lanes;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Mantissas[0].Length;

		public Vector256<ulong>[][] Mantissas { get; init; } 

		public bool IsZero { get; private set; }

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

		public Vector256<ulong>[] GetLimbVectorsUL(int limbIndex)
		{
			var result = Mantissas[limbIndex];
			return result;
		}

		public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		{
			var resultLimbVecsL = GetLimbVectorsUL(limbIndex);
			Span<Vector256<ulong>> x = new Span<Vector256<ulong>>(resultLimbVecsL);
			var resultLimbVecs = MemoryMarshal.Cast<Vector256<ulong>, Vector256<uint>>(x);
			return resultLimbVecs;
		}

		public void ClearManatissMems()
		{
			for (var i = 0; i < LimbCount; i++)
			{
				Array.Clear(Mantissas[i]);
			}

			IsZero = true;
		}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FP31VectorsPW Clone()
		{
			var result = new FP31VectorsPW(CloneMantissas());
			result.IsZero = IsZero;
			return result;
		}

		private Vector256<ulong>[][] CloneMantissas()
		{
			Vector256<ulong>[][] result = new Vector256<ulong>[Mantissas.Length][];
			
			for(var i = 0; i < Mantissas.Length; i++)
			{
				var a = new Vector256<ulong>[Mantissas[i].Length];

				Array.Copy(Mantissas[i], a, a.Length);

				result[i] = a;
			}

			return result;
		}

		#endregion
	}
}
