﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace MSS.Types
{
	public class RPoint : IBigRatShape, IEquatable<RPoint?>, IEqualityComparer<RPoint>
	{
		private static RPoint ZeroSingleton = new RPoint();

		public static RPoint Zero => ZeroSingleton;

		public BigInteger[] Values { get; init; }

		public int Exponent { get; init; }

		public RPoint() : this(0, 0, 0)
		{ }

		public RPoint(RVector rVector) : this(rVector.Values, rVector.Exponent)
		{ }

		public RPoint(BigInteger[] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		public RPoint(BigInteger x, BigInteger y, int exponent)
		{
			Values = new BigInteger[] { x, y };
			Exponent = exponent;
		}

		public BigInteger XNumerator => Values[0];

		public BigInteger YNumerator => Values[1];

		public RValue X => new RValue(XNumerator, Exponent);
		public RValue Y => new RValue(YNumerator, Exponent);


		//public RPoint Scale(SizeInt factor)
		//{
		//	return new RPoint(XNumerator * factor.Width, YNumerator * factor.Height, Exponent);
		//}

		//public RPoint Scale(PointInt factor)
		//{
		//	return new RPoint(XNumerator * factor.X, YNumerator * factor.Y, Exponent);
		//}

		//public RPoint Scale(RSize factor)
		//{
		//	return factor.Exponent != Exponent
		//      ? throw new InvalidOperationException($"Cannot scale a RPoint with Exponent: {Exponent} using a RSize with Exponent: {factor.Exponent}.")
		//		: new RPoint(X * factor.Width, Y * factor.Height, factor.Exponent);
		//}

		//public RPoint Translate(RPoint amount)
		//{
		//	return amount.Exponent != Exponent
  //              ?                throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
		//		: new RPoint(XNumerator + amount.XNumerator, YNumerator + amount.YNumerator, amount.Exponent);
		//}

		public RVector Translate(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
				: new RVector(XNumerator + amount.WidthNumerator, YNumerator + amount.HeightNumerator, amount.Exponent);
		}

		public RPoint Translate(RVector amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot translate a RPoint with Exponent: {Exponent} using a RSize with Exponent: {amount.Exponent}.")
				: new RPoint(XNumerator + amount.XNumerator, YNumerator + amount.YNumerator, amount.Exponent);
		}

		/// <summary>
		/// Returns the distance from this point to the given point.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public RVector Diff(RPoint amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot find the diff from a RPoint with Exponent: {Exponent} using a RPoint with Exponent: {amount.Exponent}.")
				: new RVector(XNumerator - amount.XNumerator, YNumerator - amount.YNumerator, amount.Exponent);
		}

		public BigVector Divide(RSize amount)
		{
			return amount.Exponent != Exponent
				? throw new InvalidOperationException($"Cannot Divide an RVector with Exponent: {Exponent} using an RSize with Exponent: {amount.Exponent}.")
				: new BigVector(XNumerator / amount.WidthNumerator, YNumerator / amount.HeightNumerator);
		}

		public bool IsZero()
		{
			return XNumerator == 0 && YNumerator == 0;	
		}

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

		public RPoint Clone()
		{
			return Reducer.Reduce(this);
		}

		public bool Equals(RPoint? a, RPoint? b)
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
			return Equals(obj as RPoint);
		}

		public bool Equals(RPoint? other)
		{
			return !(other is null) && XNumerator.Equals(other.XNumerator) && YNumerator.Equals(other.YNumerator);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(XNumerator, YNumerator);
		}

		public int GetHashCode(RPoint obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(RPoint? p1, RPoint? p2)
		{
			return EqualityComparer<RPoint>.Default.Equals(p1, p2);
		}

		public static bool operator !=(RPoint? p1, RPoint? p2)
		{
			return !(p1 == p2);
		}

		#endregion
	}
}
