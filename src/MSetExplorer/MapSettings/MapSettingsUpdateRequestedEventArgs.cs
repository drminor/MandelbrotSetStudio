using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapSettingsUpdateRequestedEventArgs : EventArgs
	{
		public MapSettingsUpdateType MapSettingsUpdateType { get; init; }
		public int TargetIterations { get; init; }
		public int RequestsPerJob { get; init; }
		public bool SaveTheZValues { get; init; }
		public bool CalculateEscapeVelocities { get; init; }

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int targetIterations)
		{
			MapSettingsUpdateType = mapSettingsUpdateType;
			TargetIterations = targetIterations;
		}

		public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, bool newValue)
		{
			if (mapSettingsUpdateType == MapSettingsUpdateType.SaveTheZValues)
			{
				SaveTheZValues = newValue;
			}
			else if (mapSettingsUpdateType == MapSettingsUpdateType.CalculateEscapeVelocities)
			{
				CalculateEscapeVelocities = newValue;
			}
			else
			{
				throw new InvalidOperationException($"A value of {mapSettingsUpdateType} is not valid when the new value is of type bool.");
			}

			MapSettingsUpdateType = mapSettingsUpdateType;
		}


		//public MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType mapSettingsUpdateType, int requestsPerJob)
		//{
		//	MapSettingsUpdateType = mapSettingsUpdateType;
		//	RequestsPerJob = requestsPerJob;
		//}
	}

}
