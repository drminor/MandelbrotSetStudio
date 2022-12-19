using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class Smx2CValueTest
	{
		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, 31, precision);

			var aSmx2C = new Smx2C(aRValue, bitsBeforeBP: 0);
			var aSmx2CRValue = aSmx2C.GetRValue();
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aSmx2CRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative_2C_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, precision);

			var aSmx2C = new Smx2C(aRValue, bitsBeforeBP: 0);
			var aSmx2CRValue = aSmx2C.GetRValue();
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aSmx2CRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_2C_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aBigInteger = BigInteger.Parse("-34359738368");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aSmx = fpMathHelper.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative2C_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aBigInteger = BigInteger.Parse("-34359738368");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aSmx = fpMathHelper.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));

			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithIntegerPortion()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aBigInteger = BigInteger.Parse("36507222016");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aSmx = fpMathHelper.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));


			var aSmxRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithIntegerPortionHiRez()
		{
			var precision = 95;	// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = fpMathHelper.Convert(aSmx);

			var aSmx2CRValue = aSmx2C.GetRValue();
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {aStr}, compare: {aSmx.GetStringValue()}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmx2CRValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_NegativeSmx2C_WithIntegerPortionHiRez()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = fpMathHelper.Convert(aSmx);

			var aSmx2CRValue = aSmx2C.GetRValue();
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {aStr}, compare: {aSmx.GetStringValue()}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmx2CRValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithMaxIntegerValue_Succeeds()
		{
			var precision = RMapConstants.DEFAULT_PRECISION;			// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;											// TargetExponent = -56, Total Bits = 64
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aSmx2C = fpMathHelper.CreateNewMaxIntegerSmx2C(precision);
			var aSmx = fpMathHelper.Convert(aSmx2C);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var aRValue = aSmx.GetRValue();

			var bRValue = new RValue(ScalerMathHelper.GetMaxSignedIntegerValue(bitsBeforeBP), 0);
			var bStr = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {bStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_Succeeds()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = fpMathHelper.Convert(aSmx);

			var aSmx2CNeg = ScalerMathHelper.Negate(aSmx2C);

			var aSmx2CNegRValue = aSmx2CNeg.GetRValue();
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {aStr}.");

			var aRValueNeg = aRValue.Mul(-1);

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmx2CNegRValue, aRValueNeg, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_IsReversable()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var fpMathHelper = BuildTheMathHelper(limbCount);
			var targetExponent = fpMathHelper.TargetExponent;
			var bitsBeforeBP = fpMathHelper.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = fpMathHelper.Convert(aSmx);

			var aSmx2CNeg = ScalerMathHelper.Negate(aSmx2C);
			var aSmx2C2 = ScalerMathHelper.Negate(aSmx2CNeg);

			var aSmx2C2RValue = aSmx2C2.GetRValue();
			var aStr = aSmx2C2.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C2 is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmx2C2RValue, aRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		private ScalarMath2C BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

	}
}
