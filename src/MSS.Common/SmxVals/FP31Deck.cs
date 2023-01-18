using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using MSS.Common.APValues;

namespace MSS.Common.SmxVals
{
    public class FP31Deck : ICloneable
    {
        #region Constructors

        public FP31Deck(int limbCount, int valueCount) : this(BuildLimbs(limbCount, valueCount))
        {
            IsZero = true;
        }

        public FP31Deck(FP31Val fP31Val, int extent) : this(Duplicate(fP31Val, extent))
        { }

        private FP31Deck(uint[][] mantissas)
        {
            Mantissas = mantissas;
            MantissaMemories = BuildMantissaMemoryVectors(Mantissas);
        }

        public FP31Deck(FP31Val[] fp31Vals)
        {
            var limbCount = fp31Vals[0].LimbCount;
            var valueCount = fp31Vals.Length;

            Mantissas = new uint[limbCount][];

            for (var j = 0; j < limbCount; j++)
            {
                Mantissas[j] = new uint[valueCount];

                for (var i = 0; i < valueCount; i++)
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

        private static FP31Val[] Duplicate(FP31Val fp31Val, int count)
        {
            var result = new FP31Val[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = fp31Val.Clone();
            }

            return result;
        }

        #endregion

        #region Public Properties

        public static readonly int Lanes = Vector256<uint>.Count;

        public int ValueCount => Mantissas[0].Length;
        public int LimbCount => Mantissas.Length;
        public int VectorCount => ValueCount / Lanes;

        public uint[][] Mantissas { get; init; }
        public Memory<uint>[] MantissaMemories { get; init; }

        public bool IsZero { get; private set; }

        #endregion

        #region Public Methods

        public int[] GetNewInPlayList()
        {
            var inPlayList = Enumerable.Range(0, VectorCount).ToArray();
            return inPlayList;
        }

        public uint[] GetMantissa(int index)
        {
            var result = Mantissas.Select(x => x[index]).ToArray();
            return result;
        }

        public void SetMantissa(int index, uint[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                Mantissas[i][index] = values[i];
            }
        }

        public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
        {
            var x = MantissaMemories[limbIndex];
            var result = MemoryMarshal.Cast<uint, Vector256<uint>>(x.Span);

            return result;
        }

        public void UpdateFrom(FP31Val fp31Val)
        {
            if (fp31Val.Mantissa.Length != LimbCount)
            {
                throw new ArgumentException("The first fp31Val has a different number of limbs than the FP31Decks's LimbCount.");
            }

            for (var j = 0; j < LimbCount; j++)
            {
                var destLimb = Mantissas[j];
                var sourceLimbVal = fp31Val.Mantissa[j];

                for (var i = 0; i < ValueCount; i++)
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

                for (var i = 0; i < fp31Vals.Length; i++)
                {
                    destLimb[i] = fp31Vals[i].Mantissa[j];
                }
            }

            IsZero = false;
        }

        public void UpdateFrom(FP31Deck source)
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

        public void UpdateFrom(FP31Deck source, int sourceIndex, int destinationIndex, int length)
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

            IsZero = true;
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
            result.IsZero = IsZero;
            return result;
        }

        private uint[][] CloneMantissas()
        {
            var result = new uint[Mantissas.Length][];

            for (var i = 0; i < Mantissas.Length; i++)
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
