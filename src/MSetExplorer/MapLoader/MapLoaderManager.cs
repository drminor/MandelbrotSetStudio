using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoaderManager : IMapLoaderManager
	{
		//private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requests;
		private int _requestsPointer;
		private readonly ReaderWriterLockSlim _requestsLock;

		#region Constructor

		public MapLoaderManager(MapSectionHelper mapSectionHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			//_synchronizationContext = SynchronizationContext.Current;
			_mapSectionHelper = mapSectionHelper;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requests = new List<GenMapRequestInfo>();
			_requestsPointer = -1;

			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		#region Public Properties

		public event EventHandler<JobProgressInfo>? RequestAdded;

		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		//private GenMapRequestInfo? CurrentRequest => DoWithReadLock(() => { return (_requestsPointer == -1 || _requestsPointer > _requests.Count - 1) ? null : _requests[_requestsPointer]; });

		public long NumberOfCountValSwitches => _mapSectionHelper.NumberOfCountValSwitches;

		#endregion

		#region Public Methods

		public int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, Action<MapSection, int, bool> callback)
		{
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(jobAreaAndCalcSettings);
			var result = Push(jobAreaAndCalcSettings.JobAreaInfo.MapBlockOffset, mapSectionRequests, callback);
			return result;
		}

		public int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection>? emptyMapSections, Action<MapSection, int, bool> callback)
		{
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(jobAreaAndCalcSettings, emptyMapSections);
			var result = Push(jobAreaAndCalcSettings.JobAreaInfo.MapBlockOffset, mapSectionRequests, callback);
			return result;
		}

		public int Push(BigVector mapBlockOffset, IList<MapSectionRequest> mapSectionRequests, Action<MapSection, int, bool> callback)
		{
			var result = 0;

			DoWithWriteLock(() =>
			{
				var mapLoader = new MapLoader(mapBlockOffset, callback, _mapSectionHelper, _mapSectionRequestProcessor);
				var startTask = mapLoader.Start(mapSectionRequests);

				var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask);
				_requests.Add(genMapRequestInfo);
				_requestsPointer = _requests.Count - 1;
				_ = startTask?.ContinueWith(MapLoaderComplete);

				result = mapLoader.JobNumber;

				genMapRequestInfo.MapSectionLoaded += GenMapRequestInfo_MapSectionLoaded;

				RequestAdded?.Invoke(this, new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count));
			});

			return result;
		}

		private void GenMapRequestInfo_MapSectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			SectionLoaded?.Invoke(this, e);
		}

		public Task? GetTaskForJob(int jobNumber)
		{
			var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);
			return request?.Task;
		}

		public void StopJob(int jobNumber)
		{
			DoWithWriteLock(() => 
			{
				StopCurrentJobInternal(jobNumber);
			});
		}

		#endregion

		//#region Event Handlers

		//private void HandleMapSection(object sender, Tuple<MapSection, int> mapSectionAndJobNumber)
		//{
		//	DoWithWriteLock(() =>
		//	{
		//		var currentRequest = CurrentRequest;

		//		if (currentRequest == null)
		//		{
		//			Debug.WriteLine($"HandleMapSection cannot handle the new section there is no current request.");
		//		}
		//		else
		//		{
		//			// TODO: Compare the JobNumber with all active Job numbers.
		//			//var jobNumber = (sender as MapLoader)?.JobNumber ?? -1;

		//			//if (jobNumber == currentRequest.JobNumber)
		//			//{
		//			//	_synchronizationContext?.Post(o => MapSectionReady?.Invoke(this, mapSectionAndJobNumber), null);
		//			//}
		//			//else
		//			//{
		//			//	Debug.WriteLine($"HandleMapSection is ignoring the new section for job with jobNumber: {jobNumber}. CurJobNum: {currentRequest.JobNumber}");
		//			//}

		//			_synchronizationContext?.p .Post(o => MapSectionReady?.Invoke(this, mapSectionAndJobNumber), null);
		//		}
		//	});
		//}

		//#endregion

		#region Private Methods

		private void StopCurrentJobInternal(int jobNumber)
		{
			var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

			if (request != null)
			{
				request.MapLoader.Stop();
			}
		}

		// TODO: Create a scheduled task to remove completed MapLoader instances.
		private void MapLoaderComplete(Task task)
		{
		//	Thread.Sleep(5 * 1000);

		//	DoWithWriteLock(() =>
		//	{
		//		var genMapRequestInfo = _requests.FirstOrDefault(x => x.Task == task);
		//		if (!_requests.Remove(genMapRequestInfo))
		//		{
		//			Debug.WriteLine($"The MapLoaderManager could not remove the request in the MapLoaderComplete action.");
		//		}
		//	});
		}

		#endregion

		#region Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_requestsLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_requestsLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_requestsLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_requestsLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_requestsLock.Dispose();
					_mapSectionRequestProcessor.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion

		private class GenMapRequestInfo
		{
			public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;

			public MapLoader MapLoader { get; init; }
			public Task Task { get; init; }

			public GenMapRequestInfo(MapLoader mapLoader, Task task)
			{
				MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
				Task = task ?? throw new ArgumentNullException(nameof(task));

				MapLoader.SectionLoaded += MapLoader_SectionLoaded;
			}

			private void MapLoader_SectionLoaded(object? sender, MapSectionProcessInfo e)
			{
				MapSectionLoaded?.Invoke(this, e);
			}

			public int JobNumber => MapLoader.JobNumber;

		}
	}
}
