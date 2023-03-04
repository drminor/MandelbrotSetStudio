using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;
using System.Numerics;

namespace VecMathTest
{
	public class FP31ValTest
	{
		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful_Small()
		{
			var precision = 20;
			var limbCount = 2;

			var number = "343597";
			var exponent = -13;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aFp31Val = FP31ValHelper.CreateFP31Val(aRValue, new ApFixedPointFormat(limbCount));
			var strA = RValueHelper.ConvertToString(aFp31Val.GetRValue());
			Debug.WriteLine($"The StringValue for the aFP31Val is {strA}.");

			var bRValue = FP31ValHelper.CreateRValue(aFp31Val);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful_Large()
		{
			var precision = 20;
			var limbCount = 5;

			var number = "126445453255269018635038690902017";
			var exponent = -134;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aFp31Val = FP31ValHelper.CreateFP31Val(aRValue, new ApFixedPointFormat(limbCount));
			var strA = RValueHelper.ConvertToString(aFp31Val.GetRValue());
			Debug.WriteLine($"The StringValue for the aFP31Val is {strA}.");

			var bRValue = FP31ValHelper.CreateRValue(aFp31Val);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful_LargeNeg()
		{
			var precision = 20;
			var limbCount = 5;
			var number = "-126445453255269018635038690902017";
			var exponent = -134;

			var aBigInteger = BigInteger.Parse(number);
			var aRValue = new RValue(aBigInteger, exponent, precision);

			var aFp31Val = FP31ValHelper.CreateFP31Val(aRValue, new ApFixedPointFormat(limbCount));
			var strA = RValueHelper.ConvertToString(aFp31Val.GetRValue());
			Debug.WriteLine($"The StringValue for the aFP31Val is {strA}.");

			var bRValue = FP31ValHelper.CreateRValue(aFp31Val);
			var strB = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {strB}.");

			Assert.Equal(strA, strB);
		}



		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful_Small_NewTec()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "343597";
			var exponent = -13;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aFP31V is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -127;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aFP31V is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative_ToRValue_IsSuccessful()
		{
			var precision = 20;
			var limbCount = 6;      // TargetExponent = -180, Total Bits = 186

			var fp31ScalarMath = BuildTheMathHelper(limbCount);
			var number = "-12644545325526901863503869090";
			var exponent = -127;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision);

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for the aFP31V is {aTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValue, aTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFP31VRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aFP31VRValue);
			Debug.WriteLine($"The StringValue for the aFP31V is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aFP31VRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void RoundTrip_Negative_ConvertExp_And_Convert_ToRValue_IsSuccessful()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFP31VRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aFP31VRValue);
			Debug.WriteLine($"The StringValue for the aFP31V is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aFP31VRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_FP31Val_WithIntegerPortion()
		{
			var precision = 20;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;     // TargetExponent = -56, Total Bits = 64
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "34359738368";
			var exponent = -33;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFP31VRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aFP31VRValue);
			Debug.WriteLine($"The StringValue for the aFP31V is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aFP31VRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_FP31Val_WithIntegerPortionHiRez()
		{
			var precision = 95;	// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -124;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFP31VRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aFP31VRValue);
			Debug.WriteLine($"The StringValue for the aFP31V is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aFP31VRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_Negative_FP31Val_WithIntegerPortionHiRez()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "12644545325526901863503869090";
			var exponent = -124;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFP31VRValue = aTv.RValue;
			var aStr = RValueHelper.ConvertToString(aFP31VRValue);
			Debug.WriteLine($"The StringValue for the aFP31V is {aStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aFP31VRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Create_FP31Val_WithMaxIntegerValue_Succeeds()
		{
			var precision = RMapConstants.DEFAULT_PRECISION;			// Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;											// TargetExponent = -56, Total Bits = 64
			
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var aFP31Val = fp31ScalarMath.CreateNewMaxIntegerFP31Val(precision);
			var aTv = new FP31ValTestValue(aFP31Val);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var aFP31V = fp31ScalarMath.Convert(aFP31Val);
			//var aStr = aFP31V.GetStringValue();
			//Debug.WriteLine($"The StringValue for the MaxIntegerSmx2C is {aStr}.");

			//var aRValue = aFP31V.GetRValue();

			//var maxSignedIntegerValue = fp31ScalarMath.MaxIntegerValue;
			var bitsBeforeBP = fp31ScalarMath.BitsBeforeBP;
			//var maxSignedIntegerValue = ScalarMathHelper.GetMaxIntegerValue(bitsBeforeBP, isSigned: true);
			var maxSignedIntegerValue = FP31ValHelper.GetMaxIntegerValue(bitsBeforeBP);


			var bRValue = new RValue(maxSignedIntegerValue, 0);
			var bStr = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the maxSignedIntegerValue is {bStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, aTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void FP31Val_Negation_Succeeds()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;  // TargetExponent = -184, Total Bits = 192
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");


			var aTv = new FP31ValTestValue(number, -124, precision, fp31ScalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the FP31Val var is {aTv}.");

			var aFP31V2Neg = FP31ValHelper.Negate(aTv.FP31Val);
			var bTv = new FP31ValTestValue(aFP31V2Neg);
			Debug.WriteLine($"The StringValue after negation from the FP31Val2Neg var is {bTv}.");

			var aFP31V2CNegRValue = bTv.RValue;
			var aRValueNeg = aRValue.Mul(-1);
			Debug.WriteLine($"The StringValue after negation from the aRValueNeg var is {RValueHelper.ConvertToString(aRValueNeg)}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValueNeg, aFP31V2CNegRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void FP31Val_Negation_IsReversable()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;	// TargetExponent = -184, Total Bits = 192
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "-12644545325526901863503869090";
			var exponent = -124;

			//var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			//Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the FP31Val var is {aTv}.");

			var aFP31V2CNeg = FP31ValHelper.Negate(aTv.FP31Val);
			var aFP31V2C2 = FP31ValHelper.Negate(aFP31V2CNeg);

			var bTv = new FP31ValTestValue(aFP31V2C2);
			Debug.WriteLine($"The StringValue after negation (and back) from the aFP31V2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aTv.RValue, bTv.RValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void FP31Val_Negation_IsReversableSm()
		{
			var precision = 53; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 2;  // TargetExponent = -56, Total Bits = 64
			var fp31ScalarMath = BuildTheMathHelper(limbCount);

			var number = "-343597";
			var exponent = -13;

			var aRValue = new RValue(BigInteger.Parse(number), exponent, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			//Debug.WriteLine($"The StringValue for the inital RValue is {RValueHelper.ConvertToString(aRValue)}.");

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			Debug.WriteLine($"The StringValue before negation from the FP31Val var is {aTv}.");

			var aFP31V2CNeg = FP31ValHelper.Negate(aTv.FP31Val);
			var aFP31V2C2 = FP31ValHelper.Negate(aFP31V2CNeg);

			var bTv = new FP31ValTestValue(aFP31V2C2);
			Debug.WriteLine($"The StringValue after negation (and back) from the aFP31V2C2 var is {bTv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aTv.RValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		private FP31ScalarMath BuildTheMathHelper(int limbCount)
		{
			var result = new FP31ScalarMath(new ApFixedPointFormat(limbCount));
			return result;
		}

	}
}
