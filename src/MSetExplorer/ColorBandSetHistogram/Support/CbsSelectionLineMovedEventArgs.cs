using System;

namespace MSetExplorer
{
	public class CbsSelectionLineMovedEventArgs : EventArgs
	{
		public int ColorBandIndex { get; init; }
		public double NewCutoff { get; init; }
		public bool UpdatingPrevious { get; init; }
		public CbsSelectionLineDragOperation Operation { get; init; }

		public CbsSelectionLineMovedEventArgs(int colorBandIndex, double newCutoff, bool updatingPrevious, CbsSelectionLineDragOperation operation)
		{
			ColorBandIndex = colorBandIndex;
			NewCutoff = newCutoff;
			UpdatingPrevious = updatingPrevious;
			Operation = operation;
		}
	}

	public enum CbsSelectionLineDragOperation
	{
		Move = 0,
		Complete = 1,
		Cancel = 2,
		NotStarted = 3
	}

}
