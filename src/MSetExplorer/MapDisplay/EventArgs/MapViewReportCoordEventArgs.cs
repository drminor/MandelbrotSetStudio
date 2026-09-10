using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapViewReportCoordEventArgs : EventArgs
	{
		public PointDbl Position { get; init; }
		public SizeDbl DisplaySize { get; init; }
		public MapCenterAndDelta CurrentMapAreaInfo { get; init; }

		public MapViewReportCoordEventArgs(PointDbl position, SizeDbl displaySize, MapCenterAndDelta currentMapAreaInfo)
		{
			Position = position;
			DisplaySize = displaySize;
			CurrentMapAreaInfo = currentMapAreaInfo;
		}
	}

}
