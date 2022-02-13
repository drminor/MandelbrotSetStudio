﻿using MSS.Types;
using System;

namespace MSetExplorer.MapWindow
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
}
