using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Numerics;
using static MongoDB.Driver.WriteConcern;

namespace MSetGenP
{
	public struct Smx : IEquatable<Smx>
	{
		#region Constructor

		public static readonly Smx Zero = new Smx(true, new ulong[] { 0 }, 0, 1000);

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
			ValidatePWValues(mantissa);

			Sign = sign;
			Mantissa = (ulong[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision = precision;
		}

		private static void ValidatePWValues(ulong[] mantissa)
		{
			if (SmxMathHelper.CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Cannot create a Smx from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			}
		}

		#endregion

		#region Public Properties

		public bool Sign { get; set; }
		public ulong[] Mantissa { get; set; }
		public int Exponent { get; set; }
		public int Precision { get; set; } // Number of significant binary digits.

		public bool IsZero => Mantissa.Length == 1 && Mantissa[0] == 0;

		#endregion

		#region Public Methods

		public RValue GetRValue()
		{
			var biValue = SmxMathHelper.FromPwULongs(Mantissa);
			biValue = Sign ? biValue : -1 * biValue;
			var result = new RValue(biValue, Exponent, Precision);

			return result;
		}

		public string GetStringValue()
		{
			var rValue = GetRValue();
			var strValue = RValueHelper.ConvertToString(rValue);

			return strValue;
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
