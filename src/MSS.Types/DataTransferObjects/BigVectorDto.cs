using ProtoBuf;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace MSS.Types.DataTransferObjects
{
	[ProtoContract(SkipConstructor = true)]
	public class BigVectorDto : ICloneable
	{
		[ProtoMember(1)]
		public long[] X { get; init; }

		[ProtoMember(2)]
		public long[] Y { get; init; }

		public BigVectorDto() : this(new BigInteger[] { 0, 0 })
		{ }

		public BigVectorDto(BigInteger[] values) : this(BigIntegerHelper.ToLongsDeprecated(values))
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

		public override string? ToString()
		{
			var sb = new StringBuilder();

			_ = sb.Append("X:")
			.Append(GetString(X))
			.Append(", Y:")
			.Append(GetString(Y));

			return sb.ToString();
		}

		private string GetString(long[] vals)
		{
			return vals[1] == 0
				? vals[0].ToString(CultureInfo.InvariantCulture)
				: vals[1].ToString(CultureInfo.InvariantCulture) + ", " + vals[0].ToString(CultureInfo.InvariantCulture);
		}

		public object Clone()
		{
			return new BigVectorDto(GetValues());
		}
	}
}
