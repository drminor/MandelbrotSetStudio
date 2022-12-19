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

		public SmxTestValue(string number, int exponent, int precision, ScalerMath smxMathHelper)
		{
			var bi = BigInteger.Parse(number);
			RValue = new RValue(bi, exponent, precision);

			SmxValue = ScalerMathHelper.CreateSmx(RValue, smxMathHelper.TargetExponent, smxMathHelper.LimbCount, smxMathHelper.BitsBeforeBP);
			Smx2CValue = smxMathHelper.Convert(SmxValue);

			StringValue = Smx2CValue.GetStringValue();
		}

		public SmxTestValue(Smx smxValue, ScalerMath smxMathHelper)
		{
			SmxValue = smxValue;

			Smx2CValue = smxMathHelper.Convert(SmxValue);
			RValue = SmxValue.GetRValue();
			StringValue = Smx2CValue.GetStringValue();
		}

		public SmxTestValue(Smx2C smx2CValue, ScalerMath smxMathHelper)
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