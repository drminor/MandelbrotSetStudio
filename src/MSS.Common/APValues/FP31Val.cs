using MSS.Common;
using MSS.Common.APValSupport;
using MSS.Types;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MSS.Common.APValues
{
	public struct FP31Val : IEquatable<FP31Val>, ICloneable
	{
		#region Constructor

		public FP31Val(bool sign, uint[] mantissa, int exponent, byte bitsBeforeBP, int precision)
		{
			if (exponent == 1)
			{
				Debug.WriteLine("WARNING the exponent is 1.");
			}

			//ValidatePWValues(mantissa);

			Sign = sign;
			Mantissa = (uint[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);

			var cSign = FP31ValHelper.GetSign(Mantissa);

			if (cSign != sign)
			{
				throw new ArgumentException("Signs don't match in Constructor of Smx2C.");
			}

			//Debug.Assert(cSign == sign, "Signs don't match in Constructor of Smx2C.");

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
		public uint[] Mantissa { get; init; }
		public int Exponent { get; init; }
		public int Precision { get; init; } // Number of significant binary digits.

		public byte BitsBeforeBP { get; init; }

		public bool IsZero => !Mantissa.Any(x => x > 0);
		public int LimbCount => Mantissa.Length;

		#endregion

		#region Public Methods

		public bool GetSign() => FP31ValHelper.GetSign(Mantissa);

		public RValue GetRValue()
		{
			var result = FP31ValHelper.CreateRValue(this);
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
				? FP31ValHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}"
				: "-" + FP31ValHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}";

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
			return obj is FP31Val smx && Equals(smx);
		}

		public bool Equals(FP31Val other)
		{
			return Sign == other.Sign &&
				((IStructuralEquatable)Mantissa).Equals(other.Mantissa, StructuralComparisons.StructuralEqualityComparer) &&
				Exponent == other.Exponent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Sign, Mantissa, Exponent);
		}

		public static bool operator ==(FP31Val left, FP31Val right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FP31Val left, FP31Val right)
		{
			return !(left == right);
		}

		#endregion
	}
}
