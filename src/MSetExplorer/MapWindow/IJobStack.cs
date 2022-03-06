using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobStack
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		event EventHandler CurrentJobChanged;

		IEnumerable<Job> Jobs { get; }
		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool GoBack();
		bool GoForward();

		void Push(Job job);

		void LoadJobStack(IEnumerable<Job> jobs);
		void UpdateJob(Job oldJob, Job newJob);
	}
}