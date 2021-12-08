using System;

namespace MEngineClient
{
	public class WorkItem<T, U>
	{
		public T Request { get; init; }
		public Action<U> WorkAction { get; init; }

		public WorkItem(T request, Action<U> workAction)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request)); ;
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
		}
	}
}
