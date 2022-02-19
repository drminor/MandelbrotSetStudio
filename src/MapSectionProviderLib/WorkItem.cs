using System;

namespace MapSectionProviderLib
{
	internal class WorkItem<T, U>
	{
		public int JobId { get; init; }
		public T Request { get; init; }
		public Action<T, U> WorkAction { get; init; }

		public WorkItem(int jobId, T request, Action<T, U> workAction)
		{
			JobId = jobId;
			Request = request ?? throw new ArgumentNullException(nameof(request)); ;
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
		}
	}
}
