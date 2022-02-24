﻿using MSS.Types;
using MSS.Types.DataTransferObjects;

namespace MSS.Common.DataTransferObjects
{
	public class DtoMapper : IMapper<RPoint, RPointDto>, IMapper<RSize, RSizeDto>, IMapper<RVector, RVectorDto>, IMapper<RRectangle, RRectangleDto>, IMapper<BigVector, BigVectorDto>
	{
		public RPointDto MapTo(RPoint source)
		{
			var result = new RPointDto(source.Values, source.Exponent);
			return result;
		}

		public RPoint MapFrom(RPointDto target)
		{
			var bVals = BigIntegerHelper.FromLongs(target.GetValues());
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
			var bVals = BigIntegerHelper.FromLongs(target.GetValues());
			var result = new RSize(bVals, target.Exponent);
			return result;
		}

		public RVectorDto MapTo(RVector source)
		{
			var result = new RVectorDto(source.Values, source.Exponent);
			return result;
		}

		public RVector MapFrom(RVectorDto target)
		{
			var bVals = BigIntegerHelper.FromLongs(target.GetValues());
			var result = new RVector(bVals, target.Exponent);
			return result;
		}

		public RRectangleDto MapTo(RRectangle source)
		{
			var result = new RRectangleDto(source.Values, source.Exponent);
			return result;
		}

		public RRectangle MapFrom(RRectangleDto target)
		{
			var bVals = BigIntegerHelper.FromLongs(target.GetValues());
			var result = new RRectangle(bVals, target.Exponent);
			return result;
		}

		public BigVectorDto MapTo(BigVector source)
		{
			var result = new BigVectorDto(source.Values);
			return result;
		}

		public BigVector MapFrom(BigVectorDto target)
		{
			var bVals = BigIntegerHelper.FromLongs(target.GetValues());
			var result = new BigVector(bVals);
			return result;
		}

	}
}
