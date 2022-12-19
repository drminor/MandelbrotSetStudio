using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class SmxTest
	{
		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful()
		{
			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, 20);

			var aSmx = new Smx(aRValue, bitsBeforeBP: 0);
			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aSmxRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var smxMathHelper = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-34359738368");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aSmx = smxMathHelper.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void CreateSmxWithIntegerPortion()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var smxMathHelper = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-36507222016");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aSmx = smxMathHelper.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void CreateSmxWithIntegerPortionHiRez()
		{
			var precision = 99;	// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var smxMathHelper = BuildTheMathHelper(limbCount);

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = smxMathHelper.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		/*

		TODO: Write unit a unit test to check this
					var testMaxVal = smxMathHelper.CreateSmx(new RValue(smxVecMathHelper.MaxIntegerValue, 0));
			var testMaxVal2 = smxVecMathHelper.CreateNewMaxIntegerSmx();

		*/
		private ScalerMath BuildTheMathHelper(int limbCount)
		{
			var result = new ScalerMath(new ApFixedPointFormat(limbCount), 4u);
			return result;
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
