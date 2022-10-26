﻿using MSS.Types;
using System.Collections;
using System.Numerics;

namespace MSetGenP
{
	public struct Smx : IEquatable<Smx>
	{
		#region Constructor

		public static readonly Smx Zero = new Smx(0, 0, 53);

		public Smx(RValue rValue) : this(rValue.Value, rValue.Exponent, rValue.Precision)
		{ }

		public Smx(RValue rValue, int precision) : this(rValue.Value, rValue.Exponent, precision)
		{ }

		public Smx(BigInteger bigInteger, int exponent, int precision)
		{
			Sign = bigInteger < 0 ? false : true;
			Mantissa = SmxMathHelper.ToPwULongs(bigInteger);
			Exponent = exponent;
			Precision = precision;
		}

		public Smx(bool sign, ulong[] mantissa, int exponent, int precision)
		{
			Sign = sign;
			Mantissa = (ulong[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision=precision;
		}

		#endregion

		#region Public Properties

		public bool Sign { get; set; }
		public ulong[] Mantissa { get; set; }
		public int Exponent { get; set; }
		public int Precision { get; set; } // Number of significant binary digits.

		#endregion

		#region Public Methods

		public RValue GetRValue()
		{
			var mantissa = SmxMathHelper.FromPwULongs(Mantissa);
			mantissa = Sign ? mantissa : -1 * mantissa;
			var result = new RValue(mantissa, Exponent, Precision);

			return result;
		}

		#endregion

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return obj is Smx smx && Equals(smx);
		}

		public bool Equals(Smx other)
		{
			return Sign == other.Sign &&
				((IStructuralEquatable)Mantissa).Equals(other.Mantissa, StructuralComparisons.StructuralEqualityComparer) &&
				Exponent == other.Exponent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Sign, Mantissa, Exponent);
		}

		public static bool operator ==(Smx left, Smx right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Smx left, Smx right)
		{
			return !(left == right);
		}

		#endregion
	}
}