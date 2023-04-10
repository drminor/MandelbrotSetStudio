using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt ScreenArea { get; init; }
		public SizeDbl CanvasSize { get; init; }
		public bool IsPreview { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, RectangleInt screenArea, SizeDbl canvasSize, bool isPreview = false)
		{
			TransformType = transformType;
			ScreenArea = screenArea;
			CanvasSize = canvasSize;
			IsPreview = isPreview;
		}
	}

}
