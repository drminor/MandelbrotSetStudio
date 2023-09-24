using ProtoBuf;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class RSizeDto
	{
		[ProtoMember(1)]
		public long[] Width { get; init; }

		[ProtoMember(2)]
		public long[] Height { get; init; }

		[ProtoMember(3)]
		public int Exponent { get; init; }

		public RSizeDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RSizeDto(BigInteger[] values, int exponent) : this(BigIntegerHelper.ToLongsM2(values), exponent)
		{ }

		public RSizeDto(long[][] values, int exponent)
		{
			Width = values[0];
			Height = values[1];
			Exponent = exponent;
		}

		public long[][] GetValues() => new long[][] { Width, Height };
	}
}
