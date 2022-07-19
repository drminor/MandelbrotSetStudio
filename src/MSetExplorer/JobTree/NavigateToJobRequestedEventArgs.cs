using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class NavigateToJobRequestedEventArgs : EventArgs
	{
		public Job Job { get; init; }

		public NavigateToJobRequestedEventArgs(Job job)
		{
			Job = job;
		}
	}

}
