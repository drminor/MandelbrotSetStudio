using MEngineDataContracts;
using System;

namespace MapSectionProviderLib
{
	internal class WorkItem<T, U>
	{
		public int JobId { get; init; }
		public T Request { get; init; }
		public U? Response { get; set; }

		public Action<T, U?> WorkAction { get; init; }

		public WorkItem(int jobId, T request, Action<T, U?> workAction)
		{
			JobId = jobId;
			Request = request ?? throw new ArgumentNullException(nameof(request)); ;
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
		}

		public void RunWorkAction(U? response)
		{
			Response = response;
			RunWorkAction();
		}

		public void RunWorkAction()
		{
			WorkAction(Request, Response);
		}
	}

	internal class MapSectionWorkRequest : WorkItem<MapSectionServiceRequest, MapSectionServiceResponse>
	{
		public MapSectionWorkRequest(int jobId, MapSectionServiceRequest request, Action<MapSectionServiceRequest, MapSectionServiceResponse?> workAction) : base(jobId, request, workAction)
		{
		}
	}

	internal class MapSectionGenerateRequest : WorkItem<MapSectionWorkRequest, MapSectionServiceResponse>
	{
		public MapSectionGenerateRequest(int jobId, MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionServiceResponse?> workAction) : base(jobId, request, workAction)
		{
		}
	}
}
