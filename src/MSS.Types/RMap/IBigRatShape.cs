using System;
using System.Numerics;

namespace MSS.Types
{
	public interface IBigRatShape : ICloneable
	{
		int Exponent { get; init; }
		BigInteger[] Values { get; }
	}
}