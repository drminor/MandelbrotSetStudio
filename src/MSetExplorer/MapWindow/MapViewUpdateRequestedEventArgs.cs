using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateRequestedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt NewArea { get; init; }

		public MapViewUpdateRequestedEventArgs(TransformType transformType, RectangleInt newArea)
		{
			TransformType = transformType;
			NewArea = newArea;
		}
	}

}
