using MSetGeneratorPrototype;
using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using System.Numerics;

namespace EngineTest
{
	internal class FP31ValTestValue
	{
		public FP31Val FP31Val { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public FP31ValTestValue(string number, int exponent, int precision, FP31ScalarMath scalarMath9)
		{
			var bi = BigInteger.Parse(number);
			RValue = new RValue(bi, exponent, precision);
			FP31Val = FP31ValHelper.CreateFP31Val(RValue, scalarMath9.ApFixedPointFormat);
			StringValue = RValueHelper.ConvertToString(RValue);
		}

		public FP31ValTestValue(FP31Val fp31Val)
		{
			FP31Val = fp31Val;
			RValue = FP31Val.GetRValue();
			StringValue = RValueHelper.ConvertToString(RValue);
		}

		public string GetDiagDisplay()
		{
			var result = FP31ValHelper.GetDiagDisplayHex("raw result", FP31Val.Mantissa);
			return result;
		}

		public override string ToString()
		{
			return StringValue;
		}

	}

}