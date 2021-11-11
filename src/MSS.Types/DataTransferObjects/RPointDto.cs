using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[Serializable]
	public record RPointDto(byte[] X, byte[] Y, int Exponent)
	{
		public RPointDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RPointDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToByteArray()).ToArray(), exponent)
		{ }

		public RPointDto(byte[][] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		public byte[][] GetValues()
		{
			byte[][] result = new byte[][] { X, Y };
			return result;
		}
	}

}
