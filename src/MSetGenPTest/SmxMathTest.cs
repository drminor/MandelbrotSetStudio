using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	public class SmxMathTest
	{
		[Fact]
		public void FullMultiply_Returns_Correct_Value()
		{
			var smxMathHelper = new SmxMathHelper(-55);

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = smxMathHelper.Multiply(aMantissa, aMantissa);
			var bBigInteger = SmxMathHelper.FromPwULongs(bMantissa);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			Assert.Equal(bBigInteger, bCompBigInteger);
		}

		//[Fact]
		//public void FillMsb_Returns_Correct_Value()
		//{
		//	var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
		//	var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

		//	var bMantissa = SmxMathHelper.Multiply(aMantissa, aMantissa);

		//	var b1Mantissa = SmxMathHelper.TrimLeadingZeros(bMantissa);
		//	var t1Longs = SmxMathHelper.FillMsb(b1Mantissa, out var shiftAmount);

		//	var t1BigInteger = SmxMathHelper.FromPwULongs(t1Longs);
		//	var t2BigInteger = t1BigInteger / BigInteger.Pow(2, shiftAmount);

		//	var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
		//	Assert.Equal(t2BigInteger, bCompBigInteger);
		//}

		[Fact]
		public void NormalizeFPV_Returns_Correct_Value()
		{
			var smxMathHelper = new SmxMathHelper(-55);

			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 0.0000000000353482168348864539511122006373007661 (or possibly: 0.000000000035348216834895204420066149556547602)
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bRawMantissa = smxMathHelper.Multiply(aMantissa, aMantissa);
			var bMantissa = smxMathHelper.PropagateCarries(bRawMantissa, out var indexOfLastNonZeroLimb);
			var nrmBMantissa = smxMathHelper.NormalizeFPV(bMantissa, indexOfLastNonZeroLimb, -128, 55, out var nrmExponent);

			// Discard 2 digits from the LSB end. (Divide by 2^64)
			//var t2Longs = new ulong[t1Longs.Length - 2];
			//Array.Copy(t1Longs, 2, t2Longs, 0, t2Longs.Length);

			var t1BigInteger = SmxMathHelper.FromPwULongs(nrmBMantissa);
			var t2BigInteger = t1BigInteger / BigInteger.Pow(2, nrmExponent);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			var b2CompBigInteger = bCompBigInteger / BigInteger.Pow(2, 172);

			Assert.Equal(t2BigInteger, b2CompBigInteger);
		}

		[Fact]
		public void MultiplyTwoRValues()
		{
			//var aRvalue = new RValue(new BigInteger(-414219082), -36, 53); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, 53); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(-27797772040142849), -62, 53); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, 53); // 9.8635556059889517056815666506964e-4

			var targetExponent = -62 - 20;
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var a = new Smx(aRValue);
			var b = new Smx(bRValue);
			var c = smxMathHelper.Multiply(a, b);

			var cSmxRValue = c.GetRValue();
			var s1 = RValueHelper.ConvertToString(cSmxRValue);

			var cRValue = aRValue.Mul(bRValue);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var areClose = RValueHelper.AreClose(cSmxRValue, cRValue);
			Assert.True(areClose);
		}

		[Fact]
		public void SquareAnRValue()
		{
			var aRValue = new RValue(BigInteger.Parse("-126445453255269018635038690902017"), -124, 53); // -0.0000059454366395492942314714083927915125745469

			var targetExponent = -4 * 32; // -128
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var a = new Smx(aRValue);
			var s0 = RValueHelper.ConvertToString(aRValue);

			var b = smxMathHelper.Square(a);
			var bSmxRValue = b.GetRValue();
			var s1 = RValueHelper.ConvertToString(bSmxRValue);

			var bRValue = aRValue.Square();
			var s2 = RValueHelper.ConvertToString(bRValue);

			var bRNrm = RNormalizer.Normalize(bRValue, bSmxRValue, out var bSmxRNrm);
			var bRSmx = new Smx(bRNrm);

			Debug.WriteLine(SmxMathHelper.GetDiagDisplay("b", b.Mantissa));
			Debug.WriteLine(SmxMathHelper.GetDiagDisplay("bRSmx", bRSmx.Mantissa));

			var areClose = RValueHelper.AreClose(bSmxRValue, bRValue);
			Assert.True(areClose);
		}

		[Fact]
		public void SquareAnRValueSm()
		{
			var aRValue = new RValue(BigInteger.Parse("-12644545325526901863503869090"), -124, 53); // 5.9454366395492942314714087866438e-10 -- Windows Calc: -5.9454366395492942314714e-10

			var targetExponent = -4 * 32; // -128
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var s0 = RValueHelper.ConvertToString(aRValue); // -5.94543663954929423147140869492508049e-10

			var a = new Smx(aRValue);

			var a2Mantissa = smxMathHelper.Square(a.Mantissa);
			var a2 = new Smx(true, a2Mantissa, -248, 53);
			var a2SmxRvalue = a2.GetRValue();
			var sA2 = a2.GetStringValue();

			var a3Mantissa = smxMathHelper.PropagateCarries(a2Mantissa, out var indexOfLastNonZeroLimb);
			var a3 = new Smx(true, a3Mantissa, -248, 53);
			var a3SmxRvalue = a3.GetRValue();
			var sA3 = a3.GetStringValue();

			var b = smxMathHelper.Square(a);                            //3.5348216834895204420064645071512155149938836924682889e-19 -- Windows Calc: 3.5348216834895e-19 
			var bSmxRValue = b.GetRValue();
			var s1 = b.GetStringValue();

			var bRValue = aRValue.Square();
			var s2 = RValueHelper.ConvertToString(bRValue);

			var areClose = RValueHelper.AreClose(bSmxRValue, bRValue); //3.5348216834895203909277564e-19
			Assert.True(areClose);
		}

		[Fact]
		public void TestForceExp()
		{
			var a = new Smx(true, new ulong[] { 353384068, 3154753262, 3994887299, 3390983361, 1473799878, 109397432 }, -248, 55);

			var targetExponent = -4 * 32; // -128
			//targetExponent = -192;
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var aRValue = a.GetRValue();
			var s0 = a.GetStringValue(); // -5.94543663954929423147140869492508049e-10

			var nrmMantissa = smxMathHelper.ForceExp(a.Mantissa, 5, -248, out var nrmExponent);

			//nrmMantissa[0] = 508346952;

			var aNrm = new Smx(true, nrmMantissa, nrmExponent, 55);
			var aNrmRValue = aNrm.GetRValue();
			var s1 = aNrm.GetStringValue();

			var areClose = RValueHelper.AreClose(aNrmRValue, aRValue);
			Assert.True(areClose);
		}

		[Fact]
		public void AddTwoRValues()
		{
			//var aRvalue = new RValue(new BigInteger(-414219082), -36, 53); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, 53); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(27797772040142849), -62, 53); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, 53); // 9.8635556059889517056815666506964e-4

			var targetExponent = -62 - 20;
			var smxMathHelper = new SmxMathHelper(targetExponent);

			var a = new Smx(aRValue);
			var b = new Smx(bRValue);

			var aSa = smxMathHelper.Convert(a);
			var bSa = smxMathHelper.Convert(b);
 
			var cSa = smxMathHelper.Add(aSa, bSa, "Test");

			var c = smxMathHelper.Convert(cSa);
			var cSmxRValue = c.GetRValue();
			var s1 = RValueHelper.ConvertToString(cSmxRValue);

			var cRValue = aRValue.Add(bRValue);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var areClose = RValueHelper.AreClose(cSmxRValue, cRValue);
			Assert.True(areClose);
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
			var bSa = new SmxSa(true, new ulong[] { 86140672, 2 }, 1, -36, 55);

			var smxMathHelper = new SmxMathHelper(RMapConstants.DEFAULT_TARGET_EXPONENT);

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

			var areClose = RValueHelper.AreClose(cSmxRValue, cRValue);
			Assert.True(areClose);
		}

		[Fact]
		public void NormalizeFPV_Via_Square()
		{
			var smxMathHelper = new SmxMathHelper(-170);

			var a = new Smx(false, new ulong[] { 4155170372, 1433657343, 4294967295, 566493183 }, -171, 55);

			var aRValue = a.GetRValue();
			var s0 = RValueHelper.ConvertToString(aRValue);

			var b = smxMathHelper.Square(a);
			var bSmxRValue = b.GetRValue();
			var s1 = RValueHelper.ConvertToString(bSmxRValue);

			var bRValue = aRValue.Square();
			var s2 = RValueHelper.ConvertToString(bRValue);

			var areClose = RValueHelper.AreClose(bSmxRValue, bRValue);
			Assert.True(areClose);
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