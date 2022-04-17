using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt NewArea { get; init; }
		public bool IsPreview { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, RectangleInt newArea, bool isPreview = false)
		{
			TransformType = transformType;
			NewArea = newArea;
			IsPreview = isPreview;
		}
	}

}
