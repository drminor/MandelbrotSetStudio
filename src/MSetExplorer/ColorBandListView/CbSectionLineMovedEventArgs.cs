using System;

namespace MSetExplorer
{
	public class CbSectionLineMovedEventArgs : EventArgs
	{
		public int ColorBandIndex { get; init; }
		public double NewCutoff { get; init; }
		public bool UpdatingPrevious { get; init; }
		public CbSectionLineDragOperation Operation { get; init; }

		public CbSectionLineMovedEventArgs(int colorBandIndex, double newCutoff, bool updatingPrevious, CbSectionLineDragOperation operation)
		{
			ColorBandIndex = colorBandIndex;
			NewCutoff = newCutoff;
			UpdatingPrevious = updatingPrevious;
			Operation = operation;
		}
	}

	public enum CbSectionLineDragOperation
	{
		Started,
		Move,
		Complete,
		Cancel,
		NotStarted
	}

}
