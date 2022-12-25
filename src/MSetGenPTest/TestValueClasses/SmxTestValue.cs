﻿using MSetGenP;
using MSS.Types;
using System.Numerics;

namespace EngineTest
{
	internal class SmxTestValue
	{
		public Smx SmxValue { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public SmxTestValue(string number, int exponent, int precision, ScalarMath scalarMath)
		{
			var bi = BigInteger.Parse(number);

			RValue = new RValue(bi, exponent, precision);
			SmxValue = ScalarMathHelper.CreateSmx(RValue, scalarMath.ApFixedPointFormat);
			StringValue = SmxValue.GetStringValue();
		}

		public SmxTestValue(Smx smxValue, ScalarMath scalarMath)
		{
			SmxValue = smxValue;
			RValue = SmxValue.GetRValue();
			StringValue = SmxValue.GetStringValue();
		}

		public SmxTestValue(RValue rValue, VecMath vecMath) : this(ScalarMathHelper.CreateSmx(rValue, vecMath.ApFixedPointFormat),
				new ScalarMath(vecMath.ApFixedPointFormat, vecMath.Threshold))
		{ }

		public SmxTestValue(Smx2C smx2CValue, ScalarMath scalarMath)
		{
			SmxValue =  scalarMath.Convert(smx2CValue);
			RValue = SmxValue.GetRValue();
			StringValue = SmxValue.GetStringValue();	
		}

		public override string ToString()
		{
			return StringValue;
		}
	}

}