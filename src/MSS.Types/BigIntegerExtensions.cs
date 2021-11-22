using System;
using System.Numerics;

namespace MSS.Types
{
	public static class BigIntegerExtensions
	{
		private readonly static BigInteger factor = new BigInteger(Math.Pow(2, 53));

		public static long[] ToLongs(this BigInteger bi)
		{
			BigInteger hi = BigInteger.DivRem(bi, factor, out BigInteger lo);

			if (hi > factor)
			{
				throw new ArgumentOutOfRangeException(nameof(bi));
			}

			long[] result = new long[] { (long) hi, (long) lo };

			return result;
		}
	}


}
