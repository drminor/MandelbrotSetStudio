using MSS.Types.Base;
using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types
{
	public class RSize : Size<BigInteger>, IBigRatShape
	{
		public int Exponent { get; init; }

		public BigInteger WidthNumerator => base.Width;
		public BigInteger HeightNumerator => base.Height;

		public new RValue Width => new RValue(WidthNumerator, Exponent);
		public new RValue Height => new RValue(HeightNumerator, Exponent);

		public RSize() : this(0, 0, 0)
		{ }

		public RSize(BigInteger[] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		// Square from single value
		public RSize(RValue extent) : this(extent.Value, extent.Value, extent.Exponent)
		{ }

		public RSize(BigInteger width, BigInteger height, int exponent) : base(width, height)
		{
			Exponent = exponent;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public new RSize Clone()
		{
			return Reducer.Reduce(this);
		}

		public override string? ToString()
		{
			var result = BigIntegerHelper.GetDisplay(Reducer.Reduce(this));
			return result;
		}

		public RSize Scale(SizeInt factor)
		{
			return new RSize(WidthNumerator * factor.Width, HeightNumerator * factor.Height, Exponent);
		}

		public RSize Scale(PointInt factor)
		{
			return new RSize(WidthNumerator * factor.X, HeightNumerator * factor.Y, Exponent);
		}

		public RSize Scale(SizeDbl factor)
		{
			//var nW = (long)((long)WidthNumerator * factor.Width);
			//var nH = (long)((long)HeightNumerator * factor.Height);
			//return new RSize(nW, nH, Exponent);

			var w = BigIntegerHelper.ConvertToDouble(Width);
			var h = BigIntegerHelper.ConvertToDouble(Height);

			var result = new RSize
				(
					new BigInteger(w * factor.Width),
					new BigInteger(h * factor.Height),
					Exponent
				);

			return result;
		}

		//private static RRectangle Scale(RectangleInt factor)
		//{
		//	var result = new RRectangle(area.X1 * factor.Width, area.X2 * factor.Width, area.Y1 * factor.Height, area.Y2 * factor.Height, factor.Exponent);
		//	return result;
		//}

		//// TODO: FIX BUG
		//public RSize Scale(RSize factor)
		//{
		//	return factor.Exponent != Exponent
		//		? throw new InvalidOperationException($"Cannot InvScale a RSize with Exponent: {Exponent} using an RSize with Exponent: {factor.Exponent}.")
		//		: new RSize(Width * factor.Width, Height * factor.Height, Exponent);
		//}

		//// TODO: FIX BUG
		//public RSize InvScale(RSize factor)
		//{
		//	if (factor.Exponent != Exponent)
		//	{
		//		throw new InvalidOperationException($"Cannot InvScale a RSize with Exponent: {Exponent} using an RSize with Exponent: {factor.Exponent}.");
		//	}

		//	var w = Width * (BigInteger) Math.Pow(2, -1 * factor.Exponent);
		//	w /= factor.Width;

		//	var h = Height * (BigInteger)Math.Pow(2, -1 * factor.Exponent);
		//	h /= factor.Height;

		//	return new RSize(w, h, Exponent);
		//}

		public RSize Translate(RPoint amount)
		{
			return amount.Exponent != Exponent
                ?                throw new InvalidOperationException($"Cannot translate an RSize with Exponent: {Exponent} using an RPoint with Exponent: {amount.Exponent}.")
				: new RSize(WidthNumerator + amount.X, HeightNumerator + amount.Y, Exponent);
		}

		public RSize Translate(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate an RSize with Exponent: {Exponent} using an RSize with Exponent: {amount.Exponent}.")
				: new RSize(base.Width + amount.WidthNumerator, HeightNumerator + amount.HeightNumerator, Exponent);
		}

		public static explicit operator RSize(int value)
		{
			return new RSize(value, value, 0);
		}

	}
}
