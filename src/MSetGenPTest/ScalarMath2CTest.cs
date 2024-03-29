using MSetGenP;
using MSS.Common;
using MSetGenP.Types;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace MSetGenPTest
{
	public class ScalarMath2CTest
	{
		#region Square and Multiply

		private const uint THRESHOLD = 4;

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var number = "-36507222016"; // \w -33 Exp
			
			var number = "2147483648";
			var exponent = -33;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			Smx2C b = scalarMath2C.Square(aTv.Smx2CValue);
			var bTv = new Smx2CTestValue(b, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {bTv}.");
			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			//var bSmx = scalarMath2C.Convert(bTv.Smx2CValue);
			//var cTv = new SmxTestValue(bSmx, new ScalarMath(new ApFixedPointFormat(limbCount), THRESHOLD));

			// RValue Square 
			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValueSm()
		{
			var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var number = "-12644545325526901863503869090"; // with exponent -124


			var number = "-1264454532552690186350"; // 1.0710346493771638176866460188605
			var exponent = -70;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			Smx2C b = scalarMath2C.Square(aTv.Smx2CValue);
			var bTv = new Smx2CTestValue(b, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {bTv}.");
			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			// RValue Square 
			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");


			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareAnRValue()
		{
			var precision = 70;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 6;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);


			var number = "-126445453255269018635038690902017";
			var exponent = -134;

			var aTv = new Smx2CTestValue(number, exponent, precision, scalarMath2C); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			Smx2C b = scalarMath2C.Square(aTv.Smx2CValue);
			var bTv = new Smx2CTestValue(b, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			// RValue Square 
			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareCompareWithAndWithoutLeadingZeros()
		{
			var precision = 35;    // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 4;      // TargetExponent = -184, Total Bits = 192
			var scalarMath2C = BuildTheMathHelper(limbCount);

			//var aBigInteger = BigInteger.Parse("-343597");
			//var aRValue = new RValue(aBigInteger, -11, precision);

			var aTv = new Smx2CTestValue("343597", -12, precision, scalarMath2C); // -167.77197265625
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			// Create a3Smx
			var a2Mantissa = scalarMath2C.Square(aTv.Smx2CValue.Mantissa);
			var a3Mantissa = scalarMath2C.SumThePartials(a2Mantissa, out var carry);

			var a3 = new Smx2C(true, a3Mantissa, aTv.Smx2CValue.Exponent * 2, aTv.Smx2CValue.BitsBeforeBP, aTv.Smx2CValue.Precision);

			var a3Tv = new Smx2CTestValue(a3, scalarMath2C);
			Debug.WriteLine($"The StringValue for the a3 is {a3Tv}.");

			// Add a leading zero
			var bMantissaLst = a3Tv.Smx2CValue.Mantissa.ToList();
			bMantissaLst.Add(0);
			var bMantissa = bMantissaLst.ToArray();

			// Create b3Smx
			var b2Mantissa = scalarMath2C.Square(bMantissa);

			var b3Mantissa = scalarMath2C.SumThePartials(b2Mantissa, out var carry2);
			var b3 = new Smx2C(true, b3Mantissa, aTv.Smx2CValue.Exponent * 2, aTv.Smx2CValue.BitsBeforeBP, aTv.Smx2CValue.Precision);
			var b3Tv = new Smx2CTestValue(b3, scalarMath2C);
			Debug.WriteLine($"The StringValue for the b3 is {b3Tv}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(a3Tv.RValue, b3Tv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);

			// Adding a leading zero will change the result since this zero is being added before the binary point.
			Assert.NotEqual(strA, strB);
		}

		[Fact]
		public void FullMultiply_Returns_Correct_Value()
		{
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath2C = BuildTheMathHelper(limbCount);

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = ScalarMathHelper.ToPwULongs(aBigInteger, out _);

			var bMantissa = scalarMath2C.Multiply(aMantissa, aMantissa);
			var bBigInteger = ScalarMathHelper.FromPwULongs(bMantissa, true);

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
		public void AddTwoPositive()
		{
			var precision = 53;
			var limbCount = 3;

			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			var aNumber = "414219082";
			var bNumber = "67781838";
			var exponent = -37;                 //9.8635556059889495372772216796875e-4

			//var aNumber = "27797772040142849";
			//var bNumber = "4548762148012033";
			//var exponent = -62;

			var aTv = new Smx2CTestValue(aNumber, exponent, precision, scalarMath2C);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Smx2CTestValue(bNumber, exponent, precision, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
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
			var precision = 23;
			var limbCount = 3;

			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

			var aNumber = "-343597";
			var bNumber = "-343707";
			var exponent = -17;

			//var aNumber = "-414219082";
			//var bNumber = "-7781838"; //67781838
			//var exponent = -36;

			//var aNumber = "-27797772040142849";
			//var bNumber = "-4548762148012033";
			//var exponent = -63;

			var aTv = new Smx2CTestValue(aNumber, exponent, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Smx2CTestValue(bNumber, exponent, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Add_LeftIs_Pos_RightIs_Neg()
		{
			var precision = 25;
			var limbCount = 5;

			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "27797772040142849";
			var bNumber = "-4548762148012033";
			var exponent = -55;

			// SWITCHED
			var aTv = new Smx2CTestValue(aNumber, exponent, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Smx2CTestValue(bNumber, exponent, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Add_LeftIs_BigPos_RightIs_Neg()
		{
			var precision = 38;
			var limbCount = 6; // Target Exponent = -147 (31 x 5 = 155, subtract 8 = 147. 5 x 32 = 160

			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			//Expected: ??; Actual: ??

			var aNumber = "4548762148012033";					// 9.8635556059889517056815666506964e-4
			var bNumber = "-27797772040142849";					//-6.02768096723593793141715568851e-3
			var exponent = -75;                     // Result: -0.00504132540663704276084899902344

			// 4,548,762,148,012,033 + -27,797,772,040,142,849 = -23,249,009,892,130,816

			// -23,249,009 B = 2,838,013,902,848 2^147 / 2^160 (or * 1/2^13)

			//Expected: -86609311; Actual: 4397959902817

			// SWITCHED
			var aTv = new Smx2CTestValue(aNumber, exponent, precision, scalarMath2C);
			Debug.WriteLine($"The StringValue for b is {aTv}.");

			var bTv = new Smx2CTestValue(bNumber, exponent, precision, scalarMath2C);
			Debug.WriteLine($"The StringValue for a is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRef is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddLeftIs_Neg_RightIs_Pos()
		{
			var precision = 38;
			var limbCount = 5;

			var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "-27797772040142849";
			var bNumber =   "4548762148012033";
			var exponent = -65;

			var aTv = new Smx2CTestValue(aNumber, exponent, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Smx2CTestValue(bNumber, exponent, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath2C.Add(aTv.Smx2CValue, bTv.Smx2CValue, "Test");
			var cTv = new Smx2CTestValue(c, scalarMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		//public void AddLeftIsNegRightIsPosSmall()
		//{
		//	var precision = 25;
		//	var limbCount = 5;
		//	//var valueCount = 8;

		//	var scalarMath2C = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);

		//	//var a = new Smx(false, new ulong[] { 151263699, 55238551, 1 }, 2, -63, precision);
		//	//var b = new Smx(true, new ulong[] { 86140672, 2, 0 }, 1, -36, precision);

		//	//var aLongs = new ulong[] {1512, 552, 1 };
		//	var aLongs5 = new ulong[] { 0, 0, 3489660928, 1342177291, 33554436 }; // 5 Limbs, Exp -152, Value: -2.000000257045030757803438
		//	var aBigInteger = -1 * ScalarMathHelper.FromPwULongs(aLongs5);
		//	var aRValueStg = new RValue(aBigInteger, -63, precision);

		//	//var bLongs = new ulong[] { 8614, 2, 0 };
		//	var bLongs5 = new ulong[] { 0, 0, 0, 442499072, 2097154 }; // 5 Limbs, Exp: -152, Value: 0.1250001253501977771520615
		//	var bBigInteger = ScalarMathHelper.FromPwULongs(bLongs5);
		//	var bRValueStg = new RValue(bBigInteger, -36, precision);

		//	var aRValue = RNormalizer.Normalize(aRValueStg, bRValueStg, out var bRValue);

		//	var aSmx = scalarMath2C.CreateSmx2C(aRValue);
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

		//	var bSmx = scalarMath2C.CreateSmx2C(bRValue);
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var cSmx = scalarMath2C.Add(aSmx, bSmx, "Test");   // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

		//	var cSmxRValue = cSmx.GetRValue();
		//	var cStr = cSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

		//	//var nrmA = RNormalizer.Normalize(aRValue, bRValue, out var nrmB);
		//	var cRValue = aRValue.Add(bRValue);
		//	var cStrComp = RValueHelper.ConvertToString(cRValue);
		//	Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cSmxRValue, cRValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	//Assert.True(haveRequiredPrecision);

		//	Assert.Equal(strA, strB);
		//}

		#endregion

		#region Support Methods

		private ScalarMath2C BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath2C(new ApFixedPointFormat(limbCount), THRESHOLD);
			return result;
		}

		#endregion
	}

}