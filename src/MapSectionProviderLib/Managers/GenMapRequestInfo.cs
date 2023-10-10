using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	internal class GenMapRequestInfo //: IDisposable
	{
		private readonly CancellationToken _ct;

		private readonly List<MapSectionRequest> _mapSectionRequests;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly Action<MapSectionRequest, MapSection> _callback;

		//private readonly Task? _onCompletedTask;
		//private MapLoader _mapLoader;

		private IMsrJob _msrJob;

		#region Constructor

		//public GenMapRequestInfo(MapLoader mapLoader, Task task, CancellationToken ct)

		public GenMapRequestInfo(MsrJob msrJob, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor,
			List<MapSectionRequest> requestsNotFound, CancellationToken ct)
		{
			_ct = ct;
			_mapSectionRequests = requestsNotFound;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_msrJob = msrJob;
			_callback = msrJob.HandleResponse;

			JobNumber = msrJob.MapLoaderJobNumber;

			//_mapLoader = new MapLoader(mapLoaderJobNumber, callback, mapSectionRequestProcessor);
			//Task = _mapLoader.Start(requestsNotFound);
			//SubmitSectionRequests(requestsNotFound, mapSectionRequestProcessor, msrJob.HandleResponse);

			_ = Task.Run(SubmitSectionRequests);

			_msrJob.Start(requestsNotFound, callback, requestsNotFound.Count);

			TaskStartedDate = DateTime.UtcNow;

			//if (Task.IsCompleted)
			//{
			//	TaskCompletedDate = DateTime.UtcNow;
			//	//_onCompletedTask = null;
			//}
			//else
			//{
			//	//_onCompletedTask = task.ContinueWith(TaskCompleted, _ct);
			//	_ = Task.ContinueWith(TaskCompleted, _ct);
			//}

			//_mapLoader.SectionLoaded += MapLoader_SectionLoaded;
			_msrJob.MapSectionLoaded += SectionLoaded;
		}

		#endregion

		#region Public Properties

		public int JobNumber { get; init; }

		public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;

		//public Task Task { get; init; }

		public DateTime TaskStartedDate { get; init; }
		public DateTime? TaskCompletedDate { get; private set; }

		public TimeSpan TotalExecutionTime => _msrJob.ElaspedTime;

		#endregion

		#region Public Methods

		public void MarkJobAsComplete()
		{
			//_mapLoader.MarkJobAsComplete();
		}

		public void Stop()
		{
			//_mapLoader.Stop();
		}

		public int GetNumberOfRequestsPending()
		{
			//var result = _mapLoader.SectionsRequested - _mapLoader.SectionsCompleted;

			var result = 0;
			return result;
		}

		#endregion

		#region Event Handlers and Private Methods

		private void SectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			MapSectionLoaded?.Invoke(this, e);
		}

		private void TaskCompleted(Task task)
		{
			TaskCompletedDate = DateTime.UtcNow;
		}

		#endregion

		#region Private Methods

		//private int Start(List<MapSectionRequest> mapSectionRequests, MapSectionRequestProcessor mapSectionRequestProcessor)
		//{
		//	var numSectionsRequested = 0;

		//	foreach (var mapSectionRequest in mapSectionRequests)
		//	{
		//		numSectionsRequested++;

		//		if (mapSectionRequest.Mirror != null)
		//		{
		//			numSectionsRequested++;
		//		}
		//	}

		//	return numSectionsRequested;
		//}

		private int SubmitSectionRequests()
		{

			var numSectionsRequested = 0;

			foreach (var mapSectionRequest in _mapSectionRequests)
			{
				//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");

				if (!mapSectionRequest.CancellationTokenSource.IsCancellationRequested)
				{
					mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
					numSectionsRequested++;

					if (mapSectionRequest.Mirror != null)
					{
						numSectionsRequested++;
					}

					_mapSectionRequestProcessor.AddWork(mapSectionRequest, _callback);
					mapSectionRequest.Sent = true;
				}
				else
				{
					var msg = $"The MapLoader is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
					msg += "MapSectionRequest's Cancellation Token is cancelled.";
					Debug.WriteLine($"{msg}");
				}
			}

			return numSectionsRequested;
		}


		#endregion

		//private GenMapRequestInfo CreateNewGenMapRequestInfo(int mapLoaderJobNumber, List<MapSectionRequest> requestsNotFound, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor, CancellationToken ct)
		//{
		//	var mapLoader = new MapLoader(mapLoaderJobNumber, callback, mapSectionRequestProcessor);
		//	var startTask = mapLoader.Start(requestsNotFound);
		//	var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask, ct);

		//	return genMapRequestInfo;
		//}

		#region IDisposable Support

		//private bool disposedValue;

		//protected virtual void Dispose(bool disposing)
		//{
		//	if (!disposedValue)
		//	{
		//		if (disposing)
		//		{
		//			// Dispose managed state (managed objects)

		//			//if (Task != null)
		//			//{
		//			//	if (Task.IsCompleted)
		//			//	{
		//			//		Task.Dispose();
		//			//	}
		//			//	else
		//			//	{
		//			//		Debug.WriteLine($"The Task is not null and not completed as the GenMapRequestInfo is being disposed.");
		//			//	}
		//			//}

		//			//if (_onCompletedTask != null)
		//			//{
		//			//	if (_onCompletedTask.IsCompleted)
		//			//	{
		//			//		_onCompletedTask.Dispose();
		//			//	}
		//			//	else
		//			//	{
		//			//		Debug.WriteLine($"The onCompletedTask is not null and not completed as the GenMapRequestInfo is being disposed.");
		//			//	}
		//			//}
		//		}

		//		disposedValue = true;
		//	}
		//}

		//public void Dispose()
		//{
		//	Dispose(disposing: true);
		//	GC.SuppressFinalize(this);
		//}

		#endregion
	}
}
