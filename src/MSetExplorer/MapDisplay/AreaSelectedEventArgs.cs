using MSS.Types;
using System;

namespace MSetExplorer
{
	public class AreaSelectedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt Area { get; init; }

		public bool IsPreview { get; init; }

		public AreaSelectedEventArgs(TransformType transformType, RectangleInt area, bool isPreview = false)
		{
			TransformType = transformType;
			Area = area;
			IsPreview = isPreview;
		}
	}

}
