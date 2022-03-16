using MapSectionProviderLib;
using MSS.Common;
using MSS.Types.MSet;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoaderManager : IMapLoaderManager, IDisposable
	{
		private readonly SynchronizationContext _synchronizationContext;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requests;
		private int _requestsPointer;
		private readonly ReaderWriterLockSlim _requestsLock;

		#region Constructor

		public MapLoaderManager(MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_synchronizationContext = SynchronizationContext.Current;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requests = new List<GenMapRequestInfo>();
			_requestsPointer = -1;

			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		public event EventHandler<MapSection> MapSectionReady;

		#region Public Properties

		private GenMapRequestInfo CurrentRequest => DoWithReadLock(() => { return (_requestsPointer == -1 || _requestsPointer > _requests.Count - 1) ? null : _requests[_requestsPointer]; });

		#endregion

		#region Public Methods

		public void Push(Job job)
		{
			Push(job, null);
		}

		public void Push(Job job, IList<MapSection> emptyMapSections)
		{
			DoWithWriteLock(() =>
			{
				StopCurrentJobInternal();

				var request = new MapLoader(job.MapBlockOffset, new ColorMap(job.MSetInfo.ColorBandSet), HandleMapSection, _mapSectionRequestProcessor);
				var mapSectionRequests = MapSectionHelper.CreateSectionRequests(job, emptyMapSections);
				var startTask = request.Start(mapSectionRequests);

				_requests.Add(new GenMapRequestInfo(request, startTask));
				_requestsPointer = _requests.Count - 1;
				_ = startTask?.ContinueWith(MapLoaderComplete);
			});
		}

		public void StopCurrentJob()
		{
			DoWithWriteLock(StopCurrentJobInternal);
		}

		#endregion

		#region Event Handlers

		private void HandleMapSection(object sender, MapSection mapSection)
		{
			DoWithWriteLock(() =>
			{
				var currentRequest = CurrentRequest;

				if (currentRequest == null)
				{
					Debug.WriteLine($"HandleMapSection cannot handle the new section there is no current request.");
				}
				else
				{
					var jobNumber = (sender as MapLoader)?.JobNumber ?? -1;

					if (jobNumber == currentRequest.JobNumber)
					{
						_synchronizationContext.Post(o => MapSectionReady?.Invoke(this, mapSection), null);
					}
					else
					{
						Debug.WriteLine($"HandleMapSection is ignoring the new section for job with jobNumber: {jobNumber}. CurJobNum: {currentRequest.JobNumber}");
					}
				}
			});
		}

		#endregion

		#region Private Methods

		private void StopCurrentJobInternal()
		{
			CurrentRequest?.MapLoader.Stop();
		}

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
			public MapLoader MapLoader { get; init; }
			public Task Task { get; init; }

			public GenMapRequestInfo(MapLoader mapLoader, Task task)
			{
				MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
				Task = task ?? throw new ArgumentNullException(nameof(task));
			}

			public int JobNumber => MapLoader.JobNumber;

		}
	}
}
