using System;

namespace MSS.Types.MSetOld
{
	public static class DoubleHelper
	{
		private const long ExponentMask = 0x7FF0000000000000;

		public static bool HasPrecision(double x)
		{
			if (x == 0)
				return false;

			if (IsSubnormal(x))
				return false;

			return true;
		}

		public static bool IsSubnormal(double v)
		{
			long bithack = BitConverter.DoubleToInt64Bits(v);
			if (bithack == 0) return false;
			return (bithack & ExponentMask) == 0;
		}
	}
}
