﻿using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MapSectionProviderLib
{
	internal class WorkItem<T, U>
	{
		public int JobId { get; init; }
		public T Request { get; init; }
		public U? Response { get; set; }

		public Action<T, U> WorkAction { get; init; }

		public WorkItem(int jobId, T request, Action<T, U> workAction)
		{
			JobId = jobId;
			Request = request ?? throw new ArgumentNullException(nameof(request));
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));
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
	}

	internal class MapSectionWorkRequest : WorkItem<MapSectionRequest, MapSection>
	{
		public MapSectionWorkRequest(int jobId, MapSectionRequest request, Action<MapSectionRequest, MapSection> workAction)
			: base(jobId, request, workAction)
		{
		}
	}

	internal class MapSectionGenerateRequest : WorkItem<MapSectionWorkRequest, MapSectionResponse>
	{
		public MapSectionGenerateRequest(int jobId, MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionResponse> workAction, MapSectionResponse mapSectionResponse)
			: this(jobId, request, workAction)
		{
			Response = mapSectionResponse;
		}

		public MapSectionGenerateRequest(int jobId, MapSectionWorkRequest request, Action<MapSectionWorkRequest, MapSectionResponse> workAction)
			: base(jobId, request, workAction)
		{ }
	}

	internal class MapSectionPersistRequest 
	{
		public MapSectionRequest Request { get; init; }
		public MapSectionResponse Response { get; init; }
		public bool OnlyInsertJobMapSectionRecord { get; init; }

		public MapSectionPersistRequest(MapSectionRequest request, MapSectionResponse response)
			: this(request, response, onlyInsertJobMapSectionRecord: false)
		{ }

		public MapSectionPersistRequest(MapSectionRequest request, MapSectionResponse response, bool onlyInsertJobMapSectionRecord)
		{
			Request = request ?? throw new ArgumentNullException(nameof(request));
			Response = response ?? throw new ArgumentNullException(nameof(response));
			OnlyInsertJobMapSectionRecord = onlyInsertJobMapSectionRecord;
		}

	}
}
