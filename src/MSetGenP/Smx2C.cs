using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public struct Smx2C : IEquatable<Smx2C>
	{
		#region Constructor

		//public Smx(RValue rValue) : this(rValue.Value, rValue.Exponent, rValue.Precision, bitsBeforeBP: 0)
		//{ }

		public Smx2C(RValue rValue, int precision, int bitsBeforeBP) : this(rValue.Value, rValue.Exponent, precision, bitsBeforeBP)
		{ }

		private Smx2C(BigInteger bigInteger, int exponent, int precision, int bitsBeforeBP)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			Sign = bigInteger < 0 ? false : true;
			var un2Cmantissa = SmxHelper.ToPwULongs(bigInteger);

			Mantissa = Sign ? un2Cmantissa : SmxHelper.ConvertTo2C(un2Cmantissa, Sign);

			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		public Smx2C(bool sign, ulong[] mantissa, int exponent, int precision, int bitsBeforeBP)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			ValidatePWValues(mantissa);

			Sign = sign;
			Mantissa = (ulong[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		private static void ValidatePWValues(ulong[] mantissa)
		{
			if (SmxHelper.CheckPW2CValues(mantissa))
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

		public int BitsBeforeBP { get; init; }

		public bool IsZero => !Mantissa.Any(x => x > 0);
		public int LimbCount => Mantissa.Length;

		#endregion

		#region Public Methods

		public Smx ConvertToSmx()
		{
			var un2cMantissa = SmxHelper.ConvertFrom2C(Mantissa);
			var result = new Smx(Sign, un2cMantissa, Exponent, Precision, BitsBeforeBP);
			return result;
		}

		public RValue GetRValue()
		{
			var un2CSmx = ConvertToSmx();
			var result = SmxHelper.GetRValue(un2CSmx); 
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
				? SmxHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}"
				: "-" + SmxHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}";

			return result;
		}

		#endregion

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return obj is Smx2C smx && Equals(smx);
		}

		public bool Equals(Smx2C other)
		{
			return Sign == other.Sign &&
				((IStructuralEquatable)Mantissa).Equals(other.Mantissa, StructuralComparisons.StructuralEqualityComparer) &&
				Exponent == other.Exponent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Sign, Mantissa, Exponent);
		}

		public static bool operator ==(Smx2C left, Smx2C right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Smx2C left, Smx2C right)
		{
			return !(left == right);
		}

		#endregion
	}
}
