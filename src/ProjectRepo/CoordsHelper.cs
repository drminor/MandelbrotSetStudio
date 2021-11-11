using MSS.Common;
using MSS.Types;
using MSS.Types.Base;
using MSS.Types.DataTransferObjects;
using ProjectRepo.Entities;
using System;
using System.Linq;

namespace ProjectRepo
{
	public class CoordsHelper
	{
		IMapper<RRectangle, RRectangleDto> _dtoMapper;

		public CoordsHelper(IMapper<RRectangle, RRectangleDto> dtoMapper)
		{
			_dtoMapper = dtoMapper;
		}

		public RRectangleRecord BuildCoords(RRectangle rRectangle)
		{
			var display = GetDisplay(rRectangle);

			var rRectangleDto = _dtoMapper.MapTo(rRectangle);
			var result = new RRectangleRecord(display, rRectangleDto);

			return result;
		}

		private string GetDisplay(RRectangle rRectangle)
		{
			double scaleFactor = Math.Pow(2, rRectangle.Exponent);
			double denominator = 1d / scaleFactor;
			string strDenominator = denominator.ToString();

			var dRectangle = new DRectangle(rRectangle.Values);
			dRectangle = dRectangle.Scale(scaleFactor);

			Rectangle<StringStruct> strVals = new Rectangle<StringStruct>(
				rRectangle.Values.Select((x,i) => 
				new StringStruct(x.ToString() + "/" + strDenominator + " (" + dRectangle.Values[i].ToString() + ")")).ToArray()
				);

			string display = strVals.ToString();
			return display;
		}

	}
}
