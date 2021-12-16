using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Numerics;

namespace MSS.Common.DataTransferObjects
{
	public class DtoMapper : IMapper<RRectangle, RRectangleDto>, IMapper<RSize, RSizeDto>, IMapper<RPoint, RPointDto>
	{
		public RPointDto MapTo(RPoint source)
		{
			var result = new RPointDto(source.Values, source.Exponent);
			return result;
		}

		public RPoint MapFrom(RPointDto target)
		{
			var bVals = GetFromLongs(target.GetValues());
			var result = new RPoint(bVals, target.Exponent);
			return result;
		}

		public RSizeDto MapTo(RSize source)
		{
			var result = new RSizeDto(source.Values, source.Exponent);
			return result;
		}

		public RSize MapFrom(RSizeDto target)
		{
			var bVals = GetFromLongs(target.GetValues());
			var result = new RSize(bVals, target.Exponent);
			return result;
		}

		public RRectangleDto MapTo(RRectangle source)
		{
			var result = new RRectangleDto(source.Values, source.Exponent);
			return result;
		}

		public RRectangle MapFrom(RRectangleDto target)
		{
			var bVals = GetFromLongs(target.GetValues());
			var result = new RRectangle(bVals, target.Exponent);
			return result;
		}

		private BigInteger[] GetFromLongs(long[][] vals)
		{
			var cnt = vals.GetLength(0);
			var bVals = new BigInteger[cnt];

			for (var i = 0; i < cnt; i++)
			{
				var t = new BigInteger(0);
				foreach (var v in vals[i])
				{
					t += v;
				}
				bVals[i] = t;
			}

			return bVals;
		}

	}
}
