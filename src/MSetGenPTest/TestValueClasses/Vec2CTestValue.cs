using MSetGenP;

namespace EngineTest
{
	internal class Vec2CTestValue
	{
		public Smx2CTestValue Smx2CTestValue { get; init; }
		public FPValues Vectors { get; init; }

		public Smx2C Smx2CValue => Smx2CTestValue.Smx2CValue;

		public Vec2CTestValue(FPValues fPValues, VecMath2C vecMath2C)
		{
			Vectors = fPValues;
			var smx2C = vecMath2C.GetSmx2CAtIndex(fPValues, index: 0);
			Smx2CTestValue = new Smx2CTestValue(smx2C, new ScalarMath2C(vecMath2C.ApFixedPointFormat, vecMath2C.Threshold));
		}

		public Vec2CTestValue(string number, int exponent, int precision, ScalarMath2C scalarMath2C) : this ("+", number, exponent, precision, scalarMath2C)
		{ }

		public Vec2CTestValue(string sign, string number, int exponent, int precision, ScalarMath2C scalarMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(sign, number, exponent, precision, scalarMath2C);
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, 4);
		}

		public Vec2CTestValue(Smx2C smx2CValue, ScalarMath2C scalarMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(smx2CValue, scalarMath2C);
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, 4);
		}

		public Vec2CTestValue(Smx smxValue, ScalarMath2C scalarMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(smxValue, scalarMath2C);
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, 4);
		}

		public override string ToString()
		{
			return Smx2CTestValue.StringValue;
		}

		private static FPValues CreateFPValues(Smx2C smx2C, int valueCount)
		{
			var xx = Enumerable.Repeat(smx2C, valueCount).ToArray();
			var result = new FPValues(xx);
			return result;
		}


	}

}