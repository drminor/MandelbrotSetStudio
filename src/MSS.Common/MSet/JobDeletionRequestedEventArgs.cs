using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public class JobDeletionRequestedEventArgs : EventArgs
	{
		public IList<Job> Jobs { get; init; }

		public JobDeletionRequestedEventArgs(IList<Job> jobs)
		{
			Jobs = jobs;
		}

		public JobDeletionRequestedEventArgs(Job job)
		{
			Jobs = new List<Job> { job };
		}
	}

}
