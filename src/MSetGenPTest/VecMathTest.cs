using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class VecMathTest
	{
		private const int VALUE_COUNT = 8;
		private const uint THRESHOLD = 4;

		#region Square and Multiply

		[Fact]
		public void SquareFourAndAQuarterNewTech()
		{
			var precision = 14;		// Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64

			var vecMath = BuildTheVecMathHelper(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new VecTestValue("36507222016", -33, precision, smxMathHelper); // -4.25

			var aTv = new VecTestValue("2147483648", -33, precision, vecMath); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			// Vec Square
			var bFPValus = aTv.CreateNewFPValues();
			vecMath.Square(aTv.Vectors, result: bFPValus);

			var bTv = new VecTestValue(bFPValus, vecMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

			var bMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw result", bTv.SmxTestValue.SmxValue.Mantissa);
			Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			// RValue Square 
			var bRValue = aTv.SmxTestValue.RValue.Square();
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

			var vecMath = BuildTheVecMathHelper(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, vecMath); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, vecMath); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new VecTestValue("27797772040142849", -62, precision, vecMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new VecTestValue("4548762148012033", -62, precision, vecMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new VecTestValue(cFPValues, vecMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.SmxTestValue.RValue.Add(bTv.SmxTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddTwoNegative()
		{
			var precision = 53;
			var limbCount = 3;

			var vecMath = BuildTheVecMathHelper(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, vecMath); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, vecMath); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new VecTestValue("-27797772040142849", -62, precision, vecMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new VecTestValue("-4548762148012033", -62, precision, vecMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new VecTestValue(cFPValues, vecMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.SmxTestValue.RValue.Add(bTv.SmxTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddLeftIsPosRightIsNeg()
		{
			var precision = 53;
			var limbCount = 3;

			var vecMath = BuildTheVecMathHelper(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, vecMath); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, vecMath); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new VecTestValue("+27797772040142849", -62, precision, vecMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new VecTestValue("-4548762148012033", -62, precision, vecMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new VecTestValue(cFPValues, vecMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.SmxTestValue.RValue.Add(bTv.SmxTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void AddLeftIsNegRightIsPos()
		{
			var precision = 53;
			var limbCount = 3;

			var vecMath = BuildTheVecMathHelper(limbCount, VALUE_COUNT, THRESHOLD);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, vecMath); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, vecMath); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new VecTestValue("-27797772040142849", -62, precision, vecMath); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new VecTestValue("+4548762148012033", -62, precision, vecMath); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new VecTestValue(cFPValues, vecMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.SmxTestValue.RValue.Add(bTv.SmxTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		//[Fact]
		public void AddLeftIsNegRightIsPosSmall()
		{
			var precision = 25;
			var limbCount = 5;
			var valueCount = 8;
			var threshold = 4u;

			var vecMath = new VecMath(new ApFixedPointFormat(limbCount), valueCount, threshold);

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

			var aTv = new VecTestValue(aRValue, vecMath);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

			var bTv = new VecTestValue(bRValue, vecMath);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

			var cFPValues = bTv.CreateNewFPValues();
			vecMath.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);        // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

			var cTv = new VecTestValue(cFPValues, vecMath);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.SmxTestValue.RValue.Add(bTv.SmxTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		#endregion

		#region Support Methods

		private ScalarMath BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

		private VecMath BuildTheVecMathHelper(int limbCount, int valueCount, uint threshold)
		{
			var result = new VecMath(new ApFixedPointFormat(limbCount), valueCount, threshold);
			return result;
		}

		#endregion
	}
}