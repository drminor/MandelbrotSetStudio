﻿using ProtoBuf;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class RPointDto
	{
		[ProtoMember(1)]
		public long[] X { get; init; }

		[ProtoMember(2)]
		public long[] Y { get; init; }

		[ProtoMember(3)]
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
