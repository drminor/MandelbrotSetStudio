using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class MapSettingsUpdateRequestedEventArgs : EventArgs
	{
		public MapSettingsUpdateType MapSettingsUpdateType { get; init; }
		public int TargetIterations { get; init; }
		public int RequestsPerJob { get; init; }
		public bool SaveTheZValues { get; init; }

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int targetIterations)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			TargetIterations = targetIterations;
		}

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, bool saveTheZValues)
		{
			Debug.Assert(mapSettingsUpdateType == MapSettingsUpdateType.SaveTheZValues, "Expecting the MapSettingsUpdateType to be 'SaveTheZValeus'");
			MapSettingsUpdateType = mapSettingsUpdateType;
			SaveTheZValues = saveTheZValues;
		}

		//public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int requestsPerJob)
		//{
		//	MapSettingsUpdateType = mapSettingsUpdateType;
		//	RequestsPerJob = requestsPerJob;
		//}
	}

}
