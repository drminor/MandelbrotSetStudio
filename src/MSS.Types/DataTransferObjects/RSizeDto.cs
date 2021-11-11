using System;
using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[Serializable]
	public record RSizeDto(byte[] Width, byte[] Height, int Exponent)
	{
		public RSizeDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RSizeDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToByteArray()).ToArray(), exponent)
		{ }

		public RSizeDto(byte[][] values, int exponent) : this(values[0], values[1], exponent)
		{ }

		public byte[][] GetValues()
		{
			byte[][] result = new byte[][] { Width, Height };
			return result;
		}
	}

}
