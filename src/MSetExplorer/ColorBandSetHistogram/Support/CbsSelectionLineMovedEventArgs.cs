using System;

namespace MSetExplorer
{
	public class CbsSelectionLineMovedEventArgs : EventArgs
	{
		public int ColorBandIndex { get; init; }
		public double NewXPosition { get; init; }

		public bool IsPreview { get; init; }
		public bool IsPreviewBeingCancelled { get; init; }

		public CbsSelectionLineMovedEventArgs(int colorBandIndex, double newXPosition, bool isPreview)
		{
			ColorBandIndex = colorBandIndex;
			NewXPosition = newXPosition;
			IsPreview = isPreview;
		}

		public static CbsSelectionLineMovedEventArgs CreateCancelPreviewInstance(int colorBandIndex)
		{
			var result = new CbsSelectionLineMovedEventArgs(colorBandIndex, -1, isPreview: true)
			{
				IsPreviewBeingCancelled = true
			};

			return result;
		}
	}

}
