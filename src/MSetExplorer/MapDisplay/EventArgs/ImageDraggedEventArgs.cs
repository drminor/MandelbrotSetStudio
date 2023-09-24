using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ImageDraggedEventArgs : EventArgs
	{
		public TransformType TransformType { get; init; }
		public VectorInt DragOffset { get; init; }

		public bool IsPreview { get; init; }
		public bool IsPreviewBeingCancelled { get; init; }

		public ImageDraggedEventArgs(TransformType transformType, VectorInt dragOffset, bool isPreview)
		{
			TransformType = transformType;
			DragOffset = dragOffset;
			IsPreview = isPreview;
		}

		public static ImageDraggedEventArgs CreateCancelPreviewInstance(TransformType transformType)
		{
			var result = new ImageDraggedEventArgs(transformType, new VectorInt(), isPreview: true)
			{
				IsPreviewBeingCancelled = true
			};

			return result;
		}
	}

}
