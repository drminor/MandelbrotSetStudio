using MSetGenP;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class ScalarMathTest
	{
		private const uint THRESHOLD = 4;

		#region Normalize and Convert

		//[Fact]
		//public void NormalizeFPV_Returns_Correct_Value()
		//{
		//	var precision = 55;    // Binary Digits of precision, 30 Decimal Digits
		//	var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
		//	//var scalarMath = BuildTheMathHelper(limbCount);
		//	var smxMathHelperFloating = new ScalarMathFloating(new ApFixedPointFormat(limbCount));

		//	var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 0.0000000000353482168348864539511122006373007661 (or possibly: 0.000000000035348216834895204420066149556547602)
		//	var aMantissa = ScalarMathHelper.ToPwULongs(aBigInteger);

		//	var bRawMantissa = smxMathHelperFloating.Multiply(aMantissa, aMantissa);
		//	var bMantissa = smxMathHelperFloating.PropagateCarries(bRawMantissa);

		//	var indexOfLastNonZeroLimb = smxMathHelperFloating.GetIndexOfLastNonZeroLimb(bMantissa);
		//	var nrmBMantissa = smxMathHelperFloating.NormalizeFPV(bMantissa, indexOfLastNonZeroLimb, -128, precision, out var nrmExponent);

		//	var t1BigInteger = ScalarMathHelper.FromPwULongs(nrmBMantissa);
		//	var t2BigInteger = t1BigInteger / BigInteger.Pow(2, nrmExponent);

		//	var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	var b2CompBigInteger = bCompBigInteger / BigInteger.Pow(2, 172);

		//	Assert.Equal(t2BigInteger, b2CompBigInteger);
		//}

		//[Fact]
		//public void TestForceExp()
		//{
		//	var precision = 37;    // Binary Digits of precision, 30 Decimal Digits
		//	var limbCount = 7;      // TargetExponent = -184, Total Bits = 192
		//	var scalarMath = BuildTheMathHelper(limbCount);

		//	var avSmx = new Smx(true, new ulong[] { 0, 353384068, 3154753262, 3994887299, 3390983361, 1473799878, 109397432 }, -216, precision, scalarMath.BitsBeforeBP);

		//	var avRValue = avSmx.GetRValue();
		//	var avStr = avSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the avSmx is {avStr}."); // -5.94543663954929423147140869492508049e-10
		//	var atMantissaDisp = SmxHelper.GetDiagDisplay("av raw operand", avSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the av mantissa is {atMantissaDisp}.");

		//	var aSmx = scalarMath.CreateSmx(avRValue);
		//	var aRValue = aSmx.GetRValue();
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the atSmx is {aStr}.");
		//	var aMantissaDisp = SmxHelper.GetDiagDisplay("raw operand", avSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the mantissa is {aMantissaDisp}.");

		//	//var bStgSmx = AdjustExponent(aSmx, aSmx.Exponent - aSmx.BitsBeforeBP);

		//	var bStgRValue = aRValue.Mul(aSmx.BitsBeforeBP);
		//	var bStg1Smx = new Smx(bStgRValue);

		//	var bStgSmx = new Smx(aSmx.Sign, bStg1Smx.Mantissa, bStg1Smx.Exponent, bStg1Smx.Precision, aSmx.BitsBeforeBP);


		//	var bMantissa = scalarMath.ForceExp(bStgSmx.Mantissa, 0, out var bExponent);
		//	Debug.Assert(bMantissa.Length == scalarMath.LimbCount, $"ForceExp returned a result with {bMantissa.Length} limbs, expecting {scalarMath.LimbCount}.");

		//	var bSmx = new Smx(aSmx.Sign, bMantissa, bExponent, precision, aSmx.BitsBeforeBP);
		//	var bSmxRValue = bSmx.GetRValue();
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, aRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	Assert.True(haveRequiredPrecision);
		//	Assert.Equal(strA, strB);
		//}

		//[Fact]
		//public void TrimLeadingZeros_FromZeroValuedSmx_Returns_Zero()
		//{
		//	var mantissa = new ulong[] { 0 };

		//	var trimmedMantissa = new scalarMath(RMapConstants.DEFAULT_PRECISION).TrimLeadingZeros(mantissa);

		//	Assert.Equal(1, trimmedMantissa.Length);
		//}

		//[Fact]
		//public void TrimLeadingZeros_FromSmxWithOneNonZeroDigit_Returns_Same()
		//{
		//	var mantissa = new ulong[] { 1 };

		//	var trimmedMantissa = new scalarMath(RMapConstants.DEFAULT_PRECISION).TrimLeadingZeros(mantissa);

		//	Assert.True(trimmedMantissa.Length == 1 && trimmedMantissa[0] == 1);
		//}

		#endregion

		#region Square

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath = BuildTheMathHelper(limbCount);
			Debug.WriteLine($"{nameof(SquareFourAndAQuarter)} is using {scalarMath.ApFixedPointFormat}");

			//var number = "-36507222016";
			//var exponent = -33;

			var number = "2147483648";
			var exponent = -33;

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var b = scalarMath.Square(aTv.SmxValue);

			var bTv = new SmxTestValue(b, scalarMath);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			// RValue Square 
			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		//public void SquareAnRValueSm()
		//{
		//	var precision = 75;    // Binary Digits of precision, 30 Decimal Digits
		//	var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
		//	var scalarMath = BuildTheMathHelper(limbCount);
		//	Debug.WriteLine($"{nameof(SquareAnRValueSm)} is using {scalarMath.ApFixedPointFormat}");

		//	var aBigInteger = BigInteger.Parse("-12644545325526901863503869090"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
		//	var aRValue = new RValue(aBigInteger, -128, precision); // 0.25
		//	var aSmx = scalarMath.CreateSmx(aRValue);
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

		//	//var aMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw operand", aSmx.Mantissa);
		//	//Debug.WriteLine($"The StringValue for the a mantissa is {aMantissaDisp}.");

		//	//var a2Mantissa = scalarMath.Square(aSmx.Mantissa);
		//	//var a2Str = ScalarMathHelper.GetDiagDisplay("raw products", a2Mantissa);
		//	//Debug.WriteLine($"The StringValue for the a2Mantissa is {a2Str}.");

		//	//var a3Mantissa = scalarMath.PropagateCarries(a2Mantissa);
		//	//var a3MantissaNrm = scalarMath.ShiftAndTrim(a3Mantissa);
		//	//var a3 = new Smx(true, a3MantissaNrm, aSmx.Exponent, aSmx.BitsBeforeBP, aSmx.Precision);
		//	//var a3Str = a3.GetStringValue();
		//	//Debug.WriteLine($"The StringValue for the a3Mantissa is {a3Str}.");

		//	var bSmx = scalarMath.Square(aSmx);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
		//	var bSmxRValue = bSmx.GetRValue();
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var bMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

		//	//var bP32Smx = AdjustExponent(bSmx, bSmx.Exponent + 32);
		//	//var bP32Str = bP32Smx.GetStringValue();
		//	//Debug.WriteLine($"The StringValue for the bSmxPlus32 is {bP32Str}.");

		//	var bRValue = aRValue.Square();
		//	var bStrComp = RValueHelper.ConvertToString(bRValue);
		//	Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

		//	//var smxMH2 = BuildTheMathHelper(10);
		//	//var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	//var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, -248, precision));
		//	////var aBiSmxRValue = aBiSmx.GetRValue();
		//	//var aBiStr = aBiSmx.GetStringValue();
		//	//Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	Assert.True(haveRequiredPrecision);
		//	Assert.Equal(strA, strB);
		//}

		//[Fact]
		//public void SquareAnRValue()
		//{
		//	var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
		//	var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
		//	var scalarMath = BuildTheMathHelper(limbCount);
		//	Debug.WriteLine($"{nameof(SquareAnRValue)} is using {scalarMath.ApFixedPointFormat}");


		//	var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
		//	var aRValue = new RValue(aBigInteger, -134, precision); // 0.25
		//	var aSmx = scalarMath.CreateSmx(aRValue);
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");
		//	var aMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw operand", aSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the a mantissa is {aMantissaDisp}.");

		//	//var a2Mantissa = scalarMath.Square(aSmx.Mantissa);
		//	//var a2Str = ScalarMathHelper.GetDiagDisplay("raw products", a2Mantissa);
		//	//Debug.WriteLine($"The StringValue for the a2Mantissa is {a2Str}.");

		//	//var a3Mantissa = scalarMath.PropagateCarries(a2Mantissa);
		//	//var a3MantissaNrm = scalarMath.ShiftAndTrim(a3Mantissa);
		//	//var a3 = new Smx(true, a3MantissaNrm, aSmx.Exponent, aSmx.BitsBeforeBP, aSmx.Precision);
		//	//var a3Str = a3.GetStringValue();
		//	//Debug.WriteLine($"The StringValue for the a3Mantissa is {a3Str}.");

		//	var bSmx = scalarMath.Square(aSmx);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
		//	var bSmxRValue = bSmx.GetRValue();
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var bMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
		//	Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

		//	//var bP32Smx = AdjustExponent(bSmx, bSmx.Exponent + 32);
		//	//var bP32Str = bP32Smx.GetStringValue();
		//	//Debug.WriteLine($"The StringValue for the bSmxPlus32 is {bP32Str}.");

		//	var bRValue = aRValue.Square();
		//	var bStrComp = RValueHelper.ConvertToString(bRValue);
		//	Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

		//	var smxMH2 = BuildTheMathHelper(10);
		//	var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, -268, precision));
		//	//var aBiSmxRValue = aBiSmx.GetRValue();
		//	var aBiStr = aBiSmx.GetStringValue();
		//	Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bSmxRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	Assert.True(haveRequiredPrecision);
		//	Assert.Equal(strA, strB);
		//}

		[Fact]
		public void SquareAnRValueXXL()
		{
			var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);
			Debug.WriteLine($"{nameof(SquareAnRValueXXL)} is using {scalarMath.ApFixedPointFormat}");

			var number = "-126445453255269018635038690902017";  // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var exponent = -134;

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");
			Debug.WriteLine($"The StringValue for the aSmx mantissa is {aTv.GetDiagDisplay()}.");

			var bSmx = scalarMath.Square(aTv.SmxValue);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bTv = new SmxTestValue(bSmx, scalarMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");
			Debug.WriteLine($"The StringValue for the bSmx mantissa is {bTv.GetDiagDisplay()}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValueXL()
		{
			var precision = 40;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);
			Debug.WriteLine($"{nameof(SquareAnRValueXL)} is using {scalarMath.ApFixedPointFormat}");

			var number = "12644545325526901863503869";  // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var exponent = -105;

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");
			Debug.WriteLine($"The StringValue for the aSmx mantissa is {aTv.GetDiagDisplay()}.");

			var bSmx = scalarMath.Square(aTv.SmxValue);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bTv = new SmxTestValue(bSmx, scalarMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");
			Debug.WriteLine($"The StringValue for the bSmx mantissa is {bTv.GetDiagDisplay()}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValueL()
		{
			var precision = 30;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);
			Debug.WriteLine($"{nameof(SquareAnRValueL)} is using {scalarMath.ApFixedPointFormat}");

			var number = "1264454532552690186";  // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var exponent = -83;

			var aTv = new SmxTestValue(number, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");
			Debug.WriteLine($"The StringValue for the aSmx mantissa is {aTv.GetDiagDisplay()}.");

			var bSmx = scalarMath.Square(aTv.SmxValue);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bTv = new SmxTestValue(bSmx, scalarMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");
			Debug.WriteLine($"The StringValue for the bSmx mantissa is {bTv.GetDiagDisplay()}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValueM()
		{
			var precision = 25;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 4;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);
			Debug.WriteLine($"{nameof(SquareAnRValueM)} is using {scalarMath.ApFixedPointFormat}");

			var number = "126445453255";  // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10
			var exponent = -60;
			var aTv = new SmxTestValue(number, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");
			Debug.WriteLine($"The StringValue for the aSmx mantissa is {aTv.GetDiagDisplay()}.");

			var bSmx = scalarMath.Square(aTv.SmxValue);                          //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895204420064645514845e-19
			var bTv = new SmxTestValue(bSmx, scalarMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");
			Debug.WriteLine($"The StringValue for the bSmx mantissa is {bTv.GetDiagDisplay()}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Square_ValueWith_6_Limbs()
		{
			//var scalarMath = new scalarMath(-170);
			var precision = 90;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 7;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			//var avSmx = new Smx(false, new ulong[] { 0, 0, 4155170372, 1433657343, 4294967295, 566493183 }, -216, scalarMath.BitsBeforeBP, precision); // TODO: a

			var avSmx = new Smx(true, new ulong[] { 0, 0, 1038792593, 358414336, 1073741824, 141623296 }, -188, scalarMath.BitsBeforeBP, precision); //-0.01648712164023891092251505985352768039098914571773819321.


			var avRValue = avSmx.GetRValue();
			var avStr = avSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the avSmx is {avStr}.");
			var atMantissaDisp = ScalarMathHelper.GetDiagDisplay("av raw operand", avSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the av mantissa is {atMantissaDisp}.");

			var aSmx = scalarMath.CreateSmx(avRValue);
			var aRValue = aSmx.GetRValue();
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the atSmx is {aStr}.");
			var aMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw operand", avSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the mantissa is {aMantissaDisp}.");

			//var a2Mantissa = scalarMath.Square(aSmx.Mantissa);
			//var a2Str = ScalarMathHelper.GetDiagDisplay("raw products", a2Mantissa);
			//Debug.WriteLine($"The StringValue for the a2Mantissa is {a2Str}.");

			//var a3Mantissa = scalarMath.SumThePartials(a2Mantissa);
			////var a3MantissaNrm = scalarMath.ShiftAndTrim(a3Mantissa);
			//var a3MantissaNrm = ScalarMathHelper.ShiftAndTrim(a3Mantissa, scalarMath.ApFixedPointFormat, isSigned: false);

			//var a3 = new Smx(true, a3MantissaNrm, aSmx.Exponent, aSmx.BitsBeforeBP, aSmx.Precision);
			//var a3Str = a3.GetStringValue();
			//Debug.WriteLine($"The StringValue for the a3Mantissa is {a3Str}.");

			var bSmx = scalarMath.Square(aSmx);
			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			//var bMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
			//Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			//var bP32Smx = AdjustExponent(bSmx, bSmx.Exponent - 32);
			//var bP32Str = bP32Smx.GetStringValue();
			//Debug.WriteLine($"The StringValue for the bSmxPlus32 is {bP32Str}.");

			var bRValue = aRValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			//var smxMH2 = BuildTheMathHelper(14);
			//var aBigInteger = ScalarMathHelper.FromPwULongs(aSmx.Mantissa);
			//var aBiSqr = BigInteger.Multiply(aBigInteger, aBigInteger);
			//var aBiSmx = smxMH2.CreateSmx(new RValue(aBiSqr, aSmx.Exponent * 2, precision));
			////var aBiSmxRValue = aBiSmx.GetRValue();
			//var aBiStr = aBiSmx.GetStringValue();
			//Debug.WriteLine($"The value of aBiSqr is {aBiStr}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareCompareWithAndWithoutLeadingZeros()
		{
			var precision = 35;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 4;      // TargetExponent = -184, Total Bits = 192
			var scalarMath = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-343597");
			var aRValue = new RValue(aBigInteger, -11, precision);

			var aSmx = scalarMath.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var a2Mantissa = scalarMath.Square(aSmx.Mantissa);
			var a3Mantissa = scalarMath.SumThePartials(a2Mantissa);
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

			var b2Mantissa = scalarMath.Square(bMantissa);
			var b3Mantissa = scalarMath.SumThePartials(b2Mantissa);
			var b3 = new Smx(true, b3Mantissa, bExponent * 2, aSmx.BitsBeforeBP, bPrecision);
			var b3SmxRValue = b3.GetRValue();
			var b3Str = a3.GetStringValue();
			Debug.WriteLine($"The StringValue for the b3Smx is {b3Str}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(b3SmxRValue, a3SmxRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		#endregion

		#region Multiply Smx x Smx

		[Fact]
		public void Multiply_53_And_70_Sixteenths()
		{
			var precision = 15;
			var limbCount = 2;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), THRESHOLD);

			var aNumber = "53";		// 3.3125	(3 5/16)
			var bNumber = "70";		// 4.375	(4 3/8)					
			var exponent = -4;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cSmx = scalarMath.Multiply(aTv.SmxValue, bTv.SmxValue);

			var cTv = new SmxTestValue(cSmx, scalarMath);
			Debug.WriteLine($"The StringValue for c is {cTv}.");

			var cRValue = aTv.RValue.Mul(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void MultiplyTwoRValues()
		{
			var precision = 53;
			var limbCount = 6;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), THRESHOLD);

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "27797772040142849";
			var bNumber = "4548762148012033";
			var exponent = -62;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cSmx = scalarMath.Multiply(aTv.SmxValue, bTv.SmxValue);

			var cTv = new SmxTestValue(cSmx, scalarMath);
			Debug.WriteLine($"The StringValue for c is {cTv}.");

			var cRValue = aTv.RValue.Mul(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		//public void FullMultiply_Returns_Correct_Value()
		//{
		//	var precision = RMapConstants.DEFAULT_PRECISION;
		//	var numberOfDecimalDigits = DoubleHelper.RoundToZero(DoubleHelper.GetNumberOfDecimalDigits(precision));


		//	var limbCount = 4;
		//	var scalarMath = BuildTheMathHelper(limbCount);

		//	var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
		//	var aMantissa = ScalarMathHelper.ToPwULongs(aBigInteger);

		//	var bMantissa1 = scalarMath.Multiply(aMantissa, aMantissa);
		//	var bMantissa2 = scalarMath.PropagateCarries(bMantissa1);
		//	var bMantissa3 = scalarMath.ShiftAndTrim(bMantissa2);

		//	var bBigInteger = ScalarMathHelper.FromPwULongs(bMantissa3);

		//	var bBigIntegerStr = bBigInteger.ToString().Substring(0, numberOfDecimalDigits);

		//	var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	var bCompStr = bCompBigInteger.ToString().Substring(0, numberOfDecimalDigits);	

		//	Assert.Equal(bCompStr, bBigIntegerStr);
		//}

		#endregion

		#region Multiply Smx x Int

		[Fact]
		public void MultiplyAnRValueWithInt()
		{
			var precision = 53;
			var limbCount = 3;
			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), 4u);

			//var aRvalue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4

			//var aBigInteger = BigInteger.Parse("-27797772040142849");
			//var aRValue = new RValue(aBigInteger, -62, precision);

			var aBigInteger = BigInteger.Parse("-36507222016");
			var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aSmx = scalarMath.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var b = 3;
			Debug.WriteLine($"The StringValue for the bSmx is {b}.");

			var cSmx = scalarMath.Multiply(aSmx, b);
			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			var cRValue = aRValue.Mul(b);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cSmxRValue, cRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		#endregion

		#region Add / Subtract

		[Fact]
		public void AddTwoPositive()
		{
			var precision = 53;
			var limbCount = 3;
			//var valueCount = 8;
			var threshold = 4u;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), threshold);

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			var aNumber = "-414219082";
			var bNumber = "67781838";
			var exponent = -36;

			//var aNumber = "27797772040142849";
			//var bNumber = "4548762148012033";
			//var exponent = -62;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath.Add(aTv.SmxValue, bTv.SmxValue, "Test");
			var cTv = new SmxTestValue(c, scalarMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddTwoNegative()
		{
			var precision = 53;
			var limbCount = 3;
			//var valueCount = 8;
			var threshold = 4u;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), threshold);

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			var aNumber = "-414219082";
			var bNumber = "67781838"; // "-7781838";
			var exponent = -36;

			//var aNumber = "-27797772040142849";
			//var bNumber = "-4548762148012033";
			//var exponent = -62;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath.Add(aTv.SmxValue, bTv.SmxValue, "Test");
			var cTv = new SmxTestValue(c, scalarMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			//var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), threshold);
			//var d = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "d");
			//var dTv = new Smx2CTestValue(d, scalarMath2C);
			//Debug.WriteLine($"The StringValue for the dSmx is {dTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddLeftIsPosRightIsNeg()
		{
			var precision = 53;
			var limbCount = 4;
			//var valueCount = 8;
			var threshold = 4u;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), threshold);

			//var aNumber = "343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			//var aNumber = "414219082";
			//var bNumber = "67781838"; // "-7781838";
			//var exponent = -36;

			var aNumber = "27797772040142849";
			var bNumber = "-4548762148012033";
			var exponent = -62;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath.Add(aTv.SmxValue, bTv.SmxValue, "Test");
			var cTv = new SmxTestValue(c, scalarMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddLeftIsNegRightIsPos()
		{
			var precision = 53;
			var limbCount = 4;
			//var valueCount = 8;
			var threshold = 4u;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), threshold);

			//var aNumber = "343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			//var aNumber = "414219082";
			//var bNumber = "67781838"; // "-7781838";
			//var exponent = -36;

			var aNumber = "-27797772040142849";
			var bNumber = "4548762148012033";
			var exponent = -62;

			var aTv = new SmxTestValue(aNumber, exponent, precision, scalarMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new SmxTestValue(bNumber, exponent, precision, scalarMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath.Add(aTv.SmxValue, bTv.SmxValue, "Test");
			var cTv = new SmxTestValue(c, scalarMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			//var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), threshold);
			//var d = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "d");
			//var dTv = new Smx2CTestValue(d, scalarMath2C);
			//Debug.WriteLine($"The StringValue for the dSmx is {dTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		//[Fact]
		public void AddLeftIsNegRightIsPosSmall()
		{
			var precision = 25;
			var limbCount = 5;
			//var valueCount = 8;
			var threshold = 4u;

			var scalarMath = new ScalarMath(new ApFixedPointFormat(limbCount), threshold);

			//var a = new Smx(false, new ulong[] { 151263699, 55238551, 1 }, 2, -63, precision);
			//var b = new Smx(true, new ulong[] { 86140672, 2, 0 }, 1, -36, precision);

			//var aLongs = new ulong[] {1512, 552, 1 };
			var aLongs5 = new ulong[] { 0, 0, 3489660928, 1342177291, 33554436 }; // 5 Limbs, Exp -152, Value: -2.000000257045030757803438
			var aBigInteger = -1 * ScalarMathHelper.FromPwULongs(aLongs5);
			var aRValueStg = new RValue(aBigInteger, -63, precision);

			//var bLongs = new ulong[] { 8614, 2, 0 };
			var bLongs5 = new ulong[] { 0, 0, 0, 442499072, 2097154 }; // 5 Limbs, Exp: -152, Value: 0.1250001253501977771520615
			var bBigInteger = ScalarMathHelper.FromPwULongs(bLongs5);
			var bRValueStg = new RValue(bBigInteger, -36, precision);

			var aRValue = RNormalizer.Normalize(aRValueStg, bRValueStg, out var bRValue);

			var aSmx = scalarMath.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var bSmx = scalarMath.CreateSmx(bRValue);
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var cSmx = scalarMath.Add(aSmx, bSmx, "Test");   // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			//var nrmA = RNormalizer.Normalize(aRValue, bRValue, out var nrmB);
			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cSmxRValue, cRValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		#endregion

		#region Support Methods

		private ScalarMath BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath(new ApFixedPointFormat(limbCount), THRESHOLD);
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