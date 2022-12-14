using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public struct Smx : IEquatable<Smx>
	{
		#region Constructor

		//public Smx(RValue rValue) : this(rValue.Value, rValue.Exponent, rValue.Precision, bitsBeforeBP: 0)
		//{ }

		public Smx(RValue rValue, int precision, int bitsBeforeBP) : this(rValue.Value, rValue.Exponent, precision, bitsBeforeBP)
		{ }

		private Smx(BigInteger bigInteger, int exponent, int precision, int bitsBeforeBP)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			Sign = bigInteger < 0 ? false : true;
			Mantissa = SmxHelper.ToPwULongs(bigInteger);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		public Smx(bool sign, ulong[] mantissa, int exponent, int precision, int bitsBeforeBP)
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
			if (SmxHelper.CheckPWValues(mantissa))
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
		public int BitsBeforeBP { get; init; }

		public bool IsZero => !Mantissa.Any(x => x > 0);
		public int LimbCount => Mantissa.Length;

		#endregion

		#region Public Methods

		public RValue GetRValue()
		{
			var result = SmxHelper.GetRValue(this); 
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
				? SmxMathHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}"
				: "-" + SmxMathHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}";

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
