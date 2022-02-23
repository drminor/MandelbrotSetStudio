﻿using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	using bh = BigIntegerHelper;

	public record RRectangleDto(long[] X1, long[] X2, long[] Y1, long[] Y2, int Exponent)
	{
		public RRectangleDto() : this(new BigInteger[] { 0, 0, 0, 0 }, 0)
		{ }

		public RRectangleDto(BigInteger[] values, int exponent) : this(values.Select(v => bh.ToLongs(v)).ToArray(), exponent)
		{ }

		public RRectangleDto(long[][] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public long[][] GetValues()
		{
			long[][] result = new long[][] { X1, X2, Y1, Y2 };
			return result;
		}
	}
}
