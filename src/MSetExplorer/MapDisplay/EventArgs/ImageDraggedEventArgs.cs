using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ImageDraggedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt Offset { get; init; }

		public ImageDraggedEventArgs(TransformType transformType, VectorInt offset)
		{
			TransformType = transformType;
			Offset = offset;
		}
	}

}
