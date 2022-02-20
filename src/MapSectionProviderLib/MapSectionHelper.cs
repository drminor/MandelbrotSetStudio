using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;

namespace MapSectionProviderLib
{
	public static class MapSectionHelper
	{
		public static MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings, out RPoint mapPosition)
		{
			//bool inverted;
			//PointInt nBlkPos;

			//if (blockPosition.Y < 0)
			//{
			//	inverted = true;
			//	nBlkPos = new PointInt(blockPosition.X, -1 * blockPosition.Y);
			//}
			//else
			//{
			//	inverted = false;
			//	nBlkPos = blockPosition;
			//}

			var nrmSubdivionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);
			mapPosition = nrmSubdivionPosition.Translate(nrmSamplePointDelta.Scale(blockPosition.Scale(subdivision.BlockSize)));

			//if (mapPosition.Y < 0)
			//{
			//	inverted = true;
			//	nBlkPos = new PointInt(blockPosition.X, -1 + (-1 * blockPosition.Y));
			//	mapPosition = subPos.Translate(spd.Scale(nBlkPos.Scale(subdivision.BlockSize)));
			//}
			//else
			//{
			//	inverted = false;
			//	nBlkPos = blockPosition;
			//}

			var dtoMapper = new DtoMapper();
			var posForDataTransfer = dtoMapper.MapTo(mapPosition);
			var spdForDataTransfer = dtoMapper.MapTo(subdivision.SamplePointDelta);

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition, // nBlkPos,
				BlockSize = subdivision.BlockSize,
				Position = posForDataTransfer,
				SamplePointsDelta = spdForDataTransfer,
				MapCalcSettings = mapCalcSettings,
				Inverted = false // inverted
			};

			return mapSectionRequest;
		}

		public static MapSectionRequest CreateRequestV1(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings, out RPoint mapPosition)
		{
			var nrmSubdivionPosition = RNormalizer.Normalize(subdivision.Position, subdivision.SamplePointDelta, out var nrmSamplePointDelta);
			mapPosition = nrmSubdivionPosition.Translate(nrmSamplePointDelta.Scale(blockPosition.Scale(subdivision.BlockSize)));

			var dtoMapper = new DtoMapper();
			var posForDataTransfer = dtoMapper.MapTo(mapPosition);
			var spdForDataTransfer = dtoMapper.MapTo(subdivision.SamplePointDelta);


			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = posForDataTransfer,
				SamplePointsDelta = spdForDataTransfer,
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;
		}

		}
	}
