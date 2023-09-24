using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class RSize : IBigRatShape, IEquatable<RSize>, IEqualityComparer<RSize>
	{
		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public RSize() : this(0, 0, 0)
		{ }

		public RSize(BigInteger[] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		// Square from single value
		public RSize(RValue extent) : this(extent.Value, extent.Value, extent.Exponent)
		{ }

		public RSize(BigInteger width, BigInteger height, int exponent)
		{
			Values = new BigInteger[] { width, height };
			Exponent = exponent;
		}

		public BigInteger WidthNumerator => Values[0];

		public BigInteger HeightNumerator => Values[1];

		public RValue Width => new(WidthNumerator, Exponent);
		public RValue Height => new(HeightNumerator, Exponent);


		public RSize Scale(SizeInt factor)
		{
			return new RSize(WidthNumerator * factor.Width, HeightNumerator * factor.Height, Exponent);
		}

		public RSize Scale(PointInt factor)
		{
			return new RSize(WidthNumerator * factor.X, HeightNumerator * factor.Y, Exponent);
		}

		public RSize Scale(VectorInt factor)
		{
			return new RSize(WidthNumerator * factor.X, HeightNumerator * factor.Y, Exponent);
		}

		public RVector Scale(BigVector factor)
		{
			return new RVector(WidthNumerator * factor.X, HeightNumerator * factor.Y, Exponent);
		}

		public RSize ScaleByHalf()
		{
			return new RSize(Values[0], Values[1], Exponent - 1);
		}

		public RSize DivideBy2()
		{
			return new RSize(Values[0] / 2, Values[1] / 2, Exponent);
		}

		public RSize Invert()
		{
			return new RSize(Values[0] * -1, Values[1] * -1, Exponent);
		}

		/// <summary>
		/// Returns the distance from this point to the given point.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public RSize Diff(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot find the diff from a RSize with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
				: new RSize(WidthNumerator - amount.WidthNumerator, HeightNumerator - amount.HeightNumerator, amount.Exponent);
		}

		//// TODO rewite RSize.Scale(SizeDbl)
		//public RSize Scale(SizeDbl factor)
		//{
		//	var w = BigIntegerHelper.ConvertToDouble(Width);
		//	var h = BigIntegerHelper.ConvertToDouble(Height);

		//	var result = new RSize
		//		(
		//			new BigInteger(w * factor.Width),
		//			new BigInteger(h * factor.Height),
		//			Exponent
		//		);

		//	return result;
		//}

		//public RSize Translate(RPoint amount)
		//{
		//	return amount.Exponent != Exponent
		//		? throw new InvalidOperationException($"Cannot translate an RSize with Exponent: {Exponent} using an RPoint with Exponent: {amount.Exponent}.")
		//		: new RSize(WidthNumerator + amount.XNumerator, HeightNumerator + amount.YNumerator, amount.Exponent);
		//}

		public RSize Translate(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate an RSize with Exponent: {Exponent} using an RSize with Exponent: {amount.Exponent}.")
				: new RSize(WidthNumerator + amount.WidthNumerator, HeightNumerator + amount.HeightNumerator, amount.Exponent);
		}

		//public RSize Scale(RSize factor)
		//{
		//	return factor.Exponent != Exponent
		//		? throw new InvalidOperationException($"Cannot InvScale a RSize with Exponent: {Exponent} using an RSize with Exponent: {factor.Exponent}.")
		//		: new RSize(Width * factor.Width, Height * factor.Height, factor.Exponent);
		//}

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

		#region ToString / ICloneable / IEqualityComparer / IEquatable Support

		public override string ToString()
		{
			return ToString(reduce: true);
		}

		public string ToString(bool reduce)
		{
			if (reduce)
			{
				return BigIntegerHelper.GetDisplay(Reducer.Reduce(this));
			}
			else
			{
				return BigIntegerHelper.GetDisplay(this);
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RSize Clone()
		{
			return Reducer.Reduce(this);
		}

		public bool Equals(RSize? a, RSize? b)
		{
			if (a is null)
			{
				return b is null;
			}
			else
			{
				return a.Equals(b);
			}
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as RSize);
		}

		public bool Equals(RSize? other)
		{
			return !(other is null) && WidthNumerator.Equals(other.WidthNumerator) && HeightNumerator.Equals(other.HeightNumerator);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(WidthNumerator, HeightNumerator);
		}

		public int GetHashCode(RSize obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(RSize p1, RSize p2)
		{
			return EqualityComparer<RSize>.Default.Equals(p1, p2);
		}

		public static bool operator !=(RSize p1, RSize p2)
		{
			return !(p1 == p2);
		}

		#endregion
	}
}
