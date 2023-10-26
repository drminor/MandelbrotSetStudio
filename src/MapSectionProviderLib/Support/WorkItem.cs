using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MapSectionProviderLib
{
	internal class WorkItem<T, U> : IWorkRequest
	{
		public int JobNumber { get; init; }
		public T Request { get; init; }
		public U? Response { get; set; }
		public virtual bool JobIsCancelled { get; protected set; }

		public Action<T, U> WorkAction { get; init; }

		public WorkItem(int jobNumber, T request, Action<T, U> workAction) : this(jobNumber, request, workAction, jobIsCancelled: false)
		{ }

		public WorkItem(int jobNumber, T request, Action<T, U> workAction, bool jobIsCancelled)
		{
			JobNumber = jobNumber;
			Request = request ?? throw new ArgumentNullException(nameof(request));
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
			JobIsCancelled = jobIsCancelled;
		}

		public void RunWorkAction(U response)
		{
			Response = response;
			WorkAction(Request, Response);
		}

		//public void RunWorkAction()
		//{
		//	WorkAction(Request, Response, JobId);
		//}

		public override string ToString()
		{
			return Response == null
				? $"WorkItem: {JobNumber} for request: {Request} /w null response"
				: $"WorkItem: {JobNumber} for request: {Request} and response: {Response}";
		}
	}

	internal class MapSectionWorkRequest : WorkItem<MapSectionRequest, MapSection>
	{
		public MapSectionWorkRequest(MapSectionRequest request, Action<MapSectionRequest, MapSection> workAction, MapSection mapSection)
			: this(request, workAction)
		{
			Response = mapSection;
		}

		public MapSectionWorkRequest(MapSectionRequest request, Action<MapSectionRequest, MapSection> workAction)
			: base(request.MapLoaderJobNumber, request, workAction)
		{
		}

		override public bool JobIsCancelled
		{
			get => Request.MsrJob.IsCancelled;
		}
	}

	internal class MapSectionGenerateRequest : WorkItem<MapSectionWorkRequest, MapSectionResponse>
	{
		public MapSectionGenerateRequest(MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionResponse> workAction, MapSectionResponse mapSectionResponse)
			: this(request, workAction)
		{
			Response = mapSectionResponse;
		}

		public MapSectionGenerateRequest(MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionResponse> workAction)
			: base(request.Request.MapLoaderJobNumber, request, workAction)
		{ }

		override public bool JobIsCancelled
		{
			get => Request.Request.MsrJob.IsCancelled;
		}
	}

	internal class MapSectionPersistRequest : WorkItem<MapSectionRequest, MapSectionResponse>
	{
		public bool OnlyInsertJobMapSectionRecord { get; init; }

		public MapSectionPersistRequest(MapSectionRequest request, MapSectionResponse response)
			: this(request, response, onlyInsertJobMapSectionRecord: false)
		{ }

		public MapSectionPersistRequest(MapSectionRequest request, MapSectionResponse? response, bool onlyInsertJobMapSectionRecord)
			: base(request.MapLoaderJobNumber, request, (request, response) => { })
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));

			OnlyInsertJobMapSectionRecord = onlyInsertJobMapSectionRecord;

			if (!onlyInsertJobMapSectionRecord)
			{
				Response = response ?? throw new ArgumentNullException(nameof(response));
			}
			else
			{
				Response = response;
			}
		}

		override public bool JobIsCancelled
		{
			get => Request.MsrJob.IsCancelled;
		}
	}

	public interface IWorkRequest
	{
		public int JobNumber { get; init; }
		public bool JobIsCancelled { get; }
	}
}
