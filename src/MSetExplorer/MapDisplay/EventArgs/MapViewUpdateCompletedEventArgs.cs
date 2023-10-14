using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapViewUpdateCompletedEventArgs : EventArgs
	{
		public int JobNumber { get; init; }
		public bool IsCancelled { get; init; }

		public MapViewUpdateCompletedEventArgs(int jobNumber, bool isCancelled)
		{
			JobNumber = jobNumber;
			IsCancelled = isCancelled;	
		}

	}

}
