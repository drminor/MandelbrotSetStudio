using System;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Diagnostics;

namespace MSetExplorer
{
    internal class NumericStringInfo
    {
        private readonly int[] _before;
        private readonly int[] _after;

        public NumericStringInfo(int[] before, int[] after)
		{
            _before = before;
            _after = after;
		}

        public NumericStringInfo(string s)
        {
            if (s.StartsWith('-'))
            {
                s = s[1..];
            }

            if (s.StartsWith('.'))
            {
                s = "0" + s;
            }
            else
            {
                if (s.EndsWith('.'))
                {
                    s = s + "0";
                }
            }

            int dpPos;
            var exponent = 0;
            var ePos = s.IndexOf("E", StringComparison.InvariantCultureIgnoreCase);

            if (ePos != -1)
			{
                exponent = (int) double.Parse(s[(ePos + 1)..], CultureInfo.InvariantCulture);
                s = s.Substring(0, ePos);
                dpPos = s.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                s = s.Remove(dpPos, 1);

                if (exponent < 0)
				{
                    s = "0." + new string('0', -1 * exponent - 1) + s;
				}
                else
				{
                    s = s.Substring(0, exponent + 1) + "." + s.Substring(exponent + 2, s.Length - exponent + 2);
				}

			}

            var sCa = s.ToCharArray();

            dpPos = s.IndexOf(".", StringComparison.InvariantCultureIgnoreCase);

            if (dpPos == -1)
            {
                _before = sCa.Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
                _after = Array.Empty<int>();
            }
            else
            {
                _before = sCa.Take(dpPos).Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
                _after = sCa.Skip(dpPos + 1).Select(x => CharUnicodeInfo.GetDecimalDigitValue(x)).ToArray();
            }




        }

        public int PlacesBeforeDp => _before.Length;
        public int PlacesAfterDp => _after.Length;

        public string SValue => GetString();

        public NumericStringInfo Add(NumericStringInfo source)
        {
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

            var result = new NumericStringInfo(before, after);

            return result;
        }

        private int[] Add(int[] a, int[] b, ref int carry)
		{
            var result = new int[Math.Max(a.Length, b.Length)];

            if (a.Length > b.Length)
			{
                for (var i = a.Length - 1; i > b.Length - 1; i--)
				{
                    var d = a[i] + carry;
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
                        Debug.WriteLine("Here");
                    }

                    result[i] = d;
                }
            }
            else if (a.Length < b.Length)
			{
                for (var i = b.Length - 1; i > a.Length - 1; i--)
                {
                    var d = b[i] + carry;
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
                        Debug.WriteLine("Here");
					}

                    result[i] = d;
                }
            }
    
            for (var i = Math.Min(a.Length, b.Length) - 1; i >= 0; i--)
			{
                var d = a[i] + b[i] + carry;

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
                    Debug.WriteLine("Here");
                }

                result[i] = d;
            }

            return result;
		}

        public string GetString()
        {
            var sb = new StringBuilder();

            if (_before.Length < 1)
            {
				_ = sb.Append('0');
            }
            else
            {
                foreach (var ic in _before)
                {
					_ = sb.Append((char)(ic + 48));
                }
            }

			_ = sb.Append('.');

            foreach (var ic in _after)
            {
				_ = sb.Append((char)(ic + 48));
            }

            return sb.ToString();
        }



    }
}
