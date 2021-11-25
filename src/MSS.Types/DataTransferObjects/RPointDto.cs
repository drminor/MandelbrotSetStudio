using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace MSS.Types.DataTransferObjects
{
	[DataContract]
	public class RPointDto
	{
		[DataMember(Order = 1)]
		public long[] X { get; init; }

		[DataMember(Order = 2)]
		public long[] Y { get; init; }

		[DataMember(Order = 3)]
		public int Exponent { get; init; }

		public RPointDto() : this(new BigInteger[] { 0, 0 }, 0)
		{ }

		public RPointDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => v.ToLongs()).ToArray(), exponent)
		{ }

		public RPointDto(long[][] values, int exponent)
		{
			X = values[0];
			Y = values[1];
			Exponent = exponent;
		}

		public long[][] GetValues() => new long[][] { X, Y };
	}
}
