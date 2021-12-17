using MSS.Common.DataTransferObjects;
using MSS.Types;
using ProjectRepo.Entities;

namespace ProjectRepo
{
	public class CoordsHelper
	{
		private readonly DtoMapper _dtoMapper;

		public CoordsHelper(DtoMapper dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}

		public RPointRecord BuildPointRecord(RPoint rPoint)
		{
			var display = BigIntegerHelper.GetDisplay(rPoint.Values, rPoint.Exponent);

			var rPointDto = _dtoMapper.MapTo(rPoint);
			var result = new RPointRecord(display, rPointDto);

			return result;
		}

		public RSizeRecord BuildSizeRecord(RSize rSize)
		{
			var display = BigIntegerHelper.GetDisplay(rSize.Values, rSize.Exponent);

			var rRectangleDto = _dtoMapper.MapTo(rSize);
			var result = new RSizeRecord(display, rRectangleDto);

			return result;
		}

		public RRectangleRecord BuildCoords(RRectangle rRectangle)
		{
			var display = BigIntegerHelper.GetDisplay(rRectangle.Values, rRectangle.Exponent);

			var rRectangleDto = _dtoMapper.MapTo(rRectangle);
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}
	}
}
