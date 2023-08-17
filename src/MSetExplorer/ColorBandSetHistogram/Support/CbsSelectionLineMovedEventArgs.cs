using MSS.Types;
using System;

namespace MSetExplorer
{
	public class CbsSelectionLineMovedEventArgs : EventArgs
	{
		public VectorInt DragOffset { get; init; }

		public bool IsPreview { get; init; }
		public bool IsPreviewBeingCancelled { get; init; }

		public CbsSelectionLineMovedEventArgs(VectorInt dragOffset, bool isPreview)
		{
			DragOffset = dragOffset;
			IsPreview = isPreview;
		}

		public static CbsSelectionLineMovedEventArgs CreateCancelPreviewInstance()
		{
			var result = new CbsSelectionLineMovedEventArgs(new VectorInt(), isPreview: true)
			{
				IsPreviewBeingCancelled = true
			};

			return result;
		}
	}

}
