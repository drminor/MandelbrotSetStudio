﻿using ProtoBuf;
using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	using bh = BigIntegerHelper;

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

		public RSizeDto(BigInteger[] bigIntegers, int exponent) : this(bigIntegers.Select(v => bh.ToLongs(v)).ToArray(), exponent)
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
