using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionProvider
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;
		private readonly DtoMapper _dtoMapper;

		public MapSectionProvider(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
			_dtoMapper = new DtoMapper();
		}

		public async Task<MapSectionResponse> GenerateMapSectionAsync(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{
			var mapSectionResponse = await _mapSectionRepo.GetMapSectionAsync(subdivision.Id.ToString(), blockPosition);

			if (mapSectionResponse is null)
			{
				var mapSectionRequest = CreateRequest(subdivision, blockPosition, mapCalcSettings);
				mapSectionResponse = await _mEngineClient.GenerateMapSectionAsync(mapSectionRequest);
				_mapSectionRepo.SaveMapSection(mapSectionResponse);
			}

			return mapSectionResponse;
		}

		private MapSectionRequest CreateRequest(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings)
		{
			RPoint subPosition;

			if (subdivision.Position.Exponent > subdivision.SamplePointDelta.Exponent)
			{
				int diff = subdivision.Position.Exponent - subdivision.SamplePointDelta.Exponent;
				subPosition = new RPoint(subdivision.Position.X * diff, subdivision.Position.Y * diff, subdivision.SamplePointDelta.Exponent);
			}
			else if (subdivision.Position.Exponent < subdivision.SamplePointDelta.Exponent)
			{
				subPosition = subdivision.Position;
			}
			else
			{
				subPosition = subdivision.Position;
			}

			var position = new RPoint(
				subPosition.X + blockPosition.X * subdivision.SamplePointDelta.Width * subdivision.BlockSize.Width,
				subPosition.Y + blockPosition.Y * subdivision.SamplePointDelta.Height * subdivision.BlockSize.Height,
				subdivision.SamplePointDelta.Exponent);

			var mapSectionRequest = new MapSectionRequest
			{
				SubdivisionId = subdivision.Id.ToString(),
				BlockPosition = blockPosition,
				BlockSize = subdivision.BlockSize,
				Position = _dtoMapper.MapTo(position),
				SamplePointsDelta = _dtoMapper.MapTo(subdivision.SamplePointDelta),
				MapCalcSettings = mapCalcSettings
			};

			return mapSectionRequest;

		}

	}
}
