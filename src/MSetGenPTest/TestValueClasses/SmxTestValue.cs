using Microsoft.VisualBasic;
using MSetGenP;
using MSS.Types;
using System.Diagnostics;
using System.Numerics;

namespace EngineTest
{
	internal class SmxTestValue
	{
		//public BigInteger BiValue { get; init; }

		public Smx2C Smx2CValue { get; init; }
		public Smx SmxValue { get; init; }
		public RValue RValue { get; init; }
		public string StringValue { get; init; }

		public SmxTestValue(string number, int exponent, int precision, ScalarMath scalarMath) : this("+", number, exponent, precision, scalarMath)
		{ }

		public SmxTestValue(string sign, string number, int exponent, int precision, ScalarMath smxMathHelper)
		{
			var bi = BigInteger.Parse(number);
			if (sign == "-") { bi = BigInteger.Negate(bi); }

			RValue = new RValue(bi, exponent, precision);

			SmxValue = ScalarMathHelper.CreateSmx(RValue, smxMathHelper.ApFixedPointFormat);
			Smx2CValue = smxMathHelper.Convert(SmxValue);

			StringValue = Smx2CValue.GetStringValue();
		}

		public SmxTestValue(Smx smxValue, ScalarMath smxMathHelper)
		{
			SmxValue = smxValue;

			Smx2CValue = smxMathHelper.Convert(SmxValue);
			RValue = SmxValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();
		}

		public SmxTestValue(RValue rValue, VecMath vecMath) : this(ScalarMathHelper.CreateSmx(rValue, vecMath.ApFixedPointFormat),
				new ScalarMath(vecMath.ApFixedPointFormat, vecMath.Threshold))



		{ }

		//this(ScalarMathHelper.CreateSmx(rValue, vecMath.TargetExponent, vecMath.LimbCount, vecMath.BitsBeforeBP), vecMath)

		public SmxTestValue(Smx2C smx2CValue, ScalarMath smxMathHelper)
		{
			Smx2CValue = smx2CValue;

			SmxValue = smxMathHelper.Convert(Smx2CValue);

			RValue = SmxValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();	
		}

		public override string ToString()
		{
			return StringValue;
		}
	}

}