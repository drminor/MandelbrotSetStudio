using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt PanAmount { get; init; }
		public int Factor { get; init; }

		public bool IsPreview { get; init; }

		public RectangleInt ScreenArea { get; init; }
		public MapAreaInfo2 CurrentMapAreaInfo { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, RectangleInt screenArea, MapAreaInfo2 currentMapAreaInfo, bool isPreview = false)
		{
			TransformType = transformType;
			PanAmount = new VectorInt();
			Factor = 1;

			CurrentMapAreaInfo = currentMapAreaInfo;
			IsPreview = isPreview;

			ScreenArea = screenArea;
		}

		public MapViewUpdateRequestedEventArgs(TransformType transformType, VectorInt panAmount, int factor, MapAreaInfo2 currentMapAreaInfo, bool isPreview = false)
		{
			TransformType = transformType;
			PanAmount = panAmount;
			Factor = factor;
			
			CurrentMapAreaInfo = currentMapAreaInfo;
			IsPreview = isPreview;

			ScreenArea = new RectangleInt();
		}

	}

}
