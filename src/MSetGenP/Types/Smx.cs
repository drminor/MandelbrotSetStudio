using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public struct Smx : IEquatable<Smx>, ICloneable
	{
		#region Constructor

		public Smx(RValue rValue, byte bitsBeforeBP) : this(rValue.Value, rValue.Exponent, bitsBeforeBP, rValue.Precision)
		{ }

		public Smx(RValue rValue, byte bitsBeforeBP, int precision) : this(rValue.Value, rValue.Exponent, bitsBeforeBP, precision)
		{ }

		private Smx(BigInteger bigInteger, int exponent, byte bitsBeforeBP) : this(bigInteger, exponent, bitsBeforeBP, RMapConstants.DEFAULT_PRECISION)
		{ }

		private Smx(BigInteger bigInteger, int exponent, byte bitsBeforeBP, int precision)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			Sign = bigInteger < 0 ? false : true;
			Mantissa = ScalarMathHelper.ToPwULongs(bigInteger);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		public Smx(bool sign, ulong[] mantissa, int exponent, byte bitsBeforeBP, int precision)
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
			var result = ScalarMathHelper.CreateRValue(this); 
			return result;
		}

		public string GetStringValue()
		{
			var rValue = GetRValue();
			var strValue = RValueHelper.ConvertToString(rValue);

			return strValue;
		}

		//public override string ToString()
		//{
		//	var result = Sign
		//		? SmxHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}"
		//		: "-" + SmxHelper.GetDiagDisplay("m", Mantissa) + $" e:{Exponent}";

		//	return result;
		//}

		public override string ToString()
		{

			var result = Sign
				? ScalarMathHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}"
				: "-" + ScalarMathHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}";

			//var result = Sign
			//	? ScalarMathHelper.GetDiagDisplayHexBlocked("m", Mantissa) + $" e:{Exponent}"
			//	: "-" + ScalarMathHelper.GetDiagDisplayHexBlocked("m", Mantissa) + $" e:{Exponent}";

			return result;
		}

		#endregion


		object ICloneable.Clone()
		{
			return Clone();
		}

		public Smx Clone()
		{
			var result = new Smx(Sign, (ulong[])Mantissa.Clone(), Exponent, BitsBeforeBP, Precision);

			return result;
		}

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
