using MSS.Common.DataTransferObjects;
using MSS.Types;
using ProjectRepo.Entities;
using System.Linq;
using System.Numerics;

namespace ProjectRepo
{
	public class CoordsHelper
	{
		DtoMapper _dtoMapper;

		public CoordsHelper(DtoMapper dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}

		public RPointRecord BuildPointRecord(RPoint rPoint)
		{
			var display = GetDisplay(rPoint.Values, rPoint.Exponent);

			var rPointDto = _dtoMapper.MapTo(rPoint);
			var result = new RPointRecord(display, rPointDto);

			return result;
		}

		public RSizeRecord BuildSizeRecord(RSize rSize)
		{
			var display = GetDisplay(rSize.Values, rSize.Exponent);

			var rRectangleDto = _dtoMapper.MapTo(rSize);
			var result = new RSizeRecord(display, rRectangleDto);

			return result;
		}

		public RRectangleRecord BuildCoords(RRectangle rRectangle)
		{
			var display = GetDisplay(rRectangle.Values, rRectangle.Exponent);

			var rRectangleDto = _dtoMapper.MapTo(rRectangle);
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}

		private string GetDisplay(BigInteger[] values, int exponent)
		{
			var strVals = values.Select(x => BigIntegerHelper.GetValue(x, exponent).ToString()).ToArray();
			var display = string.Join("; ", strVals);

			return display;
		}
	}
}
