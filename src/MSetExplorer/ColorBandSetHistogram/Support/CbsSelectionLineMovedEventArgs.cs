using System;

namespace MSetExplorer
{
	public class CbsSelectionLineMovedEventArgs : EventArgs
	{
		public int ColorBandIndex { get; init; }
		public double NewXPosition { get; init; }
		public CbsSelectionLineDragOperation Operation { get; init; }

		public CbsSelectionLineMovedEventArgs(int colorBandIndex, double newXPosition, CbsSelectionLineDragOperation operation)
		{
			Operation = operation;
			ColorBandIndex = colorBandIndex;
			NewXPosition = newXPosition;
		}

		//public static CbsSelectionLineMovedEventArgs CreateCancelDragInstance(int colorBandIndex)
		//{
		//	var result = new CbsSelectionLineMovedEventArgs(colorBandIndex, -1, operation: CbsSelectionLineDragOperation.Cancel);

		//	return result;
		//}
	}

	public enum CbsSelectionLineDragOperation
	{
		Move = 0,
		Complete = 1,
		Cancel = 2
	}

}
