using MSS.Types;
using MSS.Types.DataTransferObjects;
using System.Linq;
using System.Numerics;

namespace MSS.Common.DataTransferObjects
{
	public class DtoMapper : IMapper<RRectangle, RRectangleDto>
	{
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
