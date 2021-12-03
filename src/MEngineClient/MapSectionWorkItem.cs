using MEngineDataContracts;
using System;

namespace MEngineClient
{
	public class MapSectionWorkItem
	{
		public MapSectionRequest MapSectionRequest { get; init; }
		public Action<MapSectionResponse> WorkAction { get; init; }

		public MapSectionWorkItem(MapSectionRequest mapSectionRequest, Action<MapSectionResponse> workAction)
		{
			MapSectionRequest = mapSectionRequest ?? throw new ArgumentNullException(nameof(mapSectionRequest));
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
		}

	}
}
