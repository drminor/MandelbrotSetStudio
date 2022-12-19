using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class ScalarMath2CTest
	{
		#region Square and Multiply

		[Fact]
		public void SquareFourAndAQuarterNewTec()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aTv = new Smx2CTestValue("-36507222016", -33, precision, scalarMath2C); // -4.25

			var aTv = new Smx2CTestValue("2147483648", -33, precision, scalarMath2C); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var b = scalarMath2C.Square(aTv.Smx2CValue);
			var bTv = new Smx2CTestValue(b, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bTv.RValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareFourAndAQuarterXX()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			//var aBigInteger = BigInteger.Parse("-36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aBigInteger = BigInteger.Parse("2147483648");
			var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = scalarMath2C.Convert(aSmx);

			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var bSmx2C = scalarMath2C.Square(aSmx2C);
			var bSmx = scalarMath2C.Convert(bSmx2C);

			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValueSm()
		{
			var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;


			//var aBigInteger = BigInteger.Parse("-12644545325526901863503869090"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var aBigInteger = BigInteger.Parse("-1264454532552690186350"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var aRValue = new RValue(aBigInteger, -124, precision); // 0.25
			var aSmx2C = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));

			var aStr = RValueHelper.ConvertToString(aRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");


			var bSmx2C = scalarMath2C.Square(aSmx2C);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bSmx = scalarMath2C.Convert(bSmx2C);

			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var bMantissaDisp = ScalerMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
			var aBiRValue = new RValue(aBiSqr, -248, precision);

			//var smxMH2 = BuildTheMathHelper(10);
			//var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, -248, precision));

			var aBiSmx = ScalerMathHelper.CreateSmx(aBiRValue, targetExponent: -280, limbCount: 9, bitsBeforeBP: 8);
			var aBiSmx2C = scalarMath2C.Convert(aBiSmx, overrideFormatChecks:true);

			var aBiSmxRValue = aBiSmx.GetRValue();
			var aBiStr = aBiSmx.GetStringValue();
			Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aBiSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValue()
		{
			var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var aRValue = new RValue(aBigInteger, -134, precision); // 0.25
			var aSmx = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var aMantissaDisp = ScalerMathHelper.GetDiagDisplay("raw operand", aSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the a mantissa is {aMantissaDisp}.");

			var a2Mantissa = scalarMath2C.Square(aSmx.Mantissa);
			var a2Str = ScalerMathHelper.GetDiagDisplay("raw products", a2Mantissa);
			Debug.WriteLine($"The StringValue for the a2Mantissa is {a2Str}.");

			var a3Mantissa = scalarMath2C.PropagateCarries(a2Mantissa, out _);
			var a3MantissaNrm = scalarMath2C.ShiftAndTrim(a3Mantissa);
			var a3 = new Smx(true, a3MantissaNrm, aSmx.Exponent, aSmx.BitsBeforeBP, aSmx.Precision);
			var a3Str = a3.GetStringValue();
			Debug.WriteLine($"The StringValue for the a3Mantissa is {a3Str}.");

			var bSmx = scalarMath2C.Square(aSmx);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var bMantissaDisp = ScalerMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			//var bP32Smx = AdjustExponent(bSmx, bSmx.Exponent + 32);
			//var bP32Str = bP32Smx.GetStringValue();
			//Debug.WriteLine($"The StringValue for the bSmxPlus32 is {bP32Str}.");

			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
			var aBiSqrRValue = new RValue(aBiSqr, -268, precision);

			//var smxMH2 = BuildTheMathHelper(10);
			//var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, -268, precision));

			var aBiSmx = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aBiSqrRValue, targetExponent: -312, limbCount: 10, bitsBeforeBP: 8), overrideFormatChecks: true);
			//var aBiSmxRValue = aBiSmx.GetRValue();
			var aBiStr = aBiSmx.GetStringValue();
			Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(aBiSqrRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		//public void Square_ValueWith_7_Limbs()
		//{
		//	//var scalarMath2C = new scalarMath2C(-170);
		//	var precision = 37;    // Binary Digits of precision, 30 Decimal Digits
		//	var limbCount = 7;      // TargetExponent = -184, Total Bits = 192
		//	var scalarMath2C = BuildTheMathHelper(limbCount);
		//	var targetExponent = scalarMath2C.TargetExponent;
		//	var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

		//	var avSmx = new Smx(false, new ulong[] { 0, 0, 4155170372, 1433657343, 4294967295, 566493183, 1 }, -216, precision, scalarMath2C.BitsBeforeBP); // TODO: a

		//	var avRValue = avSmx.GetRValue();
		//	var avStr = avSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the avSmx is {avStr}.");
		//	var atMantissaDisp = SmxHelper.GetDiagDisplay("av raw operand", avSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the av mantissa is {atMantissaDisp}.");

		//	var aSmx = scalarMath2C.Convert(SmxHelper.CreateSmx(avRValue, targetExponent, limbCount, bitsBeforeBP));
		//	var aRValue = aSmx.GetRValue();
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the atSmx is {aStr}.");
		//	var aMantissaDisp = SmxHelper.GetDiagDisplay("raw operand", avSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the mantissa is {aMantissaDisp}.");

		//	var a2Mantissa = scalarMath2C.Square(aSmx.Mantissa);
		//	var a2Str = SmxHelper.GetDiagDisplay("raw products", a2Mantissa);
		//	Debug.WriteLine($"The StringValue for the a2Mantissa is {a2Str}.");

		//	var a3Mantissa = scalarMath2C.PropagateCarries(a2Mantissa);
		//	var a3MantissaNrm = scalarMath2C.ShiftAndTrim(a3Mantissa);
		//	var a3 = new Smx(true, a3MantissaNrm, aSmx.Exponent, aSmx.Precision, aSmx.BitsBeforeBP);
		//	var a3Str = a3.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the a3Mantissa is {a3Str}.");

		//	var bSmx2C = scalarMath2C.Square(aSmx);
		//	var bSmx = scalarMath2C.Convert(bSmx2C);

		//	var bSmxRValue = bSmx.GetRValue();
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var bMantissaDisp = SmxHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

		//	//var bP32Smx = AdjustExponent(bSmx, bSmx.Exponent - 32);
		//	//var bP32Str = bP32Smx.GetStringValue();
		//	//Debug.WriteLine($"The StringValue for the bSmxPlus32 is {bP32Str}.");

		//	var bRValue = aRValue.Square();
		//	var bStrComp = RValueHelper.ConvertToString(bRValue);
		//	Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

		//	//var smxMH2 = BuildTheMathHelper(14);
		//	//var aBigInteger = SmxHelper.FromPwULongs(aSmx.Mantissa);
		//	//var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	//var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, aSmx.Exponent * 2, precision));
		//	////var aBiSmxRValue = aBiSmx.GetRValue();
		//	//var aBiStr = aBiSmx.GetStringValue();
		//	//Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

		//	//var aBigInteger = SmxHelper.FromPwULongs(aSmx.Mantissa);
		//	//var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	//var aBiSqrRValue = new RValue(aBiSqr, -384, precision);

		//	////var smxMH2 = BuildTheMathHelper(10);
		//	////var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, -268, precision));

		//	//var aBiSmx = scalarMath2C.Convert(SmxHelper.CreateSmx(aBiSqrRValue, targetExponent: -384, limbCount: 10, bitsBeforeBP: 8), overrideFormatChecks:true);

		//	////var aBiSmx = scalarMath2C.Convert(SmxHelper.CreateSmx(aBiSqrRValue, targetExponent: -312, limbCount: 10, bitsBeforeBP: 8), overrideFormatChecks: true);

		//	////var aBiSmxRValue = aBiSmx.GetRValue();
		//	//var aBiStr = aBiSmx.GetStringValue();
		//	//Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	Assert.True(haveRequiredPrecision);
		//	Assert.Equal(strA, strB);
		//}

		[Fact]
		public void SquareCompareWithAndWithoutLeadingZeros()
		{
			var precision = 35;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 4;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, precision);

			var aSmx = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var a2Mantissa = scalarMath2C.Square(aSmx.Mantissa);
			var a3Mantissa = scalarMath2C.PropagateCarries(a2Mantissa, out var carry);
			var a3 = new Smx(true, a3Mantissa, aSmx.Exponent * 2, aSmx.BitsBeforeBP, aSmx.Precision);
			var a3SmxRValue = a3.GetRValue();
			var a3Str = a3.GetStringValue();
			Debug.WriteLine($"The StringValue for the a3Smx is {a3Str}.");

			// Add a leading zero
			var bMantissaLst = aSmx.Mantissa.ToList();
			bMantissaLst.Add(0);
			var bMantissa = bMantissaLst.ToArray();
			var bExponent = aSmx.Exponent;
			var bPrecision = aSmx.Precision;

			var b2Mantissa = scalarMath2C.Square(bMantissa);
			var b3Mantissa = scalarMath2C.PropagateCarries(b2Mantissa, out var carry2);
			var b3 = new Smx(true, b3Mantissa, bExponent * 2, aSmx.BitsBeforeBP, bPrecision);
			var b3SmxRValue = b3.GetRValue();
			var b3Str = a3.GetStringValue();
			Debug.WriteLine($"The StringValue for the b3Smx is {b3Str}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(b3SmxRValue, a3SmxRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void FullMultiply_Returns_Correct_Value()
		{
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = ScalerMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = scalarMath2C.Multiply(aMantissa, aMantissa);
			var bBigInteger = ScalerMathHelper.FromPwULongs(bMantissa);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			Assert.Equal(bBigInteger, bCompBigInteger);
		}

		//[Fact]
		//public void MultiplyTwoRValues()
		//{
		//	var precision = 53;
		//	var limbCount = 4;
		//	var scalarMath2C = BuildTheMathHelper(limbCount);
		//	var targetExponent = scalarMath2C.TargetExponent;
		//	var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

		//	//var aRvalue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
		//	//var bRvalue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4

		//	var aBigInteger = BigInteger.Parse("-27797772040142849");
		//	var aRValue = new RValue(aBigInteger, -62, precision); // 0.25

		//	var aSmx = SmxHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
		//	var aSmx2C = scalarMath2C.Convert(aSmx);
		//	var aStr = aSmx2C.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx2C is {aStr}. Compare {aSmx.GetStringValue()}");


		//	var bBigInteger = BigInteger.Parse("4548762148012033");
		//	var bRValue = new RValue(bBigInteger, -62, precision);

		//	var bSmx = SmxHelper.CreateSmx(bRValue, targetExponent, limbCount, bitsBeforeBP);
		//	var bSmx2C = scalarMath2C.Convert(bSmx);
		//	var bStr = bSmx2C.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.Compare {bSmx.GetStringValue()}\");");

		//	var cSmx2C = scalarMath2C.Multiply(aSmx2C, bSmx2C);
		//	var cSmx = scalarMath2C.Convert(cSmx2C);

		//	var cSmxRValue = cSmx.GetRValue();
		//	var cStr = cSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

		//	var cRValue = aRValue.Mul(bRValue);
		//	var cStrComp = RValueHelper.ConvertToString(cRValue);
		//	Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

		//	var numberOfMatchingDigitsN = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expectedN);
		//	Assert.Equal(expectedN, Math.Min(numberOfMatchingDigitsN, expectedN));
		//}

		//[Fact]
		//public void MultiplyAnRValueWithInt()
		//{
		//	var precision = 53;
		//	var limbCount = 3;
		//	var scalarMath2C = BuildTheMathHelper(limbCount);
		//	var targetExponent = scalarMath2C.TargetExponent;
		//	var bitsBeforeBP = scalarMath2C.BitsBeforeBP;

		//	//var aRvalue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
		//	//var bRvalue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4

		//	//var aBigInteger = BigInteger.Parse("-27797772040142849");
		//	//var aRValue = new RValue(aBigInteger, -62, precision);

		//	var aBigInteger = BigInteger.Parse("-36507222016");
		//	var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

		//	var aSmx = scalarMath2C.Convert(SmxHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

		//	var b = 3;
		//	Debug.WriteLine($"The StringValue for the bSmx is {b}.");

		//	var cSmx2C = scalarMath2C.Multiply(aSmx, b);
		//	var cSmx = scalarMath2C.Convert(cSmx2C);

		//	var cSmxRValue = cSmx.GetRValue();
		//	var cStr = cSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

		//	var cRValue = aRValue.Mul(b);
		//	var cStrComp = RValueHelper.ConvertToString(cRValue);
		//	Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cSmxRValue, cRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	Assert.True(haveRequiredPrecision);
		//	Assert.Equal(strA, strB);
		//}

		#endregion

		#region Add / Subtract

		[Fact]
		public void AddTwoRValues()
		{
			var precision = 53;
			var limbCount = 3;
			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;


			//var aRvalue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(27797772040142849), -62, precision); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, precision); // 9.8635556059889517056815666506964e-4

			var a = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));
			var b = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(bRValue, targetExponent, limbCount, bitsBeforeBP));
 
			var cSmx2C = scalarMath2C.Add(a, b, "Test");
			var cSmx = scalarMath2C.Convert(cSmx2C);

			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cStrComp is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cSmxRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddTwoRValuesSm()
		{
			var precision = 53;
			var limbCount = 3;
			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;


			//var aBigInteger = BigInteger.Parse("-36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			//var aBigInteger = BigInteger.Parse("2147483648");
			//var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			//var aRValue = new RValue(new BigInteger(27797772040142849), -62, precision); // -6.02768096723593793141715568851e-3
			//var bRValue = new RValue(new BigInteger(4548762148012033), -62, precision); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4


			var a = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP));
			var b = scalarMath2C.Convert(ScalerMathHelper.CreateSmx(bRValue, targetExponent, limbCount, bitsBeforeBP));

			var cSmx2C = scalarMath2C.Add(a, b, "Test");
			var cSmx = scalarMath2C.Convert(cSmx2C);

			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cStrComp is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cSmxRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddTwoRValuesUseSub()
		{
			var precision = 25;
			var limbCount = 5;
			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			var targetExponent = scalarMath2C.TargetExponent;
			var bitsBeforeBP = scalarMath2C.BitsBeforeBP;


			//var a = new Smx(false, new ulong[] { 151263699, 55238551, 1 }, 2, -63, precision);
			//var b = new Smx(true, new ulong[] { 86140672, 2, 0 }, 1, -36, precision);

			var aLongs = new ulong[] {1512, 552, 1 };
			var aBigInteger = -1 * ScalerMathHelper.FromPwULongs(aLongs);
			var aRValueStg = new RValue(aBigInteger, -63, precision);

			var bLongs = new ulong[] { 8614, 2, 0 };
			var bBigInteger = ScalerMathHelper.FromPwULongs(bLongs);
			var bRValueStg = new RValue(bBigInteger, -36, precision);

			var aRValue = RNormalizer.Normalize(aRValueStg, bRValueStg, out var bRValue);

			var aSmx = ScalerMathHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = scalarMath2C.Convert(aSmx);
			var aStr = aSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var bSmx = ScalerMathHelper.CreateSmx(bRValue, targetExponent, limbCount, bitsBeforeBP);
			var bSmx2C = scalarMath2C.Convert(bSmx);
			var bStr = bSmx2C.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");


			var cSmx2C = scalarMath2C.Add(aSmx2C, bSmx2C, "Test");
			var cSmx = scalarMath2C.Convert(cSmx2C);

			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cSmxRValue, failOnTooFewDigits: true, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		#endregion

		#region Support Methods

		private ScalarMath2C BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

		private Smx AdjustExponent(Smx o, int newExponent)
		{
			var result = new Smx(o.Sign, o.Mantissa, newExponent, o.BitsBeforeBP, o.Precision);
			return result;
		}

		#endregion
	}

}