using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.DataTransferObjects;
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
			var display = BigIntegerHelper.GetDisplay(rPoint);

			var rPointDto = _dtoMapper.MapTo(rPoint);
			var result = new RPointRecord(display, rPointDto);

			return result;
		}

		public RSizeRecord BuildSizeRecord(RSize rSize)
		{
			var display = BigIntegerHelper.GetDisplay(rSize);

			var rSizeDto = _dtoMapper.MapTo(rSize);
			var result = new RSizeRecord(display, rSizeDto);

			return result;
		}

		public RVectorRecord BuildVectorRecord(RVectorDto rVectorDto)
		{
			var rVector = _dtoMapper.MapFrom(rVectorDto);
			var display = BigIntegerHelper.GetDisplay(rVector);
			var result = new RVectorRecord(display, rVectorDto);

			return result;
		}

		public RVectorRecord BuildVectorRecord(RVector rVector)
		{
			var display = BigIntegerHelper.GetDisplay(rVector);
			var rVectorDto = _dtoMapper.MapTo(rVector);
			var result = new RVectorRecord(display, rVectorDto);

			return result;
		}

		public RRectangleRecord BuildCoords(RRectangle rRectangle)
		{
			var display = BigIntegerHelper.GetDisplay(rRectangle);

			var rRectangleDto = _dtoMapper.MapTo(rRectangle);
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}
	}
}
