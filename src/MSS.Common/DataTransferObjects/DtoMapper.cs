using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Linq;
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
			var result = new RPoint(target.GetValues().Select(v => new BigInteger(v)).ToArray(), target.Exponent);
			return result;
		}

		public RSizeDto MapTo(RSize source)
		{
			var result = new RSizeDto(source.Values, source.Exponent);
			return result;
		}

		public RSize MapFrom(RSizeDto target)
		{
			var result = new RSize(target.GetValues().Select(v => new BigInteger(v)).ToArray(), target.Exponent);
			return result;
		}

		public RRectangleDto MapTo(RRectangle source)
		{
			var result = new RRectangleDto(source.Values, source.Exponent);
			return result;
		}

		public RRectangle MapFrom(RRectangleDto target)
		{
			var result = new RRectangle(target.GetValues().Select(v => new BigInteger(v)).ToArray(), target.Exponent);
			return result;
		}

	}
}
