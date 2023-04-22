using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ImageDraggedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt DragOffset { get; init; }

		public ImageDraggedEventArgs(TransformType transformType, VectorInt dragOffset)
		{
			TransformType = transformType;
			DragOffset = dragOffset;
		}
	}

}
