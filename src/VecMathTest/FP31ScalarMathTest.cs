using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using System.Diagnostics;

namespace VecMathTest
{
	public class FP31ScalarMathTest
	{
		#region Square

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var scalarMath9 = BuildTheMathHelper(limbCount);

			//var number = "-36507222016"; // \w -33 Exp
			
			var number = "2147483648";
			var exponent = -33;

			var aTv = new FP31ValTestValue(number, exponent, precision, scalarMath9); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			FP31Val b = scalarMath9.Square(aTv.FP31Val);
			var bTv = new FP31ValTestValue(b);
			Debug.WriteLine($"The StringValue for b is {bTv}.");
			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			//var bSmx = scalarMath9.Convert(bTv.FP31Val);
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
			var scalarMath9 = BuildTheMathHelper(limbCount);

			//var number = "-12644545325526901863503869090"; // with exponent -124


			var number = "-1264454532552690186350"; // 1.0710346493771638176866460188605
			var exponent = -70;

			var aTv = new FP31ValTestValue(number, exponent, precision, scalarMath9); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			FP31Val b = scalarMath9.Square(aTv.FP31Val);
			var bTv = new FP31ValTestValue(b);
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
			var fp31ScalarMath = BuildTheMathHelper(limbCount);


			var number = "-126445453255269018635038690902017";
			var exponent = -134;

			var aTv = new FP31ValTestValue(number, exponent, precision, fp31ScalarMath); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			FP31Val b = fp31ScalarMath.Square(aTv.FP31Val);
			var bTv = new FP31ValTestValue(b);
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

		//[Fact]
		//public void FullMultiply_Returns_Correct_Value()
		//{
		//	var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
		//	var scalarMath9 = BuildTheMathHelper(limbCount);

		//	var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
		//	var aMantissa = FP31ValHelper.ToFwUInts(aBigInteger, out var sign);

		//	var bMantissa = scalarMath9.Multiply(aMantissa, aMantissa);
		//	var bBigInteger = ScalarMathHelper.FromPwULongs(bMantissa, sign: true);

		//	var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	Assert.Equal(bBigInteger, bCompBigInteger);
		//}

		#endregion

		#region Multiply Smx x Int

		[Fact]
		public void MultiplyAnRValueWithInt()
		{
			var precision = 53;
			var limbCount = 3;
			var scalarMath9 = BuildTheMathHelper(limbCount);

			var aNumber = "-36507222016";		// -4.25
			var exponent = -33;

			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var b = 3u;
			Debug.WriteLine($"The StringValue for the bSmx is {b}.");

			var cFP31Val = scalarMath9.Multiply(aTv.FP31Val, b);

			var cTv = new FP31ValTestValue(cFP31Val);
			Debug.WriteLine($"The StringValue for c is {cTv}.");	// -12.75

			var cRValue = aTv.RValue.Mul((int)b);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
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

			var scalarMath9 = new FP31ScalarMath(new ApFixedPointFormat(limbCount));

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			var aNumber = "414219082";
			var bNumber = "67781838";
			var exponent = -37;                 //9.8635556059889495372772216796875e-4

			//var aNumber = "27797772040142849";
			//var bNumber = "4548762148012033";
			//var exponent = -62;

			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9);
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31ValTestValue(bNumber, exponent, precision, scalarMath9);
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath9.Add(aTv.FP31Val, bTv.FP31Val, "Test");
			var cTv = new FP31ValTestValue(c);
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

			var scalarMath9 = new FP31ScalarMath(new ApFixedPointFormat(limbCount));

			var aNumber = "-343597";
			var bNumber = "-343707";
			var exponent = -17;

			//var aNumber = "-414219082";
			//var bNumber = "-7781838"; //67781838
			//var exponent = -36;

			//var aNumber = "-27797772040142849";
			//var bNumber = "-4548762148012033";
			//var exponent = -63;

			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31ValTestValue(bNumber, exponent, precision, scalarMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath9.Add(aTv.FP31Val, bTv.FP31Val, "Test");
			var cTv = new FP31ValTestValue(c);
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

			var scalarMath9 = new FP31ScalarMath(new ApFixedPointFormat(limbCount));

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "27797772040142849";
			var bNumber = "-4548762148012033";
			var exponent = -55;

			// SWITCHED
			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31ValTestValue(bNumber, exponent, precision, scalarMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath9.Add(aTv.FP31Val, bTv.FP31Val, "Test");
			var cTv = new FP31ValTestValue(c);
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

			var scalarMath9 = new FP31ScalarMath(new ApFixedPointFormat(limbCount));

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
			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9);
			Debug.WriteLine($"The StringValue for b is {aTv}.");

			var bTv = new FP31ValTestValue(bNumber, exponent, precision, scalarMath9);
			Debug.WriteLine($"The StringValue for a is {bTv}.");

			var c = scalarMath9.Add(aTv.FP31Val, bTv.FP31Val, "Test");
			var cTv = new FP31ValTestValue(c);
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

			var scalarMath9 = new FP31ScalarMath(new ApFixedPointFormat(limbCount));

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "-27797772040142849";
			var bNumber =   "4548762148012033";
			var exponent = -65;

			var aTv = new FP31ValTestValue(aNumber, exponent, precision, scalarMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31ValTestValue(bNumber, exponent, precision, scalarMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var c = scalarMath9.Add(aTv.FP31Val, bTv.FP31Val, "Test");
			var cTv = new FP31ValTestValue(c);
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

		//	var scalarMath9 = new scalarMath9(new ApFixedPointFormat(limbCount), THRESHOLD);

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

		//	var aSmx = scalarMath9.CreateSmx2C(aRValue);
		//	var aStr = aSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

		//	var bSmx = scalarMath9.CreateSmx2C(bRValue);
		//	var bStr = bSmx.GetStringValue();
		//	Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

		//	var cSmx = scalarMath9.Add(aSmx, bSmx, "Test");   // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

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

		private FP31ScalarMath BuildTheMathHelper(int limbCount)
		{
			var result = new FP31ScalarMath(new ApFixedPointFormat(limbCount));
			return result;
		}

		#endregion
	}

}