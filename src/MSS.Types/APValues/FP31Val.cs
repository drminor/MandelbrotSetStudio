using MSS.Types;
using System;
using System.Collections;
using System.Linq;

namespace MSS.Types.APValues
{
	public struct FP31Val : IEquatable<FP31Val>, ICloneable
	{
		#region Constructor

		public FP31Val(uint[] mantissa, int exponent, byte bitsBeforeBP, int precision)
		{
			Mantissa = (uint[])mantissa.Clone(); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision = precision;
			BitsBeforeBP = bitsBeforeBP;
		}

		#endregion

		#region Public Properties

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

		//public string GetStringValue()
		//{
		//	var rValue = GetRValue();
		//	var strValue = RValueHelper.ConvertToString(rValue);

		//	return strValue;
		//}

		public override string ToString()
		{
			var sign = GetSign();

			var result = sign
				? FP31ValHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}"
				: "-" + FP31ValHelper.GetDiagDisplayHex("m", Mantissa) + $" e:{Exponent}";

			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public FP31Val Clone()
		{
			var result = new FP31Val((uint[]) Mantissa.Clone(), Exponent, BitsBeforeBP, Precision);

			return result;
		}

		#endregion

		#region IEquatable Support

		public override bool Equals(object? obj)
		{
			return obj is FP31Val fp31Val && Equals(fp31Val);
		}

		public bool Equals(FP31Val other)
		{
			return Exponent == other.Exponent
				&& BitsBeforeBP == other.BitsBeforeBP
				&& 	((IStructuralEquatable)Mantissa).Equals(other.Mantissa, StructuralComparisons.StructuralEqualityComparer);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Mantissa[^1], Exponent, BitsBeforeBP);
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
