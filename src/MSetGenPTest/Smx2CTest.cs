﻿using MSetGenP;
using MSetGenP.Types;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenPTest
{
	public class Smx2CTest
	{
		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful_Small()
		{
			var precision = 20;
			var limbCount = 2;

			var number = "343597";
			var exponent = -13;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aSmx2C = ScalarMathHelper.CreateSmx2C(aRValue, new ApFixedPointFormat(limbCount));
			var strA = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {strA}.");

			var bRValue = ScalarMathHelper.CreateRValue(aSmx2C);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful_Large()
		{
			var precision = 20;
			var limbCount = 5;

			var number = "126445453255269018635038690902017";
			var exponent = -134;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aSmx2C = ScalarMathHelper.CreateSmx2C(aRValue, new ApFixedPointFormat(limbCount));
			var strA = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {strA}.");

			var bRValue = ScalarMathHelper.CreateRValue(aSmx2C);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful_LargeNeg()
		{
			var precision = 20;
			var limbCount = 5;
			var number = "-126445453255269018635038690902017";
			var exponent = -134;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aSmx2C = ScalarMathHelper.CreateSmx2C(aRValue, new ApFixedPointFormat(limbCount));
			var strA = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx2C is {strA}.");

			var bRValue = ScalarMathHelper.CreateRValue(aSmx2C);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}



		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful_Small_NewTec()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "343597";
			var exponent = -13;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_2C_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -127;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative_2C_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var scalarMath2C = BuildTheMathHelper(limbCount);
			var number = "-12644545325526901863503869090";
			var exponent = -127;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_2C_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aSmxRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aSmxRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative2C_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aSmxRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aSmxRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithIntegerPortion()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aSmxRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aSmxRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithIntegerPortionHiRez()
		{
			var precision = 95;	// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -124;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aSmxRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aSmxRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_NegativeSmx2C_WithIntegerPortionHiRez()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -124;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aSmxRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aSmxRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aSmxRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Smx2C_WithMaxIntegerValue_Succeeds()
		{
			var precision = RMapConstants.DEFAULT_PRECISION;			// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;											// TargetExponent = -56, Total Bits = 64
			
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var aSmx2C = scalarMath2C.CreateNewMaxIntegerSmx2C(precision);
			var aTv = new Smx2CTestValue(aSmx2C, scalarMath2C);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var aSmx = scalarMath2C.Convert(aSmx2C);
			//var aStr = aSmx.GetStringValue();
			//Debug.WriteLine($"The StringValue for the MaxIntegerSmx2C is {aStr}.");

			//var aRValue = aSmx.GetRValue();

			//var maxSignedIntegerValue = scalarMath2C.MaxIntegerValue;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;
			var maxSignedIntegerValue = ScalarMathHelper.GetMaxIntegerValue(bitsBeforeBP, isSigned: true);

			var bRValue = new RValue(maxSignedIntegerValue, 0);
			var bStr = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the maxSignedIntegerValue is {bStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_Succeeds()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");


			var aTv = new Smx2CTestValue(number, -124, precision, scalarMath2C); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmx2CNeg = ScalarMathHelper.Negate(aTv.Smx2CValue);
			var bTv = new Smx2CTestValue(aSmx2CNeg, scalarMath2C);
			Debug.WriteLine($"The StringValue after negation from the Smx2CNeg var is {bTv}.");

			var aSmx2CNegRValue = bTv.RValue;
			var aRValueNeg = aRValue.Mul(-1);
			Debug.WriteLine($"The StringValue after negation from the aRValueNeg var is {RValueHelper.ConvertToString(aRValueNeg)}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValueNeg, aSmx2CNegRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_IsReversable()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;	// TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			//var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			//Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmx2CNeg = ScalarMathHelper.Negate(aTv.Smx2CValue);
			var aSmx2C2 = ScalarMathHelper.Negate(aSmx2CNeg);

			var bTv = new Smx2CTestValue(aSmx2C2, scalarMath2C);
			Debug.WriteLine($"The StringValue after negation (and back) from the aSmx2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aTv.RValue, bTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_IsReversableSm()
		{
			var precision = 53; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;  // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var number = "-343597";
			var exponent = -13;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			//Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aTv}.");

			var aSmx2CNeg = ScalarMathHelper.Negate(aTv.Smx2CValue);
			var aSmx2C2 = ScalarMathHelper.Negate(aSmx2CNeg);

			var bTv = new Smx2CTestValue(aSmx2C2, scalarMath2C);
			Debug.WriteLine($"The StringValue after negation (and back) from the aSmx2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aTv.RValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		private ScalarMath2C BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

	}
}
