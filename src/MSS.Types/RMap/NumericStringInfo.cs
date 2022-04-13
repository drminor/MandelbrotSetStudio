using System;
using System.Linq;
using System.Globalization;
using System.Text;

namespace MSS.Types
{
    internal class NumericStringInfo
    {
        private readonly bool _isNegative;
        private readonly bool _isZero;

        private readonly int[] _before;
        private readonly int[] _after;

		#region Constructors

		public NumericStringInfo(int[] before, int[] after, bool isNegative)
		{
            _before = before;
            _after = after;
            _isNegative = isNegative;
		}

        public NumericStringInfo(double d) : this(d.ToString("G25", CultureInfo.InvariantCulture))
		{ }

        public NumericStringInfo(string s)
        {
            if (s.StartsWith('-'))
			{
                _isNegative = true;
                s = s[1..];
            }

            s = ConditionForParse(s);
            s = ConvertToFixedPoint(s);

            var dpPos = s.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
            var sCa = s.ToCharArray();

            if (dpPos == -1)
            {
                _before = sCa.Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
                _after = Array.Empty<int>();
                _isZero = _before.Length == 1 && _before[0] == 0;
            }
            else
            {
                _before = sCa.Take(dpPos).Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
                _after = sCa.Skip(dpPos + 1).Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
                _isZero = _before.Length == 1 && _before[0] == 0 && _after.Length == 1 && _after[0] == 0;
            }
        }

		#endregion

		#region Public Properties

		public int PlacesBeforeDp => _before.Length;
        public int PlacesAfterDp => _after.Length;

        public bool IsNegative => _isNegative;
        public bool IsZero => _isZero;

        public string SValue => GetString();

        #endregion

        #region Public Methods

        public NumericStringInfo Add(NumericStringInfo source)
        {
            if (IsNegative != source.IsNegative && !(IsZero || source.IsZero)) 
            //if ((IsZero || IsNegative) != (source.IsZero || source.IsNegative))
			{
                throw new InvalidOperationException("Can only Add two NumericStrings if they have the same sign.");
			}

            var carry = 0;
            var after = Add(_after, source._after, ref carry);

            var before = Add(_before, source._before, ref carry);

            if (carry != 0)
			{
                var temp = new int[before.Length + 1];
                temp[0] = carry;
                Array.Copy(before, 0, temp, 1, before.Length);
                before = temp;
			}

            var result = new NumericStringInfo(before, after, _isNegative);

            return result;
        }

        public string GetString()
        {
            var sb = new StringBuilder();

    //        if (_before.Length < 1)
    //        {
				//_ = sb.Append('0');
    //        }
    //        else
    //        {
    //            _ = sb.Append(_before.Select(x => (char)(x + 48)).ToArray());
    //        }

            if (_isNegative)
			{
                _ = sb.Append('-');
			}

            _ = sb.Append(_before.Select(x => (char)(x + 48)).ToArray());

            _ = sb.Append('.');

            if (_after.Length > 0)
			{
                _ = sb.Append(_after.Select(x => (char)(x + 48)).ToArray());
            }

            return sb.ToString();
        }

        public static string ConvertToFixedPoint(string s)
        {
            int dpPos;
            var exponent = 0;
            var ePos = s.IndexOf("E", StringComparison.InvariantCultureIgnoreCase);

            if (ePos != -1)
            {
                var eStr = s[(ePos + 1)..];
                s = s[0..ePos];

                exponent = (int)double.Parse(eStr, CultureInfo.InvariantCulture);

                dpPos = s.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                s = s.Remove(dpPos, 1);

                if (exponent < 0)
                {
                    s = "0." + new string('0', -1 * exponent - 1) + s;
                }
                else
                {
                    s = s.PadRight(exponent + 1, '0');
                    s = s.Substring(0, exponent + 1);

                    if (s.Length > exponent + 2)
                    {
                        s = s + "." + s.Substring(exponent + 2, s.Length - exponent + 2);
                    }
                }
            }

            return s;
        }

		#endregion

		#region Private Methods

		private int[] Add(int[] a, int[] b, ref int carry)
        {
            var result = new int[Math.Max(a.Length, b.Length)];

            if (a.Length > b.Length)
            {
                for (var i = a.Length - 1; i > b.Length - 1; i--)
                {
                    result[i] = AddDigitsWithCarry(ref carry, a[i]);
                }
            }
            else if (a.Length < b.Length)
            {
                for (var i = b.Length - 1; i > a.Length - 1; i--)
                {
                    result[i] = AddDigitsWithCarry(ref carry, b[i]);
                }
            }

            for (var i = Math.Min(a.Length, b.Length) - 1; i >= 0; i--)
            {
                result[i] = AddDigitsWithCarry(ref carry, a[i], b[i]);
            }

            return result;
        }

        private int AddDigitsWithCarry(ref int carry, params int[] digits)
		{
            var d = digits.Sum() + carry;
            if (d > 9)
            {
                d = d - 10;
                carry = 1;
            }
            else
            {
                carry = 0;
            }

            if (d > 9)
            {
                throw new InvalidOperationException("While AddDigitsWithCarry, the sum is > 19.");
            }

            return d;
        }

        private string ConditionForParse(string s)
		{
            s = s.Trim();

            if (s.StartsWith('.'))
            {
                s = "0" + s;
            }

            if (s.EndsWith('.'))
            {
                s = s + "0";
            }

            return s;
        }

		#endregion


	}
}
