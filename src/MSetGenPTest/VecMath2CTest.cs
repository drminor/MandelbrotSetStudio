using Microsoft.VisualBasic;
using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;

namespace EngineTest
{
	public class VecMath2CTest
	{
		private const int valueCount = 8;
		private const uint threshold = 4;

		#region Square and Multiply

		//[Fact]
		public void SquareFourAndAQuarterNewTech()
		{
			var precision = 14;		// Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var valueCount = 8;
			var threshold = 4u;

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

			//var aTv = new VecTestValue("36507222016", -33, precision, smxMathHelper); // -4.25

			var aTv = new Vec2CTestValue("2147483648", -33, precision, vecMath2C); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			// Vec Square
			var bFPValus = aTv.CreateNewFPValues();
			vecMath2C.Square(aTv.Vectors, result: bFPValus);

			var bTv = new Vec2CTestValue(bFPValus, vecMath2C);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

			var bMantissaDisp = ScalarMathHelper.GetDiagDisplay("raw result", bTv.Smx2CTestValue.SmxValue.Mantissa);
			Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			// RValue Square 
			var bRValue = aTv.Smx2CTestValue.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bRValue, bTv.Smx2CTestValue.RValue, failOnTooFewDigits: false, out var strA, out var strB);
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

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new Vec2CTestValue("27797772040142849", -63, precision, vecMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Vec2CTestValue("4548762148012033", -63, precision, vecMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
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

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new Vec2CTestValue("-","27797772040142849", -63, precision, vecMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Vec2CTestValue("-", "4548762148012033", -63, precision, vecMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
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

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			var aTv = new Vec2CTestValue("+", "4548762148012033", -63, precision, vecMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {aTv}.");

			var bTv = new Vec2CTestValue("-", "27797772040142849", -63, precision, vecMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {bTv}.");

			var cFPValues = aTv.CreateNewFPValues();
			vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
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

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

			//var aTv = new Smx2CTestValue("-414219082", -36, precision, scalarMath2C); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Smx2CTestValue("67781838", -36, precision, scalarMath2C); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");

			//var aTv = new Vec2CTestValue("-", "27797772040142849", -65, precision, vecMath2C); // -6.02768096723593793141715568851e-3
			//Debug.WriteLine($"The StringValue for a is {aTv}.");

			//var bTv = new Vec2CTestValue("+", "4548762148012033", -65, precision, vecMath2C); // 9.8635556059889517056815666506964e-4
			//Debug.WriteLine($"The StringValue for b is {bTv}.");


			var aTv = new Vec2CTestValue("-", "2779777204014", -65, precision, vecMath2C); // -6.02768096723593793141715568851e-3
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var bTv = new Vec2CTestValue("+", "454876214801", -65, precision, vecMath2C); // 9.8635556059889517056815666506964e-4
			Debug.WriteLine($"The StringValue for b is {bTv}.");



			var cFPValues = aTv.CreateNewFPValues();
			vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);
			var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
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
			//var valueCount = 8;
			var threshold = 4u;

			var vecMath2C = BuildTheVecMathHelper2C(limbCount, valueCount, threshold);

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

			var aTv = new Vec2CTestValue(aRValue, vecMath2C);
			Debug.WriteLine($"The StringValue for the aSmx is {aTv}.");

			var bTv = new Vec2CTestValue(bRValue, vecMath2C);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

			var cFPValues = bTv.CreateNewFPValues();
			vecMath2C.Add(aTv.Vectors, bTv.Vectors, c: cFPValues);        // 5 Limbs, Exp -152, Mantissa: { 0, 0, 3489660928, 899678219, 31457282 }, Value: -1.875000131694832980651377

			var cTv = new Vec2CTestValue(cFPValues, vecMath2C);
			Debug.WriteLine($"The StringValue for the cSmx is {cTv}.");

			var cRValue = aTv.Smx2CTestValue.RValue.Add(bTv.Smx2CTestValue.RValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the expected cSmx is {cStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(cRValue, cTv.RValue, failOnTooFewDigits: false, out var strA, out var strB);
			//Assert.True(haveRequiredPrecision);

			Assert.Equal(strA, strB);
		}

		#endregion

		#region Support Methods

		private ScalarMath2C BuildTheMathHelper(int limbCount)
		{
			var result = new ScalarMath2C(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

		private VecMath2C BuildTheVecMathHelper2C(int limbCount, int valueCount, uint threshold)
		{
			var result = new VecMath2C(new ApFixedPointFormat(limbCount), valueCount, threshold);
			return result;
		}

		#endregion
	}
}