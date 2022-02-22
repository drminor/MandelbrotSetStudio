using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapLoaderJobStack
	{
		event EventHandler CurrentJobChanged;
		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		IEnumerable<Job> Jobs { get; }

		bool GoBack();
		bool GoForward();

		void Push(Job job);
		void LoadJobStack(IEnumerable<Job> jobs);

		void UpdateJob(Job oldJob, Job newJob);
	}
}