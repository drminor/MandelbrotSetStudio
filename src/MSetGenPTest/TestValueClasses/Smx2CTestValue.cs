using MSetGenP;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	internal class Smx2CTestValue
	{
		public Smx2C Smx2CValue { get; init; }
		//public Smx SmxValue { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public Smx2CTestValue(string number, int exponent, int precision, ScalarMath2C scalarMath2C) //: this ("+", number, exponent, precision, scalarMath2C)
		{
			var bi = BigInteger.Parse(number);

			RValue = new RValue(bi, exponent, precision);

			//Smx2CValue = scalarMath2C.CreateSmx2C(RValue);
			Smx2CValue = ScalarMathHelper.CreateSmx2C(RValue, scalarMath2C.ApFixedPointFormat);
			StringValue = Smx2CValue.GetStringValue();

			//SmxValue = scalarMath2C.Convert(Smx2CValue);
		}

		public Smx2CTestValue(Smx2C smx2CValue, ScalarMath2C scalarMath2C)
		{
			Smx2CValue = smx2CValue;
			RValue = Smx2CValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();

			//SmxValue = scalarMath2C.Convert(smx2CValue);
		}

		public Smx2CTestValue(RValue rValue, VecMath vecMath)
		{
			RValue = rValue;
			Smx2CValue = ScalarMathHelper.CreateSmx2C(rValue, vecMath.ApFixedPointFormat);
			StringValue = Smx2CValue.GetStringValue(); 
		}

		//public Smx2CTestValue(Smx smxValue, ScalarMath2C scalarMath2C)
		//{
		//	SmxValue = smxValue;

		//	Smx2CValue = scalarMath2C.Convert(SmxValue);
		//	RValue = SmxValue.GetRValue();
		//	StringValue = Smx2CValue.GetStringValue();
		//}

		public string GetDiagDisplay()
		{
			//var result = ScalarMathHelper.GetDiagDisplayHexBlocked("raw result", Smx2CValue.Mantissa);
			
			var result = ScalarMathHelper.GetDiagDisplayHex("raw result", Smx2CValue.Mantissa);
			return result;
		}

		public override string ToString()
		{
			return StringValue;
		}

	}

}