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
			var precision = 20;
			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, precision);

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
			var scalarMath = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-34359738368");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aSmx = scalarMath.CreateSmx(aRValue);

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
			var scalarMath = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("36507222016");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aSmx = scalarMath.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Negative_SmxWithIntegerPortion()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-36507222016");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aSmx = scalarMath.CreateSmx(aRValue);

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
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			var aRValue = new RValue(BigInteger.Parse("12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = scalarMath.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Negative_SmxWithIntegerPortionHiRez()
		{
			var precision = 99;	// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = scalarMath.CreateSmx(aRValue);

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx_WithMaxIntegerValue_Succeeds()
		{
			var precision = RMapConstants.DEFAULT_PRECISION;            // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;                                          // TargetExponent = -56, Total Bits = 64

			var scalarMath = BuildTheMathHelper(limbCount);

			var aSmx = scalarMath.CreateNewMaxIntegerSmx(precision);

			var aTv = new SmxTestValue(aSmx, scalarMath);
			Debug.WriteLine($"The StringValue for the MaxIntegerSmx is {aTv}.");

			var bitsBeforeBP = scalarMath.BitsBeforeBP;
			var maxSignedIntegerValue = ScalarMathHelper.GetMaxIntegerValue(bitsBeforeBP);

			var bRValue = new RValue(maxSignedIntegerValue, 0);
			var bStr = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the maxSignedIntegerValue is {bStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx_Negation_Succeeds()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");


			var aTv = new SmxTestValue(number, exponent, precision, scalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmxNeg = ScalarMathHelper.Negate(aTv.SmxValue);
			var bTv = new SmxTestValue(aSmxNeg, scalarMath);
			Debug.WriteLine($"The StringValue after negation from the Smx2CNeg var is {bTv}.");

			var aSmx2CNegRValue = bTv.RValue;
			var aRValueNeg = aRValue.Mul(-1);
			Debug.WriteLine($"The StringValue after negation from the aRValueNeg var is {RValueHelper.ConvertToString(aRValueNeg)}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValueNeg, aSmx2CNegRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx_Negation_IsReversable()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmxNeg = ScalarMathHelper.Negate(aTv.SmxValue);
			var aSmx2 = ScalarMathHelper.Negate(aSmxNeg);

			var bTv = new SmxTestValue(aSmx2, scalarMath);
			Debug.WriteLine($"The StringValue after negation (and back) from the aSmx2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, bTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx_Negation_IsReversableSm()
		{
			var precision = 53; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;  // TargetExponent = -56, Total Bits = 64
			var scalarMath = BuildTheMathHelper(limbCount);

			var number = "-343597";
			var exponent = -12;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmxNeg = ScalarMathHelper.Negate(aTv.SmxValue);
			var aSmx2 = ScalarMathHelper.Negate(aSmxNeg);

			var bTv = new SmxTestValue(aSmx2, scalarMath);
			Debug.WriteLine($"The StringValue after negation (and back) from the aSmx2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		private ScalarMath BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath(new ApFixedPointFormat(limbCount), 4u);
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
