using MSetGenP;
using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;
using System.Numerics;

namespace EngineTest
{
	internal class Smx2CTestValue
	{
		public Smx2C Smx2CValue { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public Smx2CTestValue(string number, int exponent, int precision, ScalarMath2C scalarMath2C)
		{
			var bi = BigInteger.Parse(number);
			RValue = new RValue(bi, exponent, precision);
			//Smx2CValue = scalarMath2C.CreateSmx2C(RValue);
			Smx2CValue = ScalarMathHelper.CreateSmx2C(RValue, scalarMath2C.ApFixedPointFormat);
			StringValue = Smx2CValue.GetStringValue();
		}

		public Smx2CTestValue(Smx2C smx2CValue, ScalarMath2C scalarMath2C)
		{
			Smx2CValue = smx2CValue;
			RValue = Smx2CValue.GetRValue();
			StringValue = RValueHelper.ConvertToString(RValue);
		}

		//public Smx2CTestValue(RValue rValue, ScalarMath2C scalarMath2C)
		//{
		//	RValue = rValue;
		//	Smx2CValue = ScalarMathHelper.CreateSmx2C(rValue, scalarMath2C.ApFixedPointFormat);
		//	StringValue = RValueHelper.ConvertToString(RValue);
		//}

		public string GetDiagDisplay()
		{
			var result = ScalarMathHelper.GetDiagDisplayHex("raw result", Smx2CValue.Mantissa);
			return result;
		}

		public override string ToString()
		{
			return StringValue;
		}

	}

}