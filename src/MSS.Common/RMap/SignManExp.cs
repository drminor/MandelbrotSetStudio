using System;
using System.Globalization;

namespace MSS.Common
{
	public class SignManExp
    {
        #region Constructors

        public SignManExp() : this("0.05") // TODO: Update SignManExp to handle decimal value 0.
		{ }

        public SignManExp(bool isNegative, string mantissa, int exponent)
		{
            FormatProvider = CultureInfo.InvariantCulture;

            IsNegative = isNegative;
            Mantissa = mantissa;
            Exponent = exponent;
		}

        public SignManExp(string s)
        {
            FormatProvider = CultureInfo.InvariantCulture;

            s = IsFixed(s) ? ConvertToScientificNotation(s) : s.Trim();

            IsNegative = s.StartsWith('-');

            var ePos = s.IndexOf("E", StringComparison.InvariantCultureIgnoreCase);
            var eStr = s[(ePos + 1)..];
            Exponent = (int)double.Parse(eStr, FormatProvider);

            var digits = IsNegative ? s[1..ePos] : s[0..ePos];
            Mantissa = GetMantissa(digits);
        }

        private static bool IsFixed(string s)
		{
            return !s.Contains("E", StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion

        #region Public Properties

        public bool IsNegative { get; }
        public string Mantissa { get; }
        public int Exponent { get; }

        public int Precision => Mantissa.Length;
        public int NumberOfDigitsAfterDecimalPoint => Mantissa.Length - (Exponent + 1);

        public IFormatProvider FormatProvider { get; }

        #endregion

        #region Public Methods

        public string GetValueAsString()
		{
            var sign = IsNegative ? "-" : string.Empty;
            var digits = BuildDigits(Mantissa, Exponent);
            var expSign = Exponent < 0 ? "-" : string.Empty;
            var strExp = Math.Abs(Exponent).ToString(CultureInfo.InvariantCulture); // .PadLeft(3, '0');

            var result = sign + digits + "e" + expSign + strExp;

            return result;
        }

        public double GetValueAsDouble()
		{
            if (Mantissa.Length < 10)
			{
                var s = GetValueAsString();
                var result = double.Parse(s, FormatProvider);

                return result;
			}
            else
			{
                return double.NaN;
			}
		}

		#endregion

        #region Private Methods

        private static string BuildDigits(string mantissa, int exponent)
		{
            var numZerosToPrfix = Math.Min(exponent, 0) * -1;
            var dpIndex = Math.Max(exponent, 0) + 1;
            var result = numZerosToPrfix > 0 ? new string('0', numZerosToPrfix) + mantissa : mantissa;
            result = result[0..dpIndex] + '.' + result[dpIndex..];

            return result;
        }

        private static string GetMantissa(string digits)
		{
            var result = digits.StartsWith('0')
                ? digits[2..]
                : digits.Remove(1, 1);

            return result;
		}

        private static string StripLeadingZeroes(string s, ref int dpPos)
        {
            // strip leading zeroes (leave decimal point and digit preceeding decimal point.)
            var i = 0;
            for (; i < dpPos - 1; i++)
            {
                if (s[i] != 0)
                {
                    break;
                }
            }

            dpPos -= i;
            s = s[i..];

            return s;
        }

        private static string RemoveAndCountLeadingZeroes(string s, out int cnt)
        {
            // Also removes, but does not count the decimal point.
            cnt = 0;

            var i = 0;
            for (; i < s.Length - 1; i++)
            {
                if (s[i] == '.')
                {
                    continue;
                }

                if (s[i] != '0')
                {
                    break;
                }

                cnt++;
            }

            s = s[i..];

            return s;
        }

        private static string ConditionForParse(string s)
        {
            s = s ?? string.Empty.Trim();

            if (s.StartsWith('.'))
            {
                s = "0" + s;
            }

            if (s.EndsWith('.'))
            {
                s = s[0..^1];
            }

            return s;
        }

        #endregion

        #region Static Helpers

        public static string ConvertToScientificNotation(string s)
        {
            if (!IsFixed(s))
            {
                return s;
            }

            s = ConditionForParse(s);

            var isNegative = s.StartsWith('-');
            if (isNegative)
            {
                s = s[1..];
            }

            var exp = 0;

            if (s.Length == 0)
            {
                exp = 0;
                s = "0.0";
            }
            else
            {
                var dpPos = s.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);

                s = StripLeadingZeroes(s, ref dpPos);

                if (dpPos == 1 && !s.StartsWith('0'))
                {
                    exp = 0;
                }
                else if (dpPos == -1)
                {
                    exp = s.Length - 1;
                    s = s[0..1] + '.' + s[1..];
                }
                else
                {
                    if (s.StartsWith('0'))
                    {
                        s = RemoveAndCountLeadingZeroes(s, out exp);
                        exp *= -1;
                    }
                    else
                    {
                        exp = dpPos - 1;
                    }
                    s = s[0..1] + '.' + s[1..];
                }
            }

            var sign = isNegative ? "-" : string.Empty;
            var expSign = exp < 0 ? "-" : string.Empty;
            var strExp = Math.Abs(exp).ToString(CultureInfo.InvariantCulture); // .PadLeft(3, '0');

            var result = sign + s + "e" + expSign + strExp;

            return result;
        }

        #endregion

    }
}
