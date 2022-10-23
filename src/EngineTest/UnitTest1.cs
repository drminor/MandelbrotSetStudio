using MSetGenP;
using MSS.Common;
using MSS.Types;
using System.Numerics;

namespace EngineTest
{
	public class UnitTest1
	{
		[Fact]
		public void FullMultiply_Returns_Correct_Value()
		{
			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = SmxMathHelper.Multiply(aMantissa, aMantissa);
			var bBigInteger = SmxMathHelper.FromPwULongs(bMantissa);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			Assert.Equal(bBigInteger, bCompBigInteger);
		}

		[Fact]
		public void FillMsb_Returns_Correct_Value()
		{
			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017");
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = SmxMathHelper.Multiply(aMantissa, aMantissa);
			var b1Mantissa = SmxMathHelper.TrimLeadingZeros(bMantissa);
			var t1Longs = SmxMathHelper.FillMsb(b1Mantissa, out var shiftAmount);
			var t1BigInteger = SmxMathHelper.FromPwULongs(t1Longs);
			var t2BigInteger = t1BigInteger / BigInteger.Pow(2, shiftAmount);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			Assert.Equal(t2BigInteger, bCompBigInteger);
		}

		[Fact]
		public void Round_Returns_Correct_Value()
		{
			var aBigInteger = BigInteger.Parse("-126445453255269018635038690902017"); // 0.0000000000353482168348864539511122006373007661 (or possibly: 0.000000000035348216834895204420066149556547602)
			var aMantissa = SmxMathHelper.ToPwULongs(aBigInteger);

			var bMantissa = SmxMathHelper.Multiply(aMantissa, aMantissa);
			var b1Mantissa = SmxMathHelper.TrimLeadingZeros(bMantissa);
			var t1Longs = SmxMathHelper.FillMsb(b1Mantissa, out var shiftAmount);

			// Discard 2 digits from the LSB end. (Divide by 2^64)
			var t2Longs = new ulong[t1Longs.Length - 2];
			Array.Copy(t1Longs, 2, t2Longs, 0, t2Longs.Length);

			var t1BigInteger = SmxMathHelper.FromPwULongs(t2Longs);
			var t2BigInteger = t1BigInteger / BigInteger.Pow(2, shiftAmount);

			var bCompBigInteger = BigInteger.Multiply(aBigInteger, aBigInteger);
			var b2CompBigInteger = bCompBigInteger / BigInteger.Pow(2, 64);

			Assert.Equal(t2BigInteger, b2CompBigInteger);
		}

		[Fact]
		public void MultiplyTwoRValues()
		{
			//var aRvalue = new RValue(new BigInteger(-414219082), -36, 53); // -6.02768096723593793141715568851e-3
			//var bRvalue = new RValue(new BigInteger(67781838), -36, 53); // 9.8635556059889517056815666506964e-4

			var aRValue = new RValue(new BigInteger(-27797772040142849), -62, 53); // -6.02768096723593793141715568851e-3
			var bRValue = new RValue(new BigInteger(4548762148012033), -62, 53); // 9.8635556059889517056815666506964e-4

			var a = new Smx(aRValue);
			var b = new Smx(bRValue);
			var c = SmxMathHelper.Multiply(a, b);

			var cSmxRValue = c.GetRValue();
			var s1 = RValueHelper.ConvertToString(cSmxRValue);

			var cRValue = aRValue.Mul(bRValue);
			var s2 = RValueHelper.ConvertToString(cRValue);

			var areClose = SmxMathHelper.AreClose(cSmxRValue, cRValue);
			Assert.True(areClose);
		}

		[Fact]
		public void SquareAnRValue()
		{
			var aRValue = new RValue(BigInteger.Parse("-126445453255269018635038690902017"), -124, 53); // -0.0000059454366395492942314714083927915125745469
			var s0 = RValueHelper.ConvertToString(aRValue);

			var a = new Smx(aRValue);

			var b = SmxMathHelper.Square(a);
			var bSmxRValue = b.GetRValue();
			var s1 = RValueHelper.ConvertToString(bSmxRValue);

			var bRValue = aRValue.Square();
			var s2 = RValueHelper.ConvertToString(bRValue);

			var areClose = SmxMathHelper.AreClose(bSmxRValue, bRValue);
			Assert.True(areClose);
		}

	}

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