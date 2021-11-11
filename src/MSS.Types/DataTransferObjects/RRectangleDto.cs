using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[Serializable]
	public record RRectangleDto(byte[] X1, byte[] X2, byte[] Y1, byte[] Y2, int Exponent)
	{
		public RRectangleDto() : this(new BigInteger[] { 0, 0, 0, 0 }, 0)
		{ }

		public RRectangleDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToByteArray()).ToArray(), exponent)
		{ }

		public RRectangleDto(byte[][] values, int exponent) : this(values[0], values[1], values[2], values[3], exponent)
		{ }

		public byte[][] GetValues()
		{
			byte[][] result = new byte[][] { X1, X2, Y1, Y2 };
			return result;
		}
	}
}
