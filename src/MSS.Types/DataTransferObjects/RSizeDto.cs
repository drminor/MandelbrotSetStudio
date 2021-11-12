using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace MSS.Types.DataTransferObjects
{
	[DataContract]
	public class RSizeDto
	{
		[DataMember(Order = 1)]
		public byte[] Width { get; init; }

		[DataMember(Order = 2)]
		public byte[] Height { get; init; }

		[DataMember(Order = 3)]
		public int Exponent { get; init; }

		public RSizeDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RSizeDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToByteArray()).ToArray(), exponent)
		{ }

		public RSizeDto(byte[][] values, int exponent)
		{
			Width = values[0];
			Height = values[1];
			Exponent = exponent;
		}

		public byte[][] GetValues() => new byte[][] { Width, Height };
	}
}
