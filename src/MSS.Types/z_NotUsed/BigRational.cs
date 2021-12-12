using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;

namespace Test
{
	public struct BigRational : IComparable, IComparable<BigRational>, IEquatable<BigRational>
	{
		private BigInteger m_numerator;
		private BigInteger m_denominator;

		private static readonly BigRational s_brZero = new BigRational(BigInteger.Zero);
		private static readonly BigRational s_brOne = new BigRational(BigInteger.One);
		private static readonly BigRational s_brMinusOne = new BigRational(BigInteger.MinusOne);

		[StructLayout(LayoutKind.Explicit)]
		internal struct DoubleUlong
		{
			[FieldOffset(0)]
			public double dbl;
			[FieldOffset(0)]
			public ulong uu;
		}
		private const int DoubleMaxScale = 308;
		private static readonly BigInteger s_bnDoublePrecision = BigInteger.Pow(10, DoubleMaxScale);
		private static readonly BigInteger s_bnDoubleMaxValue = (BigInteger)double.MaxValue;
		private static readonly BigInteger s_bnDoubleMinValue = (BigInteger)double.MinValue;

		[StructLayout(LayoutKind.Explicit)]
		internal struct DecimalUInt32
		{
			[FieldOffset(0)]
			public decimal dec;
			[FieldOffset(0)]
			public int flags;
		}
		private const int DecimalScaleMask = 0x00FF0000;
		private const int DecimalSignMask = unchecked((int)0x80000000);
		private const int DecimalMaxScale = 28;
		private static readonly BigInteger s_bnDecimalPrecision = BigInteger.Pow(10, DecimalMaxScale);
		private static readonly BigInteger s_bnDecimalMaxValue = (BigInteger)decimal.MaxValue;
		private static readonly BigInteger s_bnDecimalMinValue = (BigInteger)decimal.MinValue;

		private const string c_solidus = @"/";

		public static BigRational Zero
		{
			get
			{
				return s_brZero;
			}
		}

		public static BigRational One
		{
			get
			{
				return s_brOne;
			}
		}

		public static BigRational MinusOne
		{
			get
			{
				return s_brMinusOne;
			}
		}

		public int Sign
		{
			get
			{
				return m_numerator.Sign;
			}
		}

		public BigInteger Numerator
		{
			get
			{
				return m_numerator;
			}
		}

		public BigInteger Denominator
		{
			get
			{
				return m_denominator;
			}
		}

		public BigInteger GetWholePart()
		{
			return BigInteger.Divide(m_numerator, m_denominator);
		}

		public BigRational GetFractionPart()
		{
			return new BigRational(BigInteger.Remainder(m_numerator, m_denominator), m_denominator);
		}

		public override bool Equals(object? obj)
		{
			if (obj == null)
				return false;

			if (!(obj is BigRational))
				return false;
			return Equals((BigRational)obj);
		}

		public override int GetHashCode()
		{
			return (m_numerator / Denominator).GetHashCode();
		}

		int IComparable.CompareTo(object? obj)
		{
			if (obj == null)
				return 1;
			if (!(obj is BigRational))
				throw new ArgumentException();
			return Compare(this, (BigRational)obj);
		}

		public int CompareTo(BigRational other)
		{
			return Compare(this, other);
		}

		public override string ToString()
		{
			StringBuilder ret = new StringBuilder();
			ret.Append(m_numerator.ToString("R", CultureInfo.InvariantCulture));
			ret.Append(c_solidus);
			ret.Append(Denominator.ToString("R", CultureInfo.InvariantCulture));
			return ret.ToString();
		}

		public bool Equals(BigRational other)
		{
			if (Denominator == other.Denominator)
			{
				return m_numerator == other.m_numerator;
			}
			else
			{
				return (m_numerator * other.Denominator) == (Denominator * other.m_numerator);
			}
		}

		public BigRational(BigInteger numerator)
		{
			m_numerator = numerator;
			m_denominator = BigInteger.One;
		}

		public BigRational(double value)
		{
			if (double.IsNaN(value))
			{
				throw new ArgumentException();
			}
			else if (double.IsInfinity(value))
			{
				throw new ArgumentException();
			}

			bool isFinite;
			int sign;
			int exponent;
			ulong significand;
			SplitDoubleIntoParts(value, out sign, out exponent, out significand, out isFinite);

			if (significand == 0)
			{
				this = Zero;
				return;
			}

			m_numerator = significand;
			m_denominator = BigInteger.One;

			if (exponent > 0)
			{
				m_numerator = m_numerator << exponent;
			}
			else if (exponent < 0)
			{
				m_denominator = m_denominator << -exponent;
			}
			if (sign < 0)
			{
				m_numerator = BigInteger.Negate(m_numerator);
			}
			Simplify();
		}

		public BigRational(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			if (bits == null || bits.Length != 4 || (bits[3] & ~(DecimalSignMask | DecimalScaleMask)) != 0 || (bits[3] & DecimalScaleMask) > (28 << 16))
			{
				throw new ArgumentException();
			}

			if (value == decimal.Zero)
			{
				this = Zero;
				return;
			}

			ulong ul = (((ulong)(uint)bits[2]) << 32) | ((ulong)(uint)bits[1]);
			m_numerator = (new BigInteger(ul) << 32) | (uint)bits[0];

			bool isNegative = (bits[3] & DecimalSignMask) != 0;
			if (isNegative)
			{
				m_numerator = BigInteger.Negate(m_numerator);
			}

			int scale = (bits[3] & DecimalScaleMask) >> 16;
			m_denominator = BigInteger.Pow(10, scale);

			Simplify();
		}

		public BigRational(BigInteger numerator, BigInteger denominator)
		{
			if (denominator.Sign == 0)
			{
				throw new DivideByZeroException();
			}
			else if (numerator.Sign == 0)
			{
				m_numerator = BigInteger.Zero;
				m_denominator = BigInteger.One;
			}
			else if (denominator.Sign < 0)
			{
				m_numerator = BigInteger.Negate(numerator);
				m_denominator = BigInteger.Negate(denominator);
			}
			else
			{
				m_numerator = numerator;
				m_denominator = denominator;
			}
			Simplify();
		}

		public BigRational(BigInteger whole, BigInteger numerator, BigInteger denominator)
		{
			if (denominator.Sign == 0)
			{
				throw new DivideByZeroException();
			}
			else if (numerator.Sign == 0 && whole.Sign == 0)
			{
				m_numerator = BigInteger.Zero;
				m_denominator = BigInteger.One;
			}
			else if (denominator.Sign < 0)
			{
				m_denominator = BigInteger.Negate(denominator);
				m_numerator = (BigInteger.Negate(whole) * m_denominator) + BigInteger.Negate(numerator);
			}
			else
			{
				m_denominator = denominator;
				m_numerator = (whole * denominator) + numerator;
			}
			Simplify();
		}

		public static BigRational Abs(BigRational r)
		{
			return (r.m_numerator.Sign < 0 ? new BigRational(BigInteger.Abs(r.m_numerator), r.Denominator) : r);
		}

		public static BigRational Negate(BigRational r)
		{
			return new BigRational(BigInteger.Negate(r.m_numerator), r.Denominator);
		}

		public static BigRational Invert(BigRational r)
		{
			return new BigRational(r.Denominator, r.m_numerator);
		}

		public static BigRational Add(BigRational x, BigRational y)
		{
			return x + y;
		}

		public static BigRational Subtract(BigRational x, BigRational y)
		{
			return x - y;
		}

		public static BigRational Multiply(BigRational x, BigRational y)
		{
			return x * y;
		}

		public static BigRational Divide(BigRational dividend, BigRational divisor)
		{
			return dividend / divisor;
		}

		public static BigRational Remainder(BigRational dividend, BigRational divisor)
		{
			return dividend % divisor;
		}

		public static BigRational DivRem(BigRational dividend, BigRational divisor, out BigRational remainder)
		{
			BigInteger ad = dividend.m_numerator * divisor.Denominator;
			BigInteger bc = dividend.Denominator * divisor.m_numerator;
			BigInteger bd = dividend.Denominator * divisor.Denominator;

			remainder = new BigRational(ad % bc, bd);
			return new BigRational(ad, bc);
		}
		public static BigInteger LeastCommonDenominator(BigRational x, BigRational y)
		{
			return (x.Denominator * y.Denominator) / BigInteger.GreatestCommonDivisor(x.Denominator, y.Denominator);
		}

		public static int Compare(BigRational r1, BigRational r2)
		{
			return BigInteger.Compare(r1.m_numerator * r2.Denominator, r2.m_numerator * r1.Denominator);
		}

		public static bool operator ==(BigRational x, BigRational y)
		{
			return Compare(x, y) == 0;
		}

		public static bool operator !=(BigRational x, BigRational y)
		{
			return Compare(x, y) != 0;
		}

		public static bool operator <(BigRational x, BigRational y)
		{
			return Compare(x, y) < 0;
		}

		public static bool operator <=(BigRational x, BigRational y)
		{
			return Compare(x, y) <= 0;
		}

		public static bool operator >(BigRational x, BigRational y)
		{
			return Compare(x, y) > 0;
		}

		public static bool operator >=(BigRational x, BigRational y)
		{
			return Compare(x, y) >= 0;
		}

		public static BigRational operator +(BigRational r)
		{
			return r;
		}

		public static BigRational operator -(BigRational r)
		{
			return new BigRational(-r.m_numerator, r.Denominator);
		}

		public static BigRational operator ++(BigRational r)
		{
			return r + One;
		}

		public static BigRational operator --(BigRational r)
		{
			return r - One;
		}

		public static BigRational operator +(BigRational r1, BigRational r2)
		{
			return new BigRational((r1.m_numerator * r2.Denominator) + (r1.Denominator * r2.m_numerator), (r1.Denominator * r2.Denominator));
		}

		public static BigRational operator -(BigRational r1, BigRational r2)
		{
			return new BigRational((r1.m_numerator * r2.Denominator) - (r1.Denominator * r2.m_numerator), (r1.Denominator * r2.Denominator));
		}

		public static BigRational operator *(BigRational r1, BigRational r2)
		{
			return new BigRational((r1.m_numerator * r2.m_numerator), (r1.Denominator * r2.Denominator));
		}

		public static BigRational operator /(BigRational r1, BigRational r2)
		{
			return new BigRational((r1.m_numerator * r2.Denominator), (r1.Denominator * r2.m_numerator));
		}

		public static BigRational operator %(BigRational r1, BigRational r2)
		{
			return new BigRational((r1.m_numerator * r2.Denominator) % (r1.Denominator * r2.m_numerator), (r1.Denominator * r2.Denominator));
		}

		public static explicit operator sbyte(BigRational value)
		{
			return (sbyte)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}


		public static explicit operator ushort(BigRational value)
		{
			return (ushort)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator uint(BigRational value)
		{
			return (uint)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator ulong(BigRational value)
		{
			return (ulong)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator byte(BigRational value)
		{
			return (byte)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator short(BigRational value)
		{
			return (short)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator int(BigRational value)
		{
			return (int)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator long(BigRational value)
		{
			return (long)(BigInteger.Divide(value.m_numerator, value.m_denominator));
		}

		public static explicit operator BigInteger(BigRational value)
		{
			return BigInteger.Divide(value.m_numerator, value.m_denominator);
		}

		public static explicit operator float(BigRational value)
		{
			return (float)((double)value);
		}

		public static explicit operator double(BigRational value)
		{
			if (SafeCastToDouble(value.m_numerator) && SafeCastToDouble(value.m_denominator))
			{
				return (double)value.m_numerator / (double)value.m_denominator;
			}

			BigInteger denormalized = (value.m_numerator * s_bnDoublePrecision) / value.m_denominator;
			if (denormalized.IsZero)
				return (value.Sign < 0) ? BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000)) : 0d;

			double result = 0;
			bool isDouble = false;
			int scale = DoubleMaxScale;

			while (scale > 0)
			{
				if (!isDouble)
				{
					if (SafeCastToDouble(denormalized))
					{
						result = (double)denormalized;
						isDouble = true;
					}
					else
					{
						denormalized = denormalized / 10;
					}
				}
				result = result / 10;
				scale--;
			}

			if (!isDouble)
				return (value.Sign < 0) ? double.NegativeInfinity : double.PositiveInfinity;
			else
				return result;
		}

		public static explicit operator decimal(BigRational value)
		{
			if (SafeCastToDecimal(value.m_numerator) && SafeCastToDecimal(value.m_denominator))
			{
				return (decimal)value.m_numerator / (decimal)value.m_denominator;
			}

			BigInteger denormalized = (value.m_numerator * s_bnDecimalPrecision) / value.m_denominator;
			if (denormalized.IsZero)
			{
				return decimal.Zero;
			}
			for (int scale = DecimalMaxScale; scale >= 0; scale--)
			{
				if (!SafeCastToDecimal(denormalized))
				{
					denormalized = denormalized / 10;
				}
				else
				{
					DecimalUInt32 dec = new DecimalUInt32();
					dec.dec = (decimal)denormalized;
					dec.flags = (dec.flags & ~DecimalScaleMask) | (scale << 16);
					return dec.dec;
				}
			}
			throw new OverflowException();
		}

		public static implicit operator BigRational(sbyte value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(ushort value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(uint value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(ulong value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(byte value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(short value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(int value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(long value)
		{
			return new BigRational((BigInteger)value);
		}

		public static implicit operator BigRational(BigInteger value)
		{
			return new BigRational(value);
		}

		public static implicit operator BigRational(float value)
		{
			return new BigRational((double)value);
		}

		public static implicit operator BigRational(double value)
		{
			return new BigRational(value);
		}

		public static implicit operator BigRational(decimal value)
		{
			return new BigRational(value);
		}

		private void Simplify()
		{
			if (m_numerator == BigInteger.Zero)
			{
				m_denominator = BigInteger.One;
			}

			BigInteger gcd = BigInteger.GreatestCommonDivisor(m_numerator, m_denominator);
			if (gcd > BigInteger.One)
			{
				m_numerator = m_numerator / gcd;
				m_denominator = Denominator / gcd;
			}
		}

		private static bool SafeCastToDouble(BigInteger value)
		{
			return s_bnDoubleMinValue <= value && value <= s_bnDoubleMaxValue;
		}

		private static bool SafeCastToDecimal(BigInteger value)
		{
			return s_bnDecimalMinValue <= value && value <= s_bnDecimalMaxValue;
		}

		private static void SplitDoubleIntoParts(double dbl, out int sign, out int exp, out ulong man, out bool isFinite)
		{
			DoubleUlong du;
			du.uu = 0;
			du.dbl = dbl;

			sign = 1 - ((int)(du.uu >> 62) & 2);
			man = du.uu & 0x000FFFFFFFFFFFFF;
			exp = (int)(du.uu >> 52) & 0x7FF;
			if (exp == 0)
			{
				isFinite = true;
				if (man != 0)
					exp = -1074;
			}
			else if (exp == 0x7FF)
			{
				isFinite = false;
				exp = int.MaxValue;
			}
			else
			{
				isFinite = true;
				man |= 0x0010000000000000;
				exp -= 1075;
			}
		}

		private static double GetDoubleFromParts(int sign, int exp, ulong man)
		{
			DoubleUlong du;
			du.dbl = 0;

			if (man == 0)
			{
				du.uu = 0;
			}
			else
			{
				int cbitShift = CbitHighZero(man) - 11;
				if (cbitShift < 0)
					man >>= -cbitShift;
				else
					man <<= cbitShift;

				exp += 1075;

				if (exp >= 0x7FF)
				{
					du.uu = 0x7FF0000000000000;
				}
				else if (exp <= 0)
				{
					exp--;
					if (exp < -52)
					{
						du.uu = 0;
					}
					else
					{
						du.uu = man >> -exp;
					}
				}
				else
				{
					du.uu = (man & 0x000FFFFFFFFFFFFF) | ((ulong)exp << 52);
				}
			}

			if (sign < 0)
			{
				du.uu |= 0x8000000000000000;
			}

			return du.dbl;
		}

		private static int CbitHighZero(ulong uu)
		{
			if ((uu & 0xFFFFFFFF00000000) == 0)
				return 32 + CbitHighZero((uint)uu);
			return CbitHighZero((uint)(uu >> 32));
		}

		private static int CbitHighZero(uint u)
		{
			if (u == 0)
				return 32;

			int cbit = 0;
			if ((u & 0xFFFF0000) == 0)
			{
				cbit += 16;
				u <<= 16;
			}
			if ((u & 0xFF000000) == 0)
			{
				cbit += 8;
				u <<= 8;
			}
			if ((u & 0xF0000000) == 0)
			{
				cbit += 4;
				u <<= 4;
			}
			if ((u & 0xC0000000) == 0)
			{
				cbit += 2;
				u <<= 2;
			}
			if ((u & 0x80000000) == 0)
				cbit += 1;
			return cbit;
		}

	}
}
