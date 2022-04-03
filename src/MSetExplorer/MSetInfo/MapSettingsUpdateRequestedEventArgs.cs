using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapSettingsUpdateRequestedEventArgs : EventArgs
	{
		public MapSettingsUpdateType MapSettingsUpdateType { get; init; }
		public int TargetIterations { get; init; }
		public int RequestsPerJob { get; init; }
		public RRectangle? Coords { get; init; }

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int targetIterations, int requestsPerJob)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			TargetIterations = targetIterations;
			RequestsPerJob = requestsPerJob;
		}

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, RRectangle coords)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			Coords = coords;
		}

	}

}
