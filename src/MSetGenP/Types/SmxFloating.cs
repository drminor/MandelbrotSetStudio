using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Numerics;

namespace MSetGenP
{
	public struct SmxFloating : IEquatable<SmxFloating>
	{
		#region Constructor

		//public static readonly Smx Zero = new Smx(true, new ulong[] { 0 }, 1, 1000, 0);

		public SmxFloating(RValue rValue) : this(rValue.Value, rValue.Exponent, bitsBeforeBP: 0, precision: rValue.Precision)
		{ }

		public SmxFloating(RValue rValue, int precision) : this(rValue.Value, rValue.Exponent, bitsBeforeBP: 0, precision: precision)
		{ }

		public SmxFloating(BigInteger bigInteger, int exponent, byte bitsBeforeBP, int precision)
		{
			Sign = bigInteger < 0 ? false : true;
			Mantissa = ScalarMathHelper.ToPwULongs(bigInteger);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		public SmxFloating(bool sign, ulong[] mantissa, int exponent, byte bitsBeforeBP, int precision)
		{
			ValidatePWValues(mantissa);

			Sign = sign;
			Mantissa = (ulong[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		private static void ValidatePWValues(ulong[] mantissa)
		{
			if (ScalarMathHelper.CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Cannot create a Smx from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			}
		}

		#endregion

		#region Public Properties

		public bool Sign { get; init; }
		public ulong[] Mantissa { get; init; }
		public int Exponent { get; init; }
		public int Precision { get; init; } // Number of significant binary digits.

		// TODO: Create a new class for Fixed Point values. Currently this class is being used to representg Fixed Point values as well as Floating Point values.
		public byte BitsBeforeBP { get; init; }

		public bool IsZero => !Mantissa.Any(x => x > 0);
		public int LimbCount => Mantissa.Length;

		#endregion

		#region Public Methods

		public RValue GetRValue()
		{
			var result = ScalarMathFloating.GetRValue(this); 
			return result;
		}

		public string GetStringValue()
		{
			var rValue = GetRValue();
			var strValue = RValueHelper.ConvertToString(rValue);

			return strValue;
		}

		public override string ToString()
		{
			var result = Sign
				? ScalarMathHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}"
				: "-" + ScalarMathHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}";

			return result;
		}

		#endregion

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return obj is Smx smx && Equals(smx);
		}

		public bool Equals(SmxFloating other)
		{
			return Sign == other.Sign &&
				((IStructuralEquatable)Mantissa).Equals(other.Mantissa, StructuralComparisons.StructuralEqualityComparer) &&
				Exponent == other.Exponent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Sign, Mantissa, Exponent);
		}

		public static bool operator ==(SmxFloating left, SmxFloating right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SmxFloating left, SmxFloating right)
		{
			return !(left == right);
		}

		#endregion
	}
}
