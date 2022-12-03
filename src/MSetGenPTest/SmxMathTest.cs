using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class SmxMathTest
	{
		#region Tests

		[Fact]
		public void FullMultiply_Returns_Correct_Value()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 2 * 32 - 8));

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = smxMathHelper.Multiply(aMantissa, aMantissa);
			var bBigInteger = SmxMathHelper.FromPwULongs(bMantissa);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			Assert.Equal(bBigInteger, bCompBigInteger);
		}

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;		// Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var smxMathHelper = BuildTheMathHelper(limbCount);

			//var aBigInteger = BigInteger.Parse("-36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aBigInteger = BigInteger.Parse("2147483648");
			var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var bSmx = smxMathHelper.Square(aSmx);
			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			//var bRValue = SquareAdjusted(aRValue, smxMathHelper);
			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			//var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(bSmxRValue, bRValue, out var expected);
			//Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));

			var haveRequiredPrecion = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecion);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void MultiplyTwoRValues()
		{
			//var aRvalue = new RValue(new BigInteger(-414219082), -36, 53); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, 53); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(-27797772040142849), -62, 53); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, 53); // 9.8635556059889517056815666506964e-4

			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 4 * 32 - 8));


			//var a = new Smx(aRValue);
			//var b = new Smx(bRValue);

			var a = smxMathHelper.CreateSmx(aRValue);
			var b = smxMathHelper.CreateSmx(bRValue);

			var c = smxMathHelper.MultiplyC(a, b);
			var cSmxRValue = c.GetRValue();
			var s1 = c.GetStringValue();

			var cN = smxMathHelper.Multiply(a, b);
			var cNSmxRValue = cN.GetRValue();
			var sN1 = cN.GetStringValue();


			var cRValue = aRValue.Mul(bRValue);
			//var cRValue = MulAdjusted(aRValue, bRValue, smxMathHelper);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expected);

			var numberOfMatchingDigitsN = RValueHelper.GetNumberOfMatchingDigits(cNSmxRValue, cRValue, out var expectedN);

			Assert.Equal(expected, Math.Min(numberOfMatchingDigitsN, expectedN));
		}

		[Fact]
		public void SquareAnRValueSm()
		{
			var precision = 110;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var smxMathHelper = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-12644545325526901863503869090"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var aRValue = new RValue(aBigInteger, -124, precision); // 0.25
			var aSmx = smxMathHelper.CreateSmx(aRValue);

			var aMantissaDisp = SmxMathHelper.GetDiagDisplay("raw operand", aSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the aSmx is {aMantissaDisp}.");


			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var a2Mantissa = smxMathHelper.Square(aSmx.Mantissa);
			var a2Str = SmxMathHelper.GetDiagDisplay("raw products", a2Mantissa);
			Debug.WriteLine($"The StringValue for the aSmx is {a2Str}.");

			var a3Mantissa = smxMathHelper.PropagateCarries(a2Mantissa, out var indexOfLastNonZeroLimb);
			var a3MantissaNrm = smxMathHelper.ForceExp(a3Mantissa, out var a3NrmExponent);

			var a3 = new Smx(true, a3MantissaNrm, a3NrmExponent, aSmx.Precision, aSmx.BitsBeforeBP);
			var a3Str = a3.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {a3Str}.");

			var bSmx = smxMathHelper.Square(aSmx);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(bSmxRValue, bRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void SquareAnRValue()
		{
			var precision = 53; // Binary Digits
			var aRValue = new RValue(BigInteger.Parse("-126445453255269018635038690902017"), -133, precision); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 6 * 32 - 8));

			var s0 = RValueHelper.ConvertToString(aRValue); // -5.94543663954929423147140869492508049e-10

			//var a = new Smx(aRValue);
			var a = smxMathHelper.CreateSmx(aRValue);

			//var a2Mantissa = smxMathHelper.Square(a.Mantissa);
			////var a2 = new Smx(true, a2Mantissa, -248, 53);
			////var a2SmxRvalue = a2.GetRValue();
			////var sA2 = a2.GetStringValue();

			//var a3Mantissa = smxMathHelper.PropagateCarries(a2Mantissa, out var indexOfLastNonZeroLimb);
			//var a3 = new Smx(true, a3Mantissa, -248, 53, a.BitsBeforeBP);
			//var a3SmxRvalue = a3.GetRValue();
			//var sA3 = a3.GetStringValue();

			var b = smxMathHelper.Square(a);                            //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895e-19 
			var bSmxRValue = b.GetRValue();
			var s1 = b.GetStringValue();

			var bRValue = aRValue.Square();
			//var bRValue = SquareAdjusted(aRValue, smxMathHelper);
			var s2 = RValueHelper.ConvertToString(bRValue);

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(bSmxRValue, bRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void Square_ValueWith_7_Limbs()
		{
			//var smxMathHelper = new SmxMathHelper(-170);
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 7 * 32 - 8));

			var precision = 37;
			var a = new Smx(false, new ulong[] { 0, 0, 4155170372, 1433657343, 4294967295, 566493183, 0 }, -216, precision, smxMathHelper.BitsBeforeBP); // TODO: a

			var aRValue = a.GetRValue();
			var s0 = RValueHelper.ConvertToString(aRValue);

			var b = smxMathHelper.Square(a);
			var bSmxRValue = b.GetRValue();
			var s1 = RValueHelper.ConvertToString(bSmxRValue);

			var bRValue = aRValue.Square();
			//var bRValue = SquareAdjusted(aRValue, smxMathHelper);
			var s2 = RValueHelper.ConvertToString(bRValue);

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(bSmxRValue, bRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void SquareCompareWithAndWithoutLeadingZeros()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 3 * 32 - 8));

			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, 35);

			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var s0 = aSmx.GetStringValue();

			var a2Mantissa = smxMathHelper.Square(aSmx.Mantissa);
			var a3Mantissa = smxMathHelper.PropagateCarries(a2Mantissa, out _);
			var a3 = new Smx(true, a3Mantissa, aSmx.Exponent * 2, aSmx.Precision, aSmx.BitsBeforeBP);
			var a3SmxRValue = a3.GetRValue();
			var sA3 = a3.GetStringValue();

			// Add a leading zero
			var bMantissaLst = aSmx.Mantissa.ToList();
			bMantissaLst.Add(0);
			var bMantissa = bMantissaLst.ToArray();
			var bExponent = aSmx.Exponent;
			var bPrecision = aSmx.Precision;

			var b2Mantissa = smxMathHelper.Square(bMantissa);
			var b3Mantissa = smxMathHelper.PropagateCarries(b2Mantissa, out _);
			var b3 = new Smx(true, b3Mantissa, bExponent * 2, bPrecision, aSmx.BitsBeforeBP);
			var b3SmxRValue = b3.GetRValue();
			var bA3 = b3.GetStringValue();

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(b3SmxRValue, a3SmxRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void NormalizeFPV_Returns_Correct_Value()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 2 * 32 - 8));

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 0.0000000000353482168348864539511122006373007661 (or possibly: 0.000000000035348216834895204420066149556547602)
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bRawMantissa = smxMathHelper.Multiply(aMantissa, aMantissa);
			var bMantissa = smxMathHelper.PropagateCarries(bRawMantissa, out var indexOfLastNonZeroLimb);
			var nrmBMantissa = smxMathHelper.NormalizeFPV(bMantissa, indexOfLastNonZeroLimb, -128, 55, out var nrmExponent);

			var t1BigInteger = SmxMathHelper.FromPwULongs(nrmBMantissa);
			var t2BigInteger = t1BigInteger / BigInteger.Pow(2, nrmExponent);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			var b2CompBigInteger = bCompBigInteger / BigInteger.Pow(2, 172);

			Assert.Equal(t2BigInteger, b2CompBigInteger);
		}

		[Fact]
		public void TestForceExp()
		{
			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 7 * 32 - 8));

			var a = new Smx(true, new ulong[] { 0, 353384068, 3154753262, 3994887299, 3390983361, 1473799878, 109397432 }, -216, 55, smxMathHelper.BitsBeforeBP);

			var aRValue = a.GetRValue();
			var s0 = a.GetStringValue(); // -5.94543663954929423147140869492508049e-10

			var nrmMantissa = smxMathHelper.ForceExp(a.Mantissa/*, 6, -248*/, out var nrmExponent);
			Debug.Assert(nrmMantissa.Length == smxMathHelper.LimbCount, $"ForceExp returned a result with {nrmMantissa.Length} limbs, expecting {smxMathHelper.LimbCount}.");

			var aNrm = new Smx(true, nrmMantissa, nrmExponent, 55, a.BitsBeforeBP);
			var aNrmRValue = aNrm.GetRValue();
			var s1 = aNrm.GetStringValue();

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(aNrmRValue, aRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void AddTwoRValues()
		{
			//var aRvalue = new RValue(new BigInteger(-414219082), -36, 53); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, 53); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(27797772040142849), -62, 53); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, 53); // 9.8635556059889517056815666506964e-4

			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 3 * 32 - 8));

			//var a = new Smx(aRValue);
			var a = smxMathHelper.CreateSmx(aRValue);

			//var b = new Smx(bRValue);
			var b = smxMathHelper.CreateSmx(bRValue);

			var aSa = smxMathHelper.Convert(a);
			var bSa = smxMathHelper.Convert(b);
 
			var cSa = smxMathHelper.Add(aSa, bSa, "Test");

			var c = smxMathHelper.Convert(cSa);
			var cSmxRValue = c.GetRValue();
			var s1 = RValueHelper.ConvertToString(cSmxRValue);

			var cRValue = aRValue.Add(bRValue);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void AddTwoRValuesUseSub()
		{
			//var aBi = BigIntegerHelper.FromLongs(new long[] { -1, 55238551, 151263699 });
			//var bBi = BigIntegerHelper.FromLongs(new long[] { 2, 86140672 });
			//var aRValue = new RValue(aBi, -63, 55);
			//var bRValue = new RValue(bBi, -36, 55);
			//var a = new Smx(aRValue);
			//var b = new Smx(bRValue);

			var aSa = new SmxSa(false, new ulong[] { 151263699, 55238551, 1 }, 2, -63, 55);
			var bSa = new SmxSa(true, new ulong[] { 86140672, 2, 0 }, 1, -36, 55);

			var smxMathHelper = new SmxMathHelper(new ApFixedPointFormat(8, 3 * 32 - 8));

			var a = smxMathHelper.Convert(aSa);
			var b = smxMathHelper.Convert(bSa);

			var aRValue = a.GetRValue();
			var bRValue = b.GetRValue();
			var cSa = smxMathHelper.Add(aSa, bSa, "Test");

			var c = smxMathHelper.Convert(cSa);

			var cSmxRValue = c.GetRValue();
			var s1 = RValueHelper.ConvertToString(cSmxRValue);

			var nrmA = RNormalizer.Normalize(aRValue, bRValue, out var nrmB);
			var cRValue = nrmA.Add(nrmB);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		//[Fact]
		//public void TrimLeadingZeros_FromZeroValuedSmx_Returns_Zero()
		//{
		//	var mantissa = new ulong[] { 0 };

		//	var trimmedMantissa = new SmxMathHelper(RMapConstants.DEFAULT_PRECISION).TrimLeadingZeros(mantissa);

		//	Assert.Equal(1, trimmedMantissa.Length);
		//}

		//[Fact]
		//public void TrimLeadingZeros_FromSmxWithOneNonZeroDigit_Returns_Same()
		//{
		//	var mantissa = new ulong[] { 1 };

		//	var trimmedMantissa = new SmxMathHelper(RMapConstants.DEFAULT_PRECISION).TrimLeadingZeros(mantissa);

		//	Assert.True(trimmedMantissa.Length == 1 && trimmedMantissa[0] == 1);
		//}

		#endregion

		#region Support Methods

		private SmxMathHelper BuildTheMathHelper(int limbCount)
		{
			var result = new SmxMathHelper(new ApFixedPointFormat(8, limbCount * 32 - 8));
			return result;
		}

		private RValue MulAdjusted(RValue left, RValue right, SmxMathHelper smxMathHelper)
		{
			var a = new RValue(left.Value, left.Exponent - smxMathHelper.BitsBeforeBP);
			var b = new RValue(right.Value, right.Exponent - smxMathHelper.BitsBeforeBP);

			var c = a.Mul(b);

			var result = new RValue(c.Value, c.Exponent + smxMathHelper.BitsBeforeBP);

			return result;
		}

		private RValue SquareAdjusted(RValue left, SmxMathHelper smxMathHelper)
		{
			var a = new RValue(left.Value, left.Exponent - smxMathHelper.BitsBeforeBP);

			var b = a.Square();

			var result = new RValue(b.Value, b.Exponent + smxMathHelper.BitsBeforeBP);

			return result;
		}


		#endregion
	}

	/*
		x = 4381107968, 1 * 2^-36
		y = -3 * 2^-36
	*/

	/*
		6258ffcd712f62b28ce55cf3

		SamplePointDelta: 1, -36

		XPos:
		Hi: 0
		Lo: -414219082 

		YPos
		Hi: 0
		Lo: 67781838

		MAX_ULONG = 18,446,744,073,709,551,615 = 2^64 - 1
		with 20 decimal digits of precision
		20 * 3.533 = 70.6 binary digits of precision
	*/

}