using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public class SmxMathHelper
	{
		#region Multiply and Square

		public static Smx Multiply(Smx a, Smx b)
		{
			var mantissa = Multiply(a.Mantissa, b.Mantissa);

			var sign = a.Sign == b.Sign;
			var exponent = a.Exponent + b.Exponent;
			var precision = Math.Min(a.Precision, b.Precision);

			Smx result = BuildSmx(sign, mantissa, exponent, precision);

			return result;
		}

		public static ulong[] Multiply(ulong[] ax, ulong[] bx)
		{
			Debug.WriteLine(GetDiagDisplay("ax", ax));
			Debug.WriteLine(GetDiagDisplay("bx", bx));

			var seive = new ulong[ax.Length * bx.Length];

			var fullMantissa = new ulong[ax.Length + bx.Length];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = 0; i < bx.Length; i++)
				{
					var product = ax[j] * bx[i];
					seive[j * bx.Length + i] = product;

					var resultPtr = j + i;  // 0, 1, 1, 2

					var lo = Split(product, out var hi);        //  2 x 2						3 x 3										4 x 4
					fullMantissa[resultPtr] += lo;              // 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6 
					fullMantissa[resultPtr + 1] += hi;          // 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7
				}
			}

			var splitSieve = Split(seive);
			Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, bx.Length * 2));

			Debug.WriteLine(GetDiagDisplay("result", fullMantissa));

			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			for (int i = 1; i < fullMantissa.Length - 1; i++)
			{
				var lo = Split(fullMantissa[i], out var hi);
				fullMantissa[i] = lo;
				fullMantissa[i + 1] += hi;
			}

			return fullMantissa;
		}

		public static Smx Square(Smx a)
		{
			var mantissa = Square(a.Mantissa);
			Smx result = BuildSmx(true, mantissa, a.Exponent * 2, a.Precision);

			return result;
		}

		public static ulong[] Square(ulong[] ax)
		{
			Debug.WriteLine(GetDiagDisplay("ax", ax));

			var seive = new ulong[(int)Math.Pow(ax.Length, 2)];

			var fullMantissa = new ulong[ax.Length * 2];

			// Calculate the partial 32-bit products and accumulate these into 64-bit result 'bins' where each bin can hold the hi (carry) and lo (final digit)
			for (int j = 0; j < ax.Length; j++)
			{
				for (int i = 0; i < ax.Length; i++)
				{
					ulong product;

					if (j > i)
					{
						continue;
					}

					product = ax[j] * ax[i];
					seive[j * ax.Length + i] = product;

					if (j < i)
					{
						seive[i * ax.Length + j] = product;
						product *= 2;
					}

					var resultPtr = j + i;  // 0, 1, 1, 2

					var lo = Split(product, out var hi);
					fullMantissa[resultPtr] += lo;
					fullMantissa[resultPtr + 1] += hi;
				}
			}

			var splitSieve = Split(seive);
			Debug.WriteLine(GetDiagDisplay("sieve", splitSieve, ax.Length * 2));

			Debug.WriteLine(GetDiagDisplay("result", fullMantissa));

			// Process the carry portion of each result bin.
			// This will leave each result bin with a value <= 2^32 for the final digit.
			for (int i = 1; i < fullMantissa.Length - 1; i++)
			{
				var lo = Split(fullMantissa[i], out var hi);
				fullMantissa[i] = lo;
				fullMantissa[i + 1] += hi;
			}

			return fullMantissa;
		}

		/* What partial product gets added to which bin

			//  2 x 2						3 x 3										4 x 4
			// 0, 1,   1, 2		 0, 1, 2,   1, 2, 3,  2, 3  4		0, 1, 2, 3,   1, 2, 3, 4,    2, 3, 4, 5,    3, 4, 5, 6 
			// 1, 2,   2, 3      1, 2, 3,   2, 3, 4,  3, 4, 5       1, 2, 3, 4,   2, 3, 4, 5,    3, 4, 5, 6,    4, 5, 6, 7

				2 x 2
			index a index b	index c	on, or below the diagonal 
			0		0		0		D		E
			0		1		1		B		E
			1		0		1		A		S
			1		1		2		D		E

				3 x 3
			0		0		0		D		E
			0		1		1		B		E
			0		2		2		B		E

			1		0		1		A		S
			1		1		2		D		E
			1		2		3		B		E

			2		0		2		A		S
			2		1		3		A		S
			2		2		4		D		E

				4 x 4
			0		0		0		D		E
			0		1		1		B		E
			0		2		2		B		E	**
			0		3		3		B		E

			1		0		1		A		S
			1		1		2		D		E
			1		2		3		B		E
			1		3		4		B		E

			2		0		2		A		S	**
			2		1		3		A		S
			2		2		4		D		E
			2		3		5		B		E

			3		0		3		A		S
			3		1		4		A		S
			3		2		5		A		S
			3		3		6		D		E



		*/

		#endregion

		#region Build Smx Support

		private static Smx BuildSmx(bool sign, ulong[] mantissa, int exponent, int precision)
		{
			Smx result;

			mantissa = TrimLeadingZeros(mantissa);

			var numBaseUInt32Digits = BigIntegerHelper.GetNumBaseUIntDigits(precision);
			//numBaseUInt32Digits++; // Include an extra digit for 'working room' -- Will round more aggressively when returning final value.

			if (mantissa.Length > numBaseUInt32Digits)
			{
				mantissa = FillMsb(mantissa, out var shiftAmount);
				exponent -= shiftAmount;

				var adjMantissa = Round(mantissa, numBaseUInt32Digits);
				var adjExponent = exponent + 32 * (mantissa.Length - numBaseUInt32Digits);	
				result = new Smx(sign, adjMantissa, adjExponent, precision);
			}
			else
			{
				result = new Smx(sign, mantissa, exponent, precision);
			}

			return result;
		}

		private static ulong[] Round(ulong[] mantissa, int numBaseUInt32Digits)
		{
			var result = new ulong[numBaseUInt32Digits];
			var startIndex = mantissa.Length - numBaseUInt32Digits;

			Debug.Assert(startIndex > 0);

			Array.Copy(mantissa, startIndex, result, 0, numBaseUInt32Digits);

			if (mantissa[startIndex - 1] >= Math.Pow(2, 16))
			{
				result[0] += 1;
			}

			return result;
		}

		private readonly static ulong MAX_MSB = (ulong)Math.Pow(2, 30);
		private readonly static ulong MAX_DIGIT_VALUE = (ulong)Math.Pow(2, 32);

		public static ulong[] FillMsb(ulong[] mantissa, out int shiftAmount)
		{
			shiftAmount = 0;
			var msb = mantissa[^1];

			while((msb << 1) < MAX_MSB)
			{
				msb <<= 1; // Multiply x 2
				shiftAmount++;
			}

			if (shiftAmount == 0)
			{
				return mantissa;
			}

			var mTest = Math.Pow(2, shiftAmount);

			Debug.Assert(mTest <= MAX_DIGIT_VALUE);

			var multiplyer = (ulong)Math.Pow(2, shiftAmount);

			var result = new ulong[mantissa.Length];

			for (int i = 0; i < mantissa.Length; i++)
			{
				var td = mantissa[i] * multiplyer;

				if (td > ulong.MaxValue)
				{
					throw new OverflowException($"FillMsb overflowed digit at index {i} while multiplying");
				}

				result[i] = (ulong)td;
			}

			for (int i = 0; i < mantissa.Length - 1; i++)
			{
				var lo = Split(result[i], out var hi);
				result[i] = lo;

				var hiTemp = result[i + 1] + hi;
				result[i + 1] = hiTemp;
			}

			if (result[^1] > MAX_DIGIT_VALUE)
			{
				throw new OverflowException($"FillMsb overflowed the most signifcant digit.");
			}

			return result;
		}

		public static ulong[] TrimLeadingZeros(ulong[] mantissa)
		{
			var i = mantissa.Length;
			for(; i > 0; i--)
			{
				if (mantissa[i - 1] != 0)
				{
					break;
				}	
			}

			if (i < mantissa.Length)
			{
				var result = new ulong[i];
				Array.Copy(mantissa, 0, result, 0, i);
				return result;
			}
			else
			{
				return mantissa;
			}

		}

		#endregion

		#region Split and Pack 

		private static ulong[] Split(ulong[] packedValues)
		{
			var result = new ulong[packedValues.Length * 2];

			for(int i = 0; i < packedValues.Length; i++)
			{
				var lo = Split(packedValues[i], out var hi);
				result[2 * i] = lo;
				result[2 * i + 1] = hi;
			}

			return result;
		}

		private static ulong Split(ulong x, out ulong hi)
		{
			var hiTemp = (uint)(x >> 32); // Move bits 32-63 -> bits 0-31
			hi = hiTemp;
			var lo = (uint)x;
			return lo;
		}

		private static ulong[] Pack(ulong[] splitValues)
		{
			Debug.Assert(splitValues.Length % 2 == 0);	

			var result = new ulong[splitValues.Length / 2];

			for (int i = 0; i < splitValues.Length; i += 2)
			{
				result[i / 2] = splitValues[i] + UL_UINT_FACTOR * splitValues[i + 1];
			}

			return result;
		}

		// Values are ordered from least significant to most significant.
		public static ulong[] GetPwULongs(ulong[] values)
		{
			var tResult = new List<ulong>();

			foreach (ulong value in values)
			{
				if (value >= uint.MaxValue)
				{
					var lo = Split(value, out var hi);
					tResult.Add(lo);
					tResult.Add(hi);
				}
				else
				{
					tResult.Add(value);
				}
			}

			return tResult.ToArray();
		}

		#endregion

		#region ToString Support

		private static string GetDiagDisplay(string name, ulong[] values, int stride)
		{
			var rowCnt = values.Length / stride;

			var sb = new StringBuilder();
			sb.AppendLine($"{name}:");

			for(int i = 0; i < rowCnt; i++)
			{
				var rowValues = new ulong[stride];

				Array.Copy(values, i * stride, rowValues, 0, stride);
				sb.AppendLine(GetDiagDisplay($"Row {i}", rowValues));
			}

			return sb.ToString();
		}

		private static string GetDiagDisplay(string name, ulong[] values)
		{
			var strAry = GetStrArray(values);

			return $"{name}: {string.Join("; ", strAry)}";
		}

		private static string GetHiLoDiagDisplay(string name, ulong[] values)
		{
			Debug.Assert(values.Length % 2 == 0);

			var strAry = GetStrArray(values);
			var pairs = new string[values.Length / 2];

			for (int i = 0; i < values.Length; i += 2)
			{
				pairs[i / 2] = $"{strAry[i]}, {strAry[i+1]}";
			}

			return $"{name}: {string.Join("; ", pairs)}";
		}

		private static string[] GetStrArray(ulong[] values)
		{
			var result = values.Select(x => x.ToString()).ToArray();
			return result;
		}

		#endregion

		#region To ULong Support

		// Integer used to convert BigIntegers to/from array of ulongs.
		private static readonly BigInteger BI_ULONG_FACTOR = BigInteger.Pow(2, 64);

		// Integer used to convert BigIntegers to/from array of ulongs containing partial-words
		private static readonly BigInteger BI_UINT_FACTOR = BigInteger.Pow(2, 32);

		private static readonly ulong UL_UINT_FACTOR = (ulong)Math.Pow(2, 32);

		public static ulong[] ToULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > ulong.MaxValue)
			{
				hi = BigInteger.DivRem(hi, BI_ULONG_FACTOR, out var lo);
				tResult.Add((ulong)lo);
			}

			tResult.Add((ulong)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromULongs(ulong[] values)
		{
			var result = BigInteger.Zero;

			for (int i = values.Length - 1; i >= 0; i--)
			{
				result *= BI_ULONG_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region Convert to Partial-Word ULongs

		public static ulong[] ToPwULongs(BigInteger bi)
		{
			var tResult = new List<ulong>();
			var hi = BigInteger.Abs(bi);

			while (hi > uint.MaxValue)
			{
				hi = BigInteger.DivRem(hi, BI_UINT_FACTOR, out var lo);
				tResult.Add((ulong)lo);
			}

			tResult.Add((ulong)hi);

			return tResult.ToArray();
		}

		public static BigInteger FromPwULongs(ulong[] values)
		{
			var result = BigInteger.Zero;

			for (int i = values.Length - 1; i >= 0; i--)
			{
				result *= BI_UINT_FACTOR;
				result += values[i];
			}

			return result;
		}

		#endregion

		#region RValue Comparison

		public static bool AreClose(RValue a, RValue b)
		{
			var aNrm = RNormalizer.Normalize(a, b, out var bNrm);
			var strA = aNrm.Value.ToString();
			var strB = bNrm.Value.ToString();

			var precision = Math.Min(a.Precision, b.Precision);
			var numberOfDecimalDigits = (int)Math.Round(precision * Math.Log10(2), MidpointRounding.ToZero);

			if (strA.Length < numberOfDecimalDigits)
			{
				Debug.WriteLine($"Value A has {strA.Length} decimal digits which is less than {numberOfDecimalDigits}.");
				return false;
			}

			if (strB.Length < numberOfDecimalDigits)
			{
				Debug.WriteLine($"Value B has {strB.Length} decimal digits which is less than {numberOfDecimalDigits}.");
				return false;
			}

			//numberOfDecimalDigits = Math.Min(numberOfDecimalDigits, strA.Length);
			//numberOfDecimalDigits = Math.Min(numberOfDecimalDigits, strB.Length);

			Debug.WriteLine($"AreClose is comparing {numberOfDecimalDigits} characters of {strA} and {strB}.");

			strA = strA.Substring(0, numberOfDecimalDigits);
			strB = strB.Substring(0, numberOfDecimalDigits);

			Debug.WriteLine($"AreClose is comparing {strA} with {strB} and returning {strA == strB}.");

			return strA == strB;
		}

		#endregion
	}
}
