using MSS.Types;
using System;

namespace MSetExplorer
{
	internal class AreaSelectedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public RectangleInt Area { get; init; }

		public AreaSelectedEventArgs(TransformType transformType, RectangleInt area)
		{
			TransformType = transformType;
			Area = area;
		}
	}

	internal class ScreenPannedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public SizeInt Offset { get; init; }

		public ScreenPannedEventArgs(TransformType transformType, SizeInt offset)
		{
			TransformType = transformType;
			Offset = offset;
		}
	}

}
