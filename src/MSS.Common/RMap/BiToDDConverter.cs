using System;
using System.Numerics;

namespace MSS.Common
{
	public class BiToDDConverter
	{
		public double[] GetDoubles(BigInteger n, int exponent)
		{
			long fac = GetLargestB2Long();

			BigInteger bigOnes = BigInteger.DivRem(n, new BigInteger(fac), out BigInteger remainder);

			if (bigOnes > fac)
			{
				throw new ArgumentOutOfRangeException(nameof(n));
			}

			double a = (double)bigOnes;
			double b = (double)remainder;

			a = Math.ScaleB(a, exponent);
			b = Math.ScaleB(b, exponent);

			return new double[] { a, b };
		}

		private long GetLargestB2Long()
		{
			double f = Math.Pow(2, 53);
			return (long)f;
		}

	}
}
