using System;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public static class DoubleHelper
	{
		private const long ExponentMask = 0x7FF0000000000000;

        private readonly static double LOG_BASE_TEN_OF_TWO = Math.Log10(2);

        public static string ToExactString(double d, out int exponent)
        {
            // Translate the double into sign, exponent and mantissa.
            var bits = BitConverter.DoubleToInt64Bits(d);
            // Note that the shift is sign-extended, hence the test against -1 not 1
            var negative = (bits & (1L << 63)) != 0;
            exponent = (int)((bits >> 52) & 0x7ffL);
            var mantissa = bits & 0xfffffffffffffL;

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                exponent++;
            }
            // Normal numbers; leave exponent as it is but add extra
            // bit to the front of the mantissa
            else
            {
                mantissa = mantissa | (1L << 52);
            }

            // Bias the exponent. It's actually biased by 1023, but we're
            // treating the mantissa as m.0 rather than 0.m, so we need
            // to subtract another 52 from it.
            exponent -= 1075;

            if (mantissa == 0)
            {
                return negative ? "-0" : "0";
            }

            /* Normalize */
            while ((mantissa & 1) == 0)
            {    /*  i.e., Mantissa is even */
                mantissa >>= 1;
                exponent++;
            }

            return negative 
                ? $"-{mantissa}e{exponent}" 
                : $"{mantissa}e{exponent}";
        }

        public static bool HasPrecision(double x)
		{
			return x != 0 && !IsSubnormal(x);
		}

		public static bool IsSubnormal(double v)
		{
			var bithack = BitConverter.DoubleToInt64Bits(v);
			return bithack != 0 && (bithack & ExponentMask) == 0;
		}

        public static double RoundOff(double number, int interval)
        {
            var remainder = (int)Math.IEEERemainder(number, interval);
            number += (remainder < interval / 2) ? -remainder : (interval - remainder);
            return number;
        }

        public static double GetNumberOfBinaryDigits(int numberOfDecimalDigits)
        {
			var result = numberOfDecimalDigits / LOG_BASE_TEN_OF_TWO;

			return result;
        }

		public static double GetNumberOfDecimalDigits(int numberOfBinaryDigits)
		{
			var result = numberOfBinaryDigits * LOG_BASE_TEN_OF_TWO;

			return result;
		}

        public static int RoundToZero(double x)
        {
			var result = (int)Math.Round(x, MidpointRounding.ToZero);
            return result;
		}

		public static int RoundAwayFromZero(double x)
		{
			var result = (int)Math.Round(x, MidpointRounding.AwayFromZero);
			return result;
		}

		public static int RoundToEven(double x)
		{
			var result = (int)Math.Round(x, MidpointRounding.ToEven);
			return result;
		}


	}
}
