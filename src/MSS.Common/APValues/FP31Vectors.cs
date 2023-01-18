using System;
using System.Linq;
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
		
		//public FP31Vectors(FP31Val fP31Val, int extent) : this(Duplicate(fP31Val, extent))
		//{ }

		public FP31Vectors(FP31Deck fP31Deck)
		{
			var limbCount = fP31Deck.LimbCount;
			var valueCount = fP31Deck.ValueCount;
			var vectorCount = fP31Deck.VectorCount;

			Mantissas = new Vector256<uint>[limbCount][];

			for (var j = 0; j < limbCount; j++)
			{
				var destVectors = new Vector256<uint>[valueCount];
				Mantissas[j] = destVectors;

				var sourceVectors = fP31Deck.GetLimbVectorsUW(j);

				for (var i = 0; i < vectorCount; i++)
				{
					destVectors[i] = sourceVectors[i];
				}
			}
		}

		//public FP31Vectors(FP31Val[] fp31Vals)
		//{
		//	var numberOfLimbs = fp31Vals[0].LimbCount;
		//	Mantissas = new Vector256<uint>[numberOfLimbs][];

		//	for (var j = 0; j < numberOfLimbs; j++)
		//	{
		//		Mantissas[j] = new Vector256<uint>[fp31Vals.Length];

		//		for (var i = 0; i < fp31Vals.Length; i++)
		//		{
		//			Mantissas[j][i] = fp31Vals[i].Mantissa[j];
		//		}
		//	}
		//}

		private static Vector256<uint>[][] BuildLimbs(int limbCount, int valueCount)
		{
			var result = new Vector256<uint>[limbCount][];

			for (var i = 0; i < limbCount; i++)
			{
				result[i] = new Vector256<uint>[valueCount];
			}

			return result;
		}

		#endregion

		#region Public Properties

		public static readonly int Lanes = Vector256<uint>.Count;

		public int ValueCount => Mantissas[0].Length / Lanes;
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

		//public void UpdateFrom(FP31Val fp31Val)
		//{
		//	if (fp31Val.Mantissa.Length != LimbCount)
		//	{
		//		throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
		//	}

		//	for (var j = 0; j < LimbCount; j++)
		//	{
		//		var destLimb = Mantissas[j];
		//		var sourceLimbVal = fp31Val.Mantissa[j];

		//		for (int i = 0; i < ValueCount; i++)
		//		{
		//			destLimb[i] = sourceLimbVal;
		//		}
		//	}

		//	IsZero = false;
		//}

		//public void UpdateFrom(FP31Val[] fp31Vals)
		//{
		//	if (fp31Vals[0].Mantissa.Length != LimbCount)
		//	{
		//		throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
		//	}

		//	if (fp31Vals.Length != ValueCount)
		//	{
		//		throw new ArgumentException("The number of FP31Vals is different for the FP31Deck's ValueCount.");
		//	}

		//	for (var j = 0; j < LimbCount; j++)
		//	{
		//		var destLimb = Mantissas[j];

		//		for (var i = 0; i < fp31Vals.Length; i++)
		//		{
		//			destLimb[i] = fp31Vals[i].Mantissa[j];
		//		}
		//	}

		//	IsZero = false;
		//}

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
				Array.Copy(source.Mantissas[i], Mantissas[i], ValueCount);
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

		public void ClearManatissMems(int[] inPlayList)
		{
			var indexes = inPlayList;

			for (var i = 0; i < LimbCount; i++)
			{
				var vectors = Mantissas[i];

				for (var j = 0; j < indexes.Length; j++)
				{
					vectors[indexes[j]] = Vector256<uint>.Zero;
				}
			}
		}

		public void ClearManatissMems()
		{
			for (var i = 0; i < LimbCount; i++)
			{
				var vectors = Mantissas[i];

				for (var j = 0; j < VectorCount; j++)
				{
					vectors[j] = Vector256<uint>.Zero;
				}
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
