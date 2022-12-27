using MSS.Common;
using MSS.Types;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace MSetGenP
{
	public struct Smx2C : IEquatable<Smx2C>, ICloneable
	{
		#region Constructor

		public Smx2C(RValue rValue, byte bitsBeforeBP) : this(rValue.Value, rValue.Exponent, bitsBeforeBP, rValue.Precision)
		{ }

		public Smx2C(RValue rValue, byte bitsBeforeBP, int precision) : this(rValue.Value, rValue.Exponent, bitsBeforeBP, precision)
		{ }

		private Smx2C(BigInteger bigInteger, int exponent, byte bitsBeforeBP) : this(bigInteger, exponent, bitsBeforeBP, RMapConstants.DEFAULT_PRECISION)
		{ }

		private Smx2C(BigInteger bigInteger, int exponent, byte bitsBeforeBP, int precision)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			Sign = bigInteger < 0 ? false : true;
			var un2Cmantissa = ScalarMathHelper.ToPwULongs(bigInteger);

			Mantissa = ScalarMathHelper.ConvertTo2C(un2Cmantissa, Sign);

			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		public Smx2C(bool sign, ulong[] mantissa, int exponent, byte bitsBeforeBP, int precision)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			//ValidatePWValues(mantissa);

			Sign = sign;
			Mantissa = (ulong[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);

			var cSign = ScalarMathHelper.GetSign(Mantissa);

			Debug.Assert(cSign == sign, "Signs don't match in Constructor of Smx2C.");

			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		//[Conditional("DETAIL")]
		//private static void ValidatePWValues(ulong[] mantissa)
		//{
		//	if (ScalarMathHelper.CheckPW2CValues(mantissa))
		//	{
		//		throw new ArgumentException($"Cannot create a Smx from an array of ulongs where any of the values is greater than MAX_DIGIT.");
		//	}
		//}

		#endregion

		#region Public Properties

		public bool Sign { get; init; }
		public ulong[] Mantissa { get; init; }
		public int Exponent { get; init; }
		public int Precision { get; init; } // Number of significant binary digits.

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

		//public Smx ConvertToSmx()
		//{
		//	var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(Mantissa.Length), 4u);

		//	var result = scalarMath2C.Convert(this);

		//	return result;
		//}

		public override string ToString()
		{
			var result = Sign
				? ScalarMathHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}"
				: "-" + ScalarMathHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}";

			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Smx2C Clone()
		{
			var result = new Smx2C(Sign, (ulong[]) Mantissa.Clone(), Exponent, BitsBeforeBP, Precision);

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
