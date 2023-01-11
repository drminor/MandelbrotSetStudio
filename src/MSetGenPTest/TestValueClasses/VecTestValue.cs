using MSetGenP;
using MSS.Common.SmxVals;
using MSS.Types;

namespace EngineTest
{
	internal class VecTestValue
	{
		public SmxTestValue SmxTestValue { get; init; }
		public FPValues Vectors { get; init; }

		public Smx SmxValue => SmxTestValue.SmxValue;
		public RValue RValue => SmxTestValue.RValue;


		#region Constructors

		public VecTestValue(FPValues fPValues, VecMath vecMath)
		{
			Vectors = fPValues;	
			var bSmx = vecMath.GetSmxAtIndex(fPValues, index: 0);
			SmxTestValue = new SmxTestValue(bSmx, BuildTheScalarMath(vecMath));

			var sb = bSmx.GetStringValue();

			var st = SmxTestValue.StringValue;
		}

		public VecTestValue(string number, int exponent, int precision, VecMath vecMath)
		{
			SmxTestValue = new SmxTestValue(number, exponent, precision, BuildTheScalarMath(vecMath));
			Vectors = CreateFPValues(SmxTestValue.SmxValue, vecMath.ValueCount);
		}

		public VecTestValue(Smx smxValue, VecMath vecMath)
		{
			SmxTestValue = new SmxTestValue(smxValue, BuildTheScalarMath(vecMath));
			Vectors = CreateFPValues(SmxTestValue.SmxValue, vecMath.ValueCount);
		}

		public VecTestValue(RValue rValue, VecMath vecMath)
		{
			var smx = ScalarMathHelper.CreateSmx(rValue, vecMath.ApFixedPointFormat);
			SmxTestValue = new SmxTestValue(smx, BuildTheScalarMath(vecMath));

			Vectors = CreateFPValues(SmxTestValue.SmxValue, vecMath.ValueCount);
		}

		#endregion

		public FPValues CreateNewFPValues()
		{
			var result = new FPValues(Vectors.LimbCount, Vectors.Length);
			return result;
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

		private ScalarMath BuildTheScalarMath(VecMath vecMath)
		{
			return new ScalarMath(vecMath.ApFixedPointFormat, vecMath.Threshold);
		}

	}

}