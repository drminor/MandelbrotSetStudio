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

		public Smx2CTestValue(string number, int exponent, int precision, ScalarMath2C scalarMath2C) : this ("+", number, exponent, precision, scalarMath2C)
		{ }

		public Smx2CTestValue(string sign, string number, int exponent, int precision, ScalarMath2C scalarMath2C)
		{
			var bi = BigInteger.Parse(number);
			if (sign == "-") { bi = BigInteger.Negate(bi); }
			RValue = new RValue(bi, exponent, precision);

			Smx2CValue = scalarMath2C.CreateSmx2C(RValue);
			SmxValue = scalarMath2C.Convert(Smx2CValue);

			StringValue = Smx2CValue.GetStringValue();
		}

		public Smx2CTestValue(Smx2C smx2CValue, ScalarMath2C scalarMath2C)
		{
			Smx2CValue = smx2CValue;
			SmxValue = scalarMath2C.Convert(smx2CValue);
			RValue = SmxValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();	
		}

		public Smx2CTestValue(RValue rValue, VecMath vecMath) : 
			this(ScalarMathHelper.CreateSmx(rValue, vecMath.ApFixedPointFormat),
				new ScalarMath2C(vecMath.ApFixedPointFormat, vecMath.Threshold))
		{ }

		public Smx2CTestValue(Smx smxValue, ScalarMath2C scalarMath2C)
		{
			SmxValue = smxValue;

			Smx2CValue = scalarMath2C.Convert(SmxValue);
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