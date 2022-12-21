using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	/*

				var aTv = new Smx2CTestValue("27797772040142849", -62, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Smx2CTestValue("4548762148012033", -62, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

	*/
	public class Smx2CTest
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

			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aBigInteger = BigInteger.Parse("-34359738368");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aTv = new Smx2CTestValue("-34359738368", -33, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
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
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aBigInteger = BigInteger.Parse("-34359738368");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4

			var aTv = new Smx2CTestValue("-34359738368", -33, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
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
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aBigInteger = BigInteger.Parse("36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aTv = new Smx2CTestValue("36507222016", -33, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
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
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aRValue = new RValue(BigInteger.Parse("12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aTv = new Smx2CTestValue("12644545325526901863503869090", -124, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
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
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aTv = new Smx2CTestValue("-12644545325526901863503869090", -124, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
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
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			var aSmx2C = scalarMath2C.CreateNewMaxIntegerSmx2C(precision);
			var aSmx = scalarMath2C.Convert(aSmx2C);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var aRValue = aSmx.GetRValue();

			var bRValue = new RValue(ScalarMathHelper.GetMaxIntegerValue2C(bitsBeforeBP), 0);
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
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var aRVstring = RValueHelper.ConvertToString(aRValue);

			Debug.WriteLine($"The StringValue for the inital RValue is {aRVstring}.");

			var aSmx = ScalarMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP, useTwoComplementEncoding: true);
			var aSmx2C = scalarMath2C.Convert(aSmx);

			var aSmx2CStrVal = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue before negation from the Smx2C var is {aSmx2CStrVal}.");

			var aSmx2CNeg = ScalarMathHelper.Negate(aSmx2C);

			var aSmx2CNegStrVal = aSmx2CNeg.GetStringValue();
			Debug.WriteLine($"The StringValue after negation from the Smx2CNeg var is {aSmx2CNegStrVal}.");

			var aSmx2CNegRValue = aSmx2CNeg.GetRValue();

			var aRValueNeg = aRValue.Mul(-1);
			var aRValueNegStrVal = RValueHelper.ConvertToString(aRValueNeg);

			Debug.WriteLine($"The StringValue after negation from the aRValueNeg var is {aRValueNegStrVal}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aRValueNeg, aSmx2CNegRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Smx2C_Negation_IsReversable()
		{
			var precision = 95; // Binary Digits of precision, 29 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aSmx = ScalarMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP, useTwoComplementEncoding: true);
			var aSmx2C = scalarMath2C.Convert(aSmx);

			var aSmx2CNeg = ScalarMathHelper.Negate(aSmx2C);
			var aSmx2C2 = ScalarMathHelper.Negate(aSmx2CNeg);

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
