using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt ScreenArea { get; init; }
		public MapAreaInfo CurrentMapAreaInfo { get; init; }
		public bool IsPreview { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, RectangleInt screenArea, MapAreaInfo currentMapAreaInfo, bool isPreview = false)
		{
			TransformType = transformType;
			ScreenArea = screenArea;
			CurrentMapAreaInfo = currentMapAreaInfo;
			IsPreview = isPreview;
		}
	}

}
