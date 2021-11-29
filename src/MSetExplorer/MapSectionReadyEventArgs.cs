using MEngineDataContracts;
using System;

namespace MSetExplorer
{
	public class MapSectionReadyEventArgs : EventArgs
	{
		public MapSectionReadyEventArgs(MapSectionResponse mapSectionResponse)
		{
			MapSectionResponse = mapSectionResponse;
		}

		public MapSectionResponse MapSectionResponse { get; set; }
	}
}
