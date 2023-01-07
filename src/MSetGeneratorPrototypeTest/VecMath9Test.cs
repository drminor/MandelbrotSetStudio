using MSS.Common.APValues;
using MSS.Common;
using System.Diagnostics;
using EngineTest;
using MSetGeneratorPrototype;

namespace MSetGeneratorPrototypeTest
{
	public class VecMath9Test
	{
		private const int VALUE_COUNT = 8;
		private const uint THRESHOLD = 4;

		#region Square and Multiply

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new VecTestValue("36507222016", -33, precision, smxMathHelper); // -4.25

			var aTv = new FP31DeckTestVal("2147483648", -33, precision, vecMath9); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			// Vec Square
			var bFPValus = aTv.CreateNewFP31Deck();
			vecMath9.Square(aTv.Vectors, result: bFPValus);

			var bTv = new FP31DeckTestVal(bFPValus, vecMath9);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

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
		public void SquareFourAndAQuarterNewTech()
		{
			var precision = 30;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new VecTestValue("36507222016", -33, precision, smxMathHelper); // -4.25

			var aTv = new FP31DeckTestVal("2147483648", -33, precision, vecMath9); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			// Vec Square
			var bFPValus = aTv.CreateNewFP31Deck();
			vecMath9.Square(aTv.Vectors, result: bFPValus);

			var bTv = new FP31DeckTestVal(bFPValus, vecMath9);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");
			Debug.WriteLine($"The StringValue for the result mantissa is {bTv.GetDiagDisplay()}.");

			// RValue Square 
			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
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

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aNumber = "-343597";
			//var bNumber = "-343707";
			//var exponent = -12;

			var aNumber = "414219082";
			var bNumber = "67781838";
			var exponent = -37;                 //9.8635556059889495372772216796875e-4

			//var aNumber = "27797772040142849";
			//var bNumber = "4548762148012033";
			//var exponent = -62;

			var aTv = new FP31DeckTestVal(aNumber, exponent, precision, vecMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31DeckTestVal(bNumber, exponent, precision, vecMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFP31Deck();
			vecMath9.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new FP31DeckTestVal(cFPValues, vecMath9);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddTwoNegative()
		{
			var precision = 23;
			var limbCount = 3;

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			var aNumber = "-343597";
			var bNumber = "-343707";
			var exponent = -17;

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			//var aNumber = "-27797772040142849";
			//var bNumber = "-4548762148012033";
			//var exponent = -63;

			var aTv = new FP31DeckTestVal(aNumber, exponent, precision, vecMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31DeckTestVal(bNumber, exponent, precision, vecMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFP31Deck();

			vecMath9.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);

			var cTv = new FP31DeckTestVal(cFPValues, vecMath9);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Add_LeftIs_Pos_RightIs_Neg()
		{
			var precision = 25;
			var limbCount = 5;

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "27797772040142849";
			var bNumber = "-4548762148012033";
			var exponent = -55;

			var aTv = new FP31DeckTestVal(aNumber, exponent, precision, vecMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31DeckTestVal(bNumber, exponent, precision, vecMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFP31Deck();
			vecMath9.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new FP31DeckTestVal(cFPValues, vecMath9);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Add_LeftIs_BigPos_RightIs_Neg()
		{
			var precision = 38;
			var limbCount = 6;

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "4548762148012033";
			var bNumber = "-27797772040142849";
			var exponent = -75;

			var aTv = new FP31DeckTestVal(aNumber, exponent, precision, vecMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31DeckTestVal(bNumber, exponent, precision, vecMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFP31Deck();
			vecMath9.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new FP31DeckTestVal(cFPValues, vecMath9);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var doneFlags = vecMath9.DoneFlags.Select(x => x.ToString()).ToArray();
			var doneFlagsStr = string.Join(", ", doneFlags);
			Debug.WriteLine(doneFlagsStr);

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void Add_LeftIs_Neg_RightIs_Pos()
		{
			var precision = 40;
			var limbCount = 4;

			var vecMath9 = BuildTheVecMath9(limbCount, VALUE_COUNT, THRESHOLD);

			//var aNumber = "-414219082";
			//var bNumber = "67781838";
			//var exponent = -36;

			var aNumber = "-27797772040142849";
			var bNumber = "4548762148012033";
			var exponent = -66;


			var aTv = new FP31DeckTestVal(aNumber, exponent, precision, vecMath9); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new FP31DeckTestVal(bNumber, exponent, precision, vecMath9); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFP31Deck();
			vecMath9.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new FP31DeckTestVal(cFPValues, vecMath9);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.RValue.Add(bTv.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		////[Fact]
		//public void AddLeftIsNegRightIsPosSmall()
		//{
		//	var precision = 25;
		//	var limbCount = 5;
		//	//var valueCount = 8;
		//	var threshold = 4u;

		//	var vecMath2C = BuildTheVecMathHelper2C(limbCount, VALUE_COUNT, threshold);

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

		//	var aTv = new Vec2CTestValue(aRValue, vecMath2C);
		//	Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

		//	var bTv = new Vec2CTestValue(bRValue, vecMath2C);
		//	Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

		//	var cFPValues = bTv.CreateNewFPValues();
		//	vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);        // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

		//	var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
		//	Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

		//	var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
		//	var cStrComp = RValueHelper.ConvertToString(cRValue);
		//	Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

		//	var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
		//	//Assert.True(haveRequiredPrecision);

		//	Assert.Equal(strA, strB);
		//}

		#endregion

		#region Support Methods

		private VecMath9 BuildTheVecMath9(int limbCount, int valueCount, uint threshold)
		{
			var result = new VecMath9(new ApFixedPointFormat(limbCount), valueCount, threshold);
			return result;
		}

		#endregion
	}
}