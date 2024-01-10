using System;

namespace MSetExplorer
{
	public class CbsSectionLineMovedEventArgs : EventArgs
	{
		public int ColorBandIndex { get; init; }
		public double NewCutoff { get; init; }
		public bool UpdatingPrevious { get; init; }
		public CbsSectionLineDragOperation Operation { get; init; }

		public CbsSectionLineMovedEventArgs(int colorBandIndex, double newCutoff, bool updatingPrevious, CbsSectionLineDragOperation operation)
		{
			ColorBandIndex = colorBandIndex;
			NewCutoff = newCutoff;
			UpdatingPrevious = updatingPrevious;
			Operation = operation;
		}
	}

	public enum CbsSectionLineDragOperation
	{
		Move = 0,
		Complete = 1,
		Cancel = 2,
		NotStarted = 3
	}

}
