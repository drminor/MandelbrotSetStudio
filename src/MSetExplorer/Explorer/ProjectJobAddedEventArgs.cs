using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class ProjectJobAddedEventArgs : EventArgs
	{
		public Job Job { get; init; }

		public ProjectJobAddedEventArgs(Job job)
		{
			Job = job;
		}
	}


}
