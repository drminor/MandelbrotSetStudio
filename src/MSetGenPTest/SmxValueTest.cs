using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class SmxValueTest
	{
		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 2 * 32 - 8));

			//var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			//var aBigInteger = BigInteger.Parse("-34359738368");
			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, 20);

			//var aSmx = new Smx(aRValue, 20);
			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var aSmxRValue = aSmx.GetRValue();

			var haveRequiredPrecion = RValueHelper.GetStringsToCompare(aRValue, aSmxRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecion);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ForceExp_And_Convert_ToRValue_IsSuccessful()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 2 * 32 - 8));

			var aBigInteger = BigInteger.Parse("-34359738368");
			var aRValue = new RValue(aBigInteger, -33, 20);

			var aSmx = new Smx(aRValue, 20);
			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();

			var indexOfLastNonZeroLimb = smxMathHelper.GetIndexOfLastNonZeroLimb(aSmx.Mantissa);
			var nrmMantissa = smxMathHelper.ForceExp(aSmx.Mantissa, indexOfLastNonZeroLimb, aSmx.Exponent, out var nrmExponent);

			Debug.Assert(nrmMantissa.Length == smxMathHelper.LimbCount, $"ForceExp returned a result with {nrmMantissa.Length} limbs, expecting {smxMathHelper.LimbCount}.");

			var a2Smx = new Smx(aSmx.Sign, nrmMantissa, nrmExponent, aSmx.Precision, aSmx.BitsBeforeBP);

			var a2SmxRValue = a2Smx.GetRValue();
			var a2Str = a2Smx.GetStringValue();

			var haveRequiredPrecion = RValueHelper.GetStringsToCompare(aSmxRValue, a2SmxRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecion);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		//public void ValueGreaterThan255_CausesOverflow()
		//{
		//}


		//[Fact]
		//public void ValueSmallerThan_OneOver2RaisedToTheNegative35Power_CausesUnderflow()
		//{
			
		//}

	}
}
