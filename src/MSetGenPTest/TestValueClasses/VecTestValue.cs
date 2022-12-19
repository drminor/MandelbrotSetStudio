using MSetGenP;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	internal class VecTestValue
	{
		public SmxTestValue SmxTestValue { get; init; }
		public FPValues Vectors { get; init; }

		public Smx SmxValue => SmxTestValue.SmxValue;


		public VecTestValue(FPValues fPValues, VecMath vecMath)
		{
			Vectors = fPValues;	
			var bSmx = vecMath.GetSmxAtIndex(fPValues, index: 0);
			SmxTestValue = new SmxTestValue(bSmx, new ScalarMath(vecMath.ApFixedPointFormat, vecMath.Threshold));
		}

		public VecTestValue(string number, int exponent, int precision, ScalarMath scalarMath) : this("+", number, exponent, precision, scalarMath)
		{ }

		public VecTestValue(string sign, string number, int exponent, int precision, ScalarMath scalarMath)
		{
			SmxTestValue = new SmxTestValue(sign, number, exponent, precision, scalarMath);
			Vectors = CreateFPValues(SmxTestValue.SmxValue, 4);
		}

		public VecTestValue(Smx smxValue, ScalarMath scalarMath)
		{
			SmxTestValue = new SmxTestValue(smxValue, scalarMath);
			Vectors = CreateFPValues(SmxTestValue.SmxValue, 4);
		}

		public VecTestValue(Smx2C smx2CValue, ScalarMath scalarMath)
		{
			SmxTestValue = new SmxTestValue(smx2CValue, scalarMath);
			Vectors = CreateFPValues(SmxTestValue.SmxValue, 4);
		}

		public override string ToString()
		{
			return SmxTestValue.StringValue;
		}

		private static FPValues CreateFPValues(Smx smx, int valueCount)
		{
			var xx = Enumerable.Repeat(smx, valueCount).ToArray();
			var result = new FPValues(xx);
			return result;
		}

	}

}