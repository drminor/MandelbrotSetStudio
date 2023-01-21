using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSS.Common.APValues
{
	public class FP31Vectors : ICloneable
	{
		#region Constructors

		public FP31Vectors(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
		{
			IsZero = true;
		}

		public FP31Vectors(Vector256<uint>[][] mantissas)
		{
			Mantissas = mantissas;
		}

		public FP31Vectors(Vector256<uint>[] mantissa, int valueCount)
		{
			var limbCount = mantissa.Length;
			var vectorCount = valueCount / Lanes;

			Mantissas = new Vector256<uint>[limbCount][];

			for (var j = 0; j < limbCount; j++)
			{
				Mantissas[j] = Enumerable.Repeat(mantissa[j], vectorCount).ToArray();
			}
		}

		//public FP31Vectors(FP31Deck fP31Deck)
		//{
		//	var limbCount = fP31Deck.LimbCount;
		//	var valueCount = fP31Deck.ValueCount;
		//	var vectorCount = fP31Deck.VectorCount;

		//	Mantissas = new Vector256<uint>[limbCount][];

		//	for (var j = 0; j < limbCount; j++)
		//	{
		//		var destVectors = new Vector256<uint>[valueCount];
		//		Mantissas[j] = destVectors;

		//		var sourceVectors = fP31Deck.GetLimbVectorsUW(j);

		//		for (var i = 0; i < vectorCount; i++)
		//		{
		//			destVectors[i] = sourceVectors[i];
		//		}
		//	}
		//}

		public FP31Vectors(FP31Val fp31Val, int valueCount) : this(CreateASingleVector(fp31Val), valueCount)
		{ }

		private static Vector256<uint>[] CreateASingleVector(FP31Val fp31Val)
		{
			var limbCount = fp31Val.LimbCount;

			var single = new Vector256<uint>[limbCount];

			for (var j = 0; j < limbCount; j++)
			{
				single[j] = Vector256.Create(fp31Val.Mantissa[j]);
			}

			return single;
		}

		public FP31Vectors(FP31Val[] fp31Vals)
		{
			var limbCount = fp31Vals[0].LimbCount;
			var valueCount = fp31Vals.Length;
			var vectorCount = valueCount / Lanes;

			Mantissas = new Vector256<uint>[limbCount][];

			for (var j = 0; j < limbCount; j++)
			{
				var limbs = new Vector256<uint>[vectorCount];
				var elements = MemoryMarshal.Cast<Vector256<uint>, uint>(limbs);

				for (var i = 0; i < valueCount; i++)
				{
					elements[i] = fp31Vals[i].Mantissa[j];
				}

				Mantissas[j] = limbs;
			}
		}

		private static Vector256<uint>[][] BuildLimbs(int limbCount, int valueCount)
		{
			var vectorCount = valueCount / Lanes;
			var result = new Vector256<uint>[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new Vector256<uint>[vectorCount];
			}

			return result;
		}

		#endregion

		#region Public Properties

		public static readonly int Lanes = Vector256<uint>.Count;

		public int ValueCount => Mantissas[0].Length * Lanes;
		public int LimbCount => Mantissas.Length;
		public int VectorCount => Mantissas[0].Length;

		public Vector256<uint>[][] Mantissas { get; init; } 

		public bool IsZero { get; private set; }

		#endregion

		#region Public Methods

		public int[] GetNewInPlayList()
		{
			var inPlayList = Enumerable.Range(0, VectorCount).ToArray();
			return inPlayList;
		}

		public Vector256<uint>[] GetLimbVectorsUW(int limbIndex)
		{
			var result = Mantissas[limbIndex];
			return result;
		}

		public void UpdateFrom(FP31Val fp31Val)
		{
			if (fp31Val.Mantissa.Length != LimbCount)
			{
				throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
			}

			var sourceVectors = CreateASingleVector(fp31Val);

			for (var j = 0; j < LimbCount; j++)
			{
				var destLimb = Mantissas[j];
				var sourceLimbVal = sourceVectors[j];

				for (int i = 0; i < VectorCount; i++)
				{
					destLimb[i] = sourceLimbVal;
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

			for (var j = 0; j < LimbCount; j++)
			{
				var destLimb = Mantissas[j];

				var elements = MemoryMarshal.Cast<Vector256<uint>, uint>(destLimb);

				for (var i = 0; i < ValueCount; i++)
				{
					elements[i] = fp31Vals[i].Mantissa[j];
				}
			}

			IsZero = false;
		}

		public void UpdateFrom(FP31Vectors source)
		{
			if (source.LimbCount != LimbCount)
			{
				throw new ArgumentException("The source deck has a different LimbCount.");
			}

			if (source.ValueCount != ValueCount)
			{
				throw new ArgumentException("The source deck has a different ValueCount.");
			}

			for (var i = 0; i < Mantissas.Length; i++)
			{
				Array.Copy(source.Mantissas[i], Mantissas[i], VectorCount);
			}

			IsZero = source.IsZero;

		}

		public void UpdateFrom(FP31Vectors source, int sourceIndex, int destinationIndex, int length)
		{
			if (source.LimbCount != LimbCount)
			{
				throw new ArgumentException("The source deck has a different LimbCount.");
			}

			if (sourceIndex + length > source.ValueCount)
			{
				throw new ArgumentException("The target deck is shorter than startIndex + length.");
			}

			if (destinationIndex + length > ValueCount)
			{
				throw new ArgumentException("The target deck is shorter than startIndex + length.");
			}

			for (var i = 0; i < Mantissas.Length; i++)
			{
				Array.Copy(source.Mantissas[i], sourceIndex, Mantissas[i], destinationIndex, length);
			}

			IsZero = false;
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

		public FP31Vectors Clone()
		{
			var result = new FP31Vectors(CloneMantissas());
			result.IsZero = IsZero;
			return result;
		}

		private Vector256<uint>[][] CloneMantissas()
		{
			Vector256<uint>[][] result = new Vector256<uint>[Mantissas.Length][];
			
			for(var i = 0; i < Mantissas.Length; i++)
			{
				var a = new Vector256<uint>[Mantissas[i].Length];

				Array.Copy(Mantissas[i], a, a.Length);

				result[i] = a;
			}

			return result;
		}

		#endregion
	}
}
