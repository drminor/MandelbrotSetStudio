using MSS.Types.MSet;
using System.Collections.Generic;

namespace MSetExplorer
{
	internal interface IMapLoaderJobStack
	{
		bool CanGoBack { get; }
		bool CanGoForward { get; }
		Job CurrentJob { get; }
		IEnumerable<GenMapRequestInfo> GenMapRequests { get; }

		bool GoBack();
		bool GoForward();

		void Push(Job job);
		void LoadJobStack(IEnumerable<Job> jobs);

		void UpdateJob(GenMapRequestInfo genMapRequestInfo, Job job);
	}
}