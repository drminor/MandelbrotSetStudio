﻿using MSS.Common;
using MSS.Types;
using System.Collections;

namespace MSetGenP
{
	public struct SmxSa : IEquatable<SmxSa>
	{
		#region Constructor

		public SmxSa(bool sign, ShiftedArray<ulong> mantissaSa, int exponent, int precision)
		{
			ValidatePWValues(mantissaSa);

			Sign = sign;
			MantissaSa = mantissaSa;
			Exponent = exponent;
			Precision = precision;
		}

		public SmxSa(bool sign, ulong[] mantissa, int indexOfLastNonZeroLimb, int exponent, int precision)
		{
			ValidatePWValues(mantissa);

			Sign = sign;
			MantissaSa = new ShiftedArray<ulong>(mantissa, 0, indexOfLastNonZeroLimb); // SmxMathHelper.GetPwULongs(mantissa);
			Exponent = exponent;
			Precision = precision;
		}

		private static void ValidatePWValues(ulong[] mantissa)
		{
			if (SmxMathHelper.CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Cannot create a SmxSa from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			}
		}

		private static void ValidatePWValues(ShiftedArray<ulong> mantissa)
		{
			if (SmxMathHelper.CheckPWValues(mantissa))
			{
				throw new ArgumentException($"Cannot create a SmxSa from an array of ulongs where any of the values is greater than MAX_DIGIT.");
			}
		}

		#endregion

		#region Public Properties

		public bool Sign { get; set; }
		public ShiftedArray<ulong> MantissaSa { get; init; }
		public int Exponent { get; set; }
		public int Precision { get; set; } // Number of significant binary digits.

		public bool IsZero => !MantissaSa.Array.Any(x => x > 0) && !MantissaSa.Carry.HasValue;

		public int LimbCount => MantissaSa.Length;
		public int IndexOfLastNonZeroLimb => MantissaSa.IndexOfLastNonZeroLimb;

		#endregion

		#region Public Methods

		public ulong[] Materialize()
		{
			var result = MantissaSa.Materialize();
			return result;
		}

		public RValue GetRValue()
		{
			var result = SmxMathHelper.GetRValue(this);
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
			return obj is SmxSa smx && Equals(smx);
		}

		public bool Equals(SmxSa other)
		{
			return Sign == other.Sign &&
				((IStructuralEquatable)MantissaSa).Equals(other.MantissaSa, StructuralComparisons.StructuralEqualityComparer) &&
				Exponent == other.Exponent;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Sign, MantissaSa, Exponent);
		}

		public static bool operator ==(SmxSa left, SmxSa right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SmxSa left, SmxSa right)
		{
			return !(left == right);
		}

		#endregion
	}
}
