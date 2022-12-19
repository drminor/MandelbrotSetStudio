using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class VecMathTest
	{
		#region Square and Multiply

		[Fact]
		public void SquareFourAndAQuarterNewTech()
		{
			var precision = 14;		// Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var valueCount = 8;
			var threshold = 4u;

			var smxMathHelper = BuildTheMathHelper(limbCount);
			var smxVecMathHelper = BuildTheVecMathHelper(limbCount, valueCount, threshold);

			/* Old stuff
			//var aBigInteger = BigInteger.Parse("-36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aBigInteger = BigInteger.Parse("2147483648");
			var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			*/

			// New Stuff
			//var aTvC = new Smx2CTestValue("-36507222016", -33, precision, fpMathHelper); // -4.25

			var aTv = new SmxTestValue("2147483648", -33, precision, smxMathHelper); // 0.25
			Debug.WriteLine($"The StringValue for a is {aTv}.");

			var aFPVals = CreateTestValues(aTv.SmxValue, valueCount);
			var rFPValus = new FPValues(limbCount, valueCount);

			var aCompSmx = smxVecMathHelper.GetSmxAtIndex(aFPVals, index: 0);
			var aCompSmxRValue = aCompSmx.GetRValue();
			var aCompStr = aCompSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aCompSmx is {aCompStr}.");

			smxVecMathHelper.Square(aFPVals, result: rFPValus);

			var bSmx = smxVecMathHelper.GetSmxAtIndex(rFPValus, index: 0);
			var bTv = new SmxTestValue(bSmx, smxMathHelper);
			Debug.WriteLine($"The StringValue for the bSmx is {bTv}.");

			var bMantissaDisp = ScalerMathHelper.GetDiagDisplay("raw result", bSmx.Mantissa);
			Debug.WriteLine($"The StringValue for the result mantissa is {bMantissaDisp}.");

			var bRValue = aTv.RValue.Square();
			var bStrComp = RValueHelper.ConvertToString(bRValue);
			Debug.WriteLine($"The StringValue for the bRValue is {bStrComp}.");

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bTv.RValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		[Fact]
		public void SquareFourAndAQuarter()
		{
			var precision = 14;     // Binary Digits of precision, 30 Decimal Digits
			var limbCount = 2;      // TargetExponent = -56, Total Bits = 64
			var valueCount = 8;
			var threshold = 4u;

			var smxMathHelper = BuildTheMathHelper(limbCount);
			var smxVecMathHelper = BuildTheVecMathHelper(limbCount, valueCount, threshold);

			//var aBigInteger = BigInteger.Parse("-36507222016");
			//var aRValue = new RValue(aBigInteger, -33, precision); // -4.25

			var aBigInteger = BigInteger.Parse("2147483648");
			var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var aFPVals = CreateTestValues(aSmx, valueCount);
			var rFPValus = new FPValues(limbCount, valueCount);

			var aCompSmx = smxVecMathHelper.GetSmxAtIndex(aFPVals, index: 0);
			var aCompSmxRValue = aCompSmx.GetRValue();
			var aCompStr = aCompSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aCompSmx is {aCompStr}.");

			smxVecMathHelper.Square(aFPVals, result: rFPValus);

			var bSmx = smxVecMathHelper.GetSmxAtIndex(rFPValus, index: 0);

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

			var haveRequiredPrecision = RValueHelper.GetStringsToCompare(bSmxRValue, bRValue, failOnTooFewDigits: false, out var strA, out var strB);
			Assert.True(haveRequiredPrecision);
			Assert.Equal(strA, strB);
		}

		#endregion

		#region Add / Subtract

		[Fact]
		public void AddTwoRValues()
		{
			var precision = 53;
			var limbCount = 3;
			//var valueCount = 8;
			var threshold = 4u;

			var smxMathHelper = new ScalerMath(new ApFixedPointFormat(limbCount), threshold);

			//var aRvalue = new RValue(new BigInteger(-414219082), -36, precision); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, precision); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(27797772040142849), -62, precision); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, precision); // 9.8635556059889517056815666506964e-4

			var a = smxMathHelper.CreateSmx(aRValue);
			var b = smxMathHelper.CreateSmx(bRValue);
 
			var c = smxMathHelper.Add(a, b, "Test");
			var cSmxRValue = c.GetRValue();
			var cStr = c.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the aSmx is {cStrComp}.");

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		[Fact]
		public void AddTwoRValuesUseSub()
		{
			var precision = 25;
			var limbCount = 5;
			//var valueCount = 8;
			var threshold = 4u;

			var smxMathHelper = new ScalerMath(new ApFixedPointFormat(limbCount), threshold);

			//var a = new Smx(false, new ulong[] { 151263699, 55238551, 1 }, 2, -63, precision);
			//var b = new Smx(true, new ulong[] { 86140672, 2, 0 }, 1, -36, precision);

			var aLongs = new ulong[] {1512, 552, 1 };
			var aBigInteger = -1 * ScalerMathHelper.FromPwULongs(aLongs);
			var aRValueStg = new RValue(aBigInteger, -63, precision);

			var bLongs = new ulong[] { 8614, 2, 0 };
			var bBigInteger = ScalerMathHelper.FromPwULongs(bLongs);
			var bRValueStg = new RValue(bBigInteger, -36, precision);

			var aRValue = RNormalizer.Normalize(aRValueStg, bRValueStg, out var bRValue);

			var aSmx = smxMathHelper.CreateSmx(aRValue);
			var aStr = aSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the aSmx is {aStr}.");

			var bSmx = smxMathHelper.CreateSmx(bRValue);
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");

			var cSmx = smxMathHelper.Add(aSmx, bSmx, "Test");

			var cSmxRValue = cSmx.GetRValue();
			var cStr = cSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the cSmx is {cStr}.");

			//var nrmA = RNormalizer.Normalize(aRValue, bRValue, out var nrmB);
			var cRValue = aRValue.Add(bRValue);
			var cStrComp = RValueHelper.ConvertToString(cRValue);
			Debug.WriteLine($"The StringValue for the cRValue is {cStrComp}.");

			var numberOfMatchingDigits = RValueHelper.GetNumberOfMatchingDigits(cSmxRValue, cRValue, out var expected);
			Assert.Equal(expected, Math.Min(numberOfMatchingDigits, expected));
		}

		#endregion

		#region Support Methods

		private ScalerMath BuildTheMathHelper(int limbCount)
		{
			var result = new ScalerMath(new ApFixedPointFormat(limbCount), 4u);
			return result;
		}

		private VecMath BuildTheVecMathHelper(int limbCount, int valueCount, uint threshold)
		{
			var result = new VecMath(new ApFixedPointFormat(limbCount), valueCount, threshold);
			return result;
		}

		private FPValues CreateTestValues(Smx smx, int valueCount)
		{
			var xx = Enumerable.Repeat(smx, valueCount).ToArray();
			var result = new FPValues(xx);
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