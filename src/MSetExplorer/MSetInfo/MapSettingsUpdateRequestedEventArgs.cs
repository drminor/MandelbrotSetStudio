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

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int targetIterations)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			TargetIterations = targetIterations;
		}

		//public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int requestsPerJob)
		//{
		//	MapSettingsUpdateType = mapSettingsUpdateType;
		//	RequestsPerJob = requestsPerJob;
		//}

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, RRectangle coords)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			Coords = coords;
		}

	}

}
