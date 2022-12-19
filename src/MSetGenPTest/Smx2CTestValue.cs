using MSetGenP;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	internal class Smx2CTestValue
	{
		//public BigInteger BiValue { get; init; }

		public Smx2C Smx2CValue { get; init; }
		public Smx SmxValue { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public Smx2CTestValue(string number, int exponent, int precision, FPMathHelper fPMathHelper)
		{
			var bi = BigInteger.Parse(number);
			RValue = new RValue(bi, exponent, precision);

			//SmxValue = SmxHelper.CreateSmx(RValue, fPMathHelper.TargetExponent, fPMathHelper.LimbCount, fPMathHelper.BitsBeforeBP);
			//Smx2CValue = fPMathHelper.Convert(SmxValue);

			Smx2CValue = fPMathHelper.Create(RValue);
			SmxValue = fPMathHelper.Convert(Smx2CValue);

			StringValue = Smx2CValue.GetStringValue();
		}

		public Smx2CTestValue(Smx2C smx2CValue, FPMathHelper fPMathHelper)
		{
			Smx2CValue = smx2CValue;
			SmxValue = fPMathHelper.Convert(smx2CValue);
			RValue = SmxValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();	
		}

		public override string ToString()
		{
			return StringValue;
		}


		/*

			-- Starting with a 'std' val
			var aBigInteger = BigInteger.Parse("2147483648");
			var aRValue = new RValue(aBigInteger, -33, precision); // 0.25

			var aSmx = SmxHelper.CreateSmx(aRValue, targetExponent, limbCount, bitsBeforeBP);
			var aSmx2C = fpMathHelper.Convert(aSmx);


			-- Starting with a 2C val
			var bSmx2C = fpMathHelper.Square(aSmx2C);
			var bSmx = fpMathHelper.Convert(bSmx2C);

			var bSmxRValue = bSmx.GetRValue();
			var bStr = bSmx.GetStringValue();
			Debug.WriteLine($"The StringValue for the bSmx is {bStr}.");


		*/
	}

}