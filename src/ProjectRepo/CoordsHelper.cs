using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.Base;
using ProjectRepo.Entities;
using System;
using System.Diagnostics;
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
			var strDenominator = GetValue(1, -1 * exponent).ToString();

			var dVals = values.Select(v => GetValue(v, exponent)).ToArray();

			string[] strVals = values.Select((x,i) => new string(x.ToString() + "/" + strDenominator + " (" + dVals[i].ToString() + ")")).ToArray();

			string display = string.Join("; ", strVals);
			return display;
		}

		public double GetValue(BigInteger bigInteger, int exponent)
		{
			double result = Math.ScaleB(ConvertBigIntegerToDouble(bigInteger), exponent);
			return result;
		}

		private double ConvertBigIntegerToDouble(BigInteger val)
		{
			try
			{
				if (!SafeCastToDouble(val))
				{
					throw new OverflowException($"It is not safe to cast BigInteger: {val} to a double.");
				}

				double result = (double)val;

				if (!DoubleHelper.HasPrecision(result))
				{
					throw new OverflowException($"When converting BigInteger: {val} to a double, precision was lost.");
				}

				return result;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e.GetType()}:{e.Message}");
				return 0;
			}
		}

		private static bool SafeCastToDouble(BigInteger value)
		{
			BigInteger s_bnDoubleMinValue = (BigInteger)double.MinValue;
			BigInteger s_bnDoubleMaxValue = (BigInteger)double.MaxValue;
			return s_bnDoubleMinValue <= value && value <= s_bnDoubleMaxValue;
		}

	}
}
