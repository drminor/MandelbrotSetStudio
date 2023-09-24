using ProtoBuf;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class RVectorDto
	{
		[ProtoMember(1)]
		public long[] X { get; init; }

		[ProtoMember(2)]
		public long[] Y { get; init; }

		[ProtoMember(3)]
		public int Exponent { get; init; }

		public RVectorDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RVectorDto(BigInteger[] values, int exponent) : this(BigIntegerHelper.ToLongs(values), exponent)
		{ }

		public RVectorDto(long[][] values, int exponent)
		{
			X = values[0];
			Y = values[1];
			Exponent = exponent;
		}

		public long[][] GetValues()
		{
			return new long[][] { X, Y };
		}
	}
}
