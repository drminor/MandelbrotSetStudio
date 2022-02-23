﻿using ProtoBuf;
using System.Linq;
using System.Numerics;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class BigVectorDto
	{
		[ProtoMember(1)]
		public long[] X { get; init; }

		[ProtoMember(2)]
		public long[] Y { get; init; }

		public BigVectorDto() : this(new BigInteger[] { 0, 0 })
		{ }

		public BigVectorDto(BigInteger[] values) : this(values.Select(v => BigIntegerHelper.ToLongs(v)).ToArray())
		{ }

		public BigVectorDto(long[][] values)
		{
			X = values[0];
			Y = values[1];
		}

		public long[][] GetValues()
		{
			return new long[][] { X, Y };
		}
	}
}