using MEngineDataContracts;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapLoaderJobStack
	{
		event EventHandler CurrentJobChanged;

		event EventHandler<MapSection> MapSectionReady;

		Job CurrentJob { get; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		IEnumerable<Job> Jobs { get; }

		bool GoBack();
		bool GoForward();

		void Push(Job job);
		void Push(Job job, IList<MapSection> emptyMapSections);
		void LoadJobStack(IEnumerable<Job> jobs);

		void UpdateJob(Job oldJob, Job newJob);

		void StopCurrentJob();
	}
}