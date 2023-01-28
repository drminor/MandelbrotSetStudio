using MEngineDataContracts;
using MSS.Types.MSet;
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

	internal class MapSectionWorkRequest : WorkItem<MapSectionRequest, MapSectionResponse>
	{
		public MapSectionWorkRequest(int jobId, MapSectionRequest request, Action<MapSectionRequest, MapSectionResponse?> workAction)
			: base(jobId, request, workAction)
		{
		}
	}

	internal class MapSectionGenerateRequest : WorkItem<MapSectionWorkRequest, MapSectionResponse>
	{
		public MapSectionGenerateRequest(int jobId, MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionResponse?> workAction)
			: base(jobId, request, workAction)
		{
		}
	}
}
