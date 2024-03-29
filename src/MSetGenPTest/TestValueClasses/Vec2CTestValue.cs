﻿using MSetGenP;
using MSetGenP.Types;
using MSS.Types;
using System.Diagnostics;
using static MongoDB.Driver.WriteConcern;

namespace MSetGenPTest
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

			var sign = ScalarMathHelper.GetSign(smx2C.Mantissa);

			var isSignBitSet = !sign;

			if (ScalarMathHelper.UpdateTheReserveBit(smx2C.Mantissa, isSignBitSet))
			{
				Debug.WriteLine("WARNING: The ReserveBit did not match the sign bit when fetching a value from an FPValues (Vec2CTestValue constructor.");
			}

			Smx2CTestValue = new Smx2CTestValue(smx2C, BuildTheScalarMath2C(vecMath2C));
		}

		public Vec2CTestValue(string number, int exponent, int precision, VecMath2C vecMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(number, exponent, precision, BuildTheScalarMath2C(vecMath2C));
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		}

		public Vec2CTestValue(Smx2C smx2CValue, VecMath2C vecMath2C)
		{
			Smx2CTestValue = new Smx2CTestValue(smx2CValue, BuildTheScalarMath2C(vecMath2C));
			Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		}

		//public Vec2CTestValue(RValue rValue, VecMath2C vecMath2C)
		//{
		//	Smx2C smx2CValue = ScalarMathHelper.CreateSmx2C(rValue, vecMath2C.ApFixedPointFormat);

		//	Smx2CTestValue = new Smx2CTestValue(smx2CValue, BuildTheScalarMath2C(vecMath2C));
		//	Vectors = CreateFPValues(Smx2CTestValue.Smx2CValue, vecMath2C.ValueCount);
		//}

		#endregion

		public FPValues CreateNewFPValues()
		{
			var result = new FPValues(Vectors.LimbCount, Vectors.Length);
			return result;
		}

		private static FPValues CreateFPValues(Smx2C smx2C, int valueCount)
		{
			var elements = new List<Smx2C>();

			for(var i = 0; i < valueCount; i++)
			{
				elements.Add(smx2C.Clone());
			}

			var result = new FPValues(elements.ToArray());
			return result;
		}

		private ScalarMath2C BuildTheScalarMath2C(VecMath2C vecMath2C)
		{
			return new ScalarMath2C(vecMath2C.ApFixedPointFormat, vecMath2C.Threshold);
		}

		public string GetDiagDisplay()
		{
			var result = ScalarMathHelper.GetDiagDisplayHex("raw result", Smx2CValue.Mantissa);
			return result;
		}

		public override string ToString()
		{
			return Smx2CTestValue.StringValue;
		}

	}
}