using System;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public class PairOfVec<T> where T : struct
	{
		public PairOfVec(int limbCount)
		{
			Lower = new Vector256<T>[limbCount];
			Upper = new Vector256<T>[limbCount];

			ClearLimbSet();
		}

		public PairOfVec(Vector256<T>[] lower, Vector256<T>[] upper)
		{
			Lower = lower ?? throw new ArgumentNullException(nameof(lower));
			Upper = upper ?? throw new ArgumentNullException(nameof(upper));
		}

		public Vector256<T>[] Lower { get; init; }
		public Vector256<T>[] Upper { get; init; }

		public void ClearLimbSet()
		{
			for (var i = 0; i < Lower.Length; i++)
			{
				Lower[i] = Vector256<T>.Zero;
				Upper[i] = Vector256<T>.Zero;
			}
		}
	}
}
