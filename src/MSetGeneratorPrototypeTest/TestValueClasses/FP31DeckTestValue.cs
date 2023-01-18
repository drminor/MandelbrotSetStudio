using MSetGeneratorPrototype;
using MSS.Common.APValues;
using MSS.Common.SmxVals;
using MSS.Types;

namespace EngineTest
{
    internal class FP31DeckTestVal
	{
		public FP31ValTestValue FP31ValTestVal { get; init; }
		public FP31Deck Vectors { get; init; }

		public FP31Val FP31Val => FP31ValTestVal.FP31Val;
		public RValue RValue => FP31ValTestVal.RValue;

		#region Constructors

		public FP31DeckTestVal(FP31Deck fp31Deck, VecMath9 vecMath9)
		{
			Vectors = fp31Deck;
			var smx2C = GetFP31ValAtIndex(fp31Deck, index: 0, vecMath9.TargetExponent, vecMath9.BitsBeforeBP);
			FP31ValTestVal = new FP31ValTestValue(smx2C);
		}

		public FP31DeckTestVal(string number, int exponent, int precision, VecMath9 vecMath9)
		{
			FP31ValTestVal = new FP31ValTestValue(number, exponent, precision, BuildTheScalarMath(vecMath9));
			Vectors = CreateFP31Deck(FP31ValTestVal.FP31Val, vecMath9.ValueCount);
		}

		public FP31DeckTestVal(FP31Val fP31Val, VecMath9 vecMath9)
		{
			FP31ValTestVal = new FP31ValTestValue(fP31Val);
			Vectors = CreateFP31Deck(FP31ValTestVal.FP31Val, vecMath9.ValueCount);
		}

		//public Vec2CTestValue(RValue rValue, VecMath2C vecMath2C)
		//{
		//	Smx2C smx2CValue = ScalarMathHelper.CreateSmx2C(rValue, vecMath2C.ApFixedPointFormat);

		//	Smx2CTestValue = new Smx2CTestValue(smx2CValue, BuildTheScalarMath2C(vecMath2C));
		//	Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		//}

		#endregion

		private FP31Deck CreateFP31Deck(FP31Val fP31Val, int valueCount)
		{
			var elements = new List<FP31Val>();

			for (var i = 0; i < valueCount; i++)
			{
				elements.Add(fP31Val.Clone());
			}

			var result = new FP31Deck(elements.ToArray());
			return result;
		}

		private FP31Val GetFP31ValAtIndex(FP31Deck vectors, int index, int targetExponent, byte bitsBeforeBP, int precision = RMapConstants.DEFAULT_PRECISION)
		{
			var mantissa = vectors.Mantissas.Select(x => x[index]).ToArray();
			var result = new FP31Val(mantissa, targetExponent, bitsBeforeBP, precision);

			return result;
		}

		public FP31Deck CreateNewFP31Deck()
		{
			var result = new FP31Deck(Vectors.LimbCount, Vectors.ValueCount);
			return result;
		}

		public string GetDiagDisplay()
		{
			var result = FP31ValHelper.GetDiagDisplayHex("raw result", FP31Val.Mantissa);
			return result;
		}

		public override string ToString()
		{
			return FP31ValTestVal.StringValue;
		}

		private ScalarMath9 BuildTheScalarMath(VecMath9 vecMath9)
		{
			return new ScalarMath9(vecMath9.ApFixedPointFormat);
		}

	}
}