using System;
using System.Diagnostics;

namespace MSS.Common.SmxVals
{
	public class ShiftedArray<T> where T : struct
	{
		#region Constructors

		public ShiftedArray()
		{
			Array = new T[0];
			Offset = 0;
			Extension = 0;
			IndexOfLastNonZeroLimb = -1;
			Carry = null;
			CarryIndex = 0;
		}

		public ShiftedArray(T[] array, int offset, int indexOfLastNonZeroLimb)
		{
			Array = array ?? throw new ArgumentNullException(nameof(array));
			Offset = offset;
			Extension = 0;
			IndexOfLastNonZeroLimb = indexOfLastNonZeroLimb;
			Carry = null;
			CarryIndex = 0;
		}

		#endregion

		#region Public Properties

		public T[] Array { get; set; }
		public T? Carry { get; private set; }
		public int CarryIndex { get; private set; }

		public int Offset { get; set; }
		public int Extension { get; set; }
		public int IndexOfLastNonZeroLimb { get; set; }

		public int Length => Array.Length + Offset + Extension;

		public T this[int index]
		{
			get
			{
				if (index > Length - 1)
				{
					throw new IndexOutOfRangeException();
				}

				if (index == CarryIndex && Carry.HasValue)
				{
					return Carry.Value;
				}

				if (index < Offset || index > Length - 1 - Extension)
				{
					return default;
				}

				return Array[index - Offset];
			}
		}

		#endregion

		#region Public Methods

		public void SetCarry(T carry)
		{
			Carry = carry;
			Extension++;

			CarryIndex = Length - 1;
			IndexOfLastNonZeroLimb = Length - 1;
		}

		public T[] Materialize()
		{
			try
			{
				if (IndexOfLastNonZeroLimb < 0)
				{
					return new T[] { default };
				}

				var len = IndexOfLastNonZeroLimb + 1;

				var result = new T[len];

				var i = 0;

				for (; i < Offset; i++)
				{
					result[i] = default;
				}

				for (; i < len; i++)
				{
					result[i] = this[i];
				}

				return result;
			}
			catch (Exception ee)
			{
				Debug.WriteLine($"Got ee: {ee}.");
				throw;

			}
		}

		public T[] MaterializeAll()
		{
			try
			{
				var len = Length;

				var result = new T[len];

				for (var i = 0; i < len; i++)
				{
					result[i] = this[i];
				}

				return result;
			}
			catch (Exception ee)
			{
				Debug.WriteLine($"Got ee: {ee}.");
				throw;
			}
		}


		#endregion

	}
}
