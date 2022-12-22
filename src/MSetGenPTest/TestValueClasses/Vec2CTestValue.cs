using Microsoft.VisualBasic;
using MSetGenP;
using MSS.Types;

namespace EngineTest
{
	internal class Vec2CTestValue
	{
		public Smx2CTestValue Smx2CTestValue { get; init; }
		public FPValues Vectors { get; init; }

		public Smx2C Smx2CValue => Smx2CTestValue.Smx2CValue;
		public RValue RValue => Smx2CTestValue.RValue;

		#region Constructors

		public Vec2CTestValue(FPValues fPValues, VecMath2C vecMath2C)
		{
			Vectors = fPValues;
			var smx2C = vecMath2C.GetSmx2CAtIndex(fPValues, index: 0);
			Smx2CTestValue = new Smx2CTestValue(smx2C, BuildTheScalarMath2C(vecMath2C));
		}

		public Vec2CTestValue(string number, int exponent, int precision, VecMath2C vecMath2C) //: this ("+", number, exponent, precision, vecMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(number, exponent, precision, BuildTheScalarMath2C(vecMath2C));
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		}

		public Vec2CTestValue(Smx2C smx2CValue, VecMath2C vecMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(smx2CValue, BuildTheScalarMath2C(vecMath2C));
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		}

		public Vec2CTestValue(RValue rValue, VecMath2C vecMath2C)
		{
			//Smx2CTestValue = new Smx2CTestValue(rValue, vce)
			//(rValue smx2CValue, BuildTheScalarMath2C(vecMath2C));

			Smx2C smx2CValue = ScalarMathHelper.CreateSmx2C(rValue, vecMath2C.ApFixedPointFormat);

			Smx2CTestValue = new Smx2CTestValue(smx2CValue, BuildTheScalarMath2C(vecMath2C));
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		}

		//public Vec2CTestValue(Smx smxValue, VecMath2C vecMath2C)
		//{
		//	Smx2CTestValue = new Smx2CTestValue(smxValue, BuildTheScalarMath2C(vecMath2C));
		//	Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		//}

		#endregion

		public FPValues CreateNewFPValues()
		{
			var result = new FPValues(Vectors.LimbCount, Vectors.Length);
			return result;
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


		private ScalarMath2C BuildTheScalarMath2C(VecMath2C vecMath2C)
		{
			return new ScalarMath2C(vecMath2C.ApFixedPointFormat, vecMath2C.Threshold);
		}


	}

}