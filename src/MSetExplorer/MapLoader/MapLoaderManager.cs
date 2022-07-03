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
		private readonly CancellationTokenSource _cts;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requests;
		private readonly ReaderWriterLockSlim _requestsLock;

		private readonly Task _removeCompletedRequestsTask;

		#region Constructor

		public MapLoaderManager(MapSectionHelper mapSectionHelper, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_cts = new CancellationTokenSource();
			_mapSectionHelper = mapSectionHelper;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requests = new List<GenMapRequestInfo>();

			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			_removeCompletedRequestsTask = Task.Run(() => RemoveCompletedRequests(_requests, _requestsLock, _cts.Token), _cts.Token);
		}

		#endregion

		#region Public Properties

		public event EventHandler<JobProgressInfo>? RequestAdded;

		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

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

				var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask, _cts.Token);
				_requests.Add(genMapRequestInfo);

				result = mapLoader.JobNumber;

				genMapRequestInfo.MapSectionLoaded += GenMapRequestInfo_MapSectionLoaded;

				RequestAdded?.Invoke(this, new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count));
			});

			return result;
		}

		private void GenMapRequestInfo_MapSectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			_requestsLock.EnterReadLock();

			try
			{
				SectionLoaded?.Invoke(this, e);
			}
			finally
			{
				_requestsLock.ExitReadLock();
			}
		}

		public Task? GetTaskForJob(int jobNumber)
		{
			Task? result = null;

			DoWithReadLock(() =>
			{
				var t = _requests.FirstOrDefault(x => x.JobNumber == jobNumber)?.Task;
				return t;
			});

			return result;
		}

		public void StopJob(int jobNumber)
		{
			DoWithWriteLock(() => 
			{
				StopCurrentJobInternal(jobNumber);
			});
		}

		#endregion

		#region Private Methods

		private void StopCurrentJobInternal(int jobNumber)
		{
			var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

			if (request != null)
			{
				request.MapLoader.Stop();
			}
		}

		private void RemoveCompletedRequests(List<GenMapRequestInfo> requestInfos, ReaderWriterLockSlim requestsLock, CancellationToken ct)
		{
			var timeToWait = TimeSpan.FromSeconds(20);
			var timeToWarn = TimeSpan.FromMinutes(3);

			var countToWarn = 0;

			try
			{
				var requestInfosToBeDisposed = new List<GenMapRequestInfo>();

				while (!ct.IsCancellationRequested)
				{
					requestsLock.EnterUpgradeableReadLock();

					try
					{
						//requestInfosToBeDisposed.Clear();
						Debug.Assert(requestInfosToBeDisposed.Count == 0, "RequestInfosToBeCleared is not empty.");
						var now = DateTime.UtcNow;

						foreach(var requestInfo in requestInfos)
						{
							var x = now - requestInfo.TaskCompletedDate;

							if (x > timeToWait)
							{
								requestInfosToBeDisposed.Add(requestInfo);
							}
							else
							{
								if (requestInfo.TaskStartedDate - now > timeToWarn)
								{
									countToWarn++;
								}
							}
						}

						if (requestInfosToBeDisposed.Count > 0)
						{
							requestsLock.EnterWriteLock();

							try
							{
								foreach(var requestInfo in requestInfosToBeDisposed)
								{
									_requests.Remove(requestInfo);
									requestInfo.Dispose();
								}
							}
							finally
							{
								requestsLock.ExitWriteLock();
								requestInfosToBeDisposed.Clear();
							}
						}
					}
					finally
					{
						requestsLock.ExitUpgradeableReadLock();

						if (countToWarn > 0)
						{
							Debug.WriteLine($"WARNING: There are {countToWarn} MapLoaderRequests running longer than {timeToWarn.TotalMinutes} minutes.");
						}
					}

				}
			}
			catch (TaskCanceledException)
			{

			}
			catch (Exception)
			{
				throw;
			}
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
					_mapSectionRequestProcessor.Dispose();

					_cts.Cancel();

					if (_removeCompletedRequestsTask.Wait(5 * 1000))
					{
						foreach (var genMapRequestInfo in _requests)
						{
							genMapRequestInfo.Dispose();
						}

						_removeCompletedRequestsTask.Dispose();
						_requestsLock.Dispose();
					}
					else
					{
						Debug.WriteLine($"The MapLoaderManager's RemoveCompletedRequestTask did not stop.");
					}
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

		private class GenMapRequestInfo : IDisposable
		{
			private readonly CancellationToken _ct;
			private readonly Task? _onCompletedTask;

			#region Constructor

			public GenMapRequestInfo(MapLoader mapLoader, Task task, CancellationToken ct)
			{
				_ct = ct;
				MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
				Task = task ?? throw new ArgumentNullException(nameof(task));
				TaskStartedDate = DateTime.UtcNow;

				if (task.IsCompleted)
				{
					TaskCompletedDate = DateTime.UtcNow;
					_onCompletedTask = null;
				}
				else
				{
					_onCompletedTask = task.ContinueWith(TaskCompleted, _ct);
				}

				MapLoader.SectionLoaded += MapLoader_SectionLoaded;
			}

			#endregion

			#region Public Properties

			public int JobNumber => MapLoader.JobNumber;

			public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;

			public MapLoader MapLoader { get; init; }
			public Task Task { get; init; }

			public DateTime TaskStartedDate { get; init; }
			public DateTime? TaskCompletedDate { get; private set; }

			#endregion

			#region Event Handlers and Private Methods

			private void MapLoader_SectionLoaded(object? sender, MapSectionProcessInfo e)
			{
				MapSectionLoaded?.Invoke(this, e);
			}

			private void TaskCompleted(Task task)
			{
				TaskCompletedDate = DateTime.UtcNow;
			}

			#endregion

			#region IDisposable Support

			private bool disposedValue;

			protected virtual void Dispose(bool disposing)
			{
				if (!disposedValue)
				{
					if (disposing)
					{
						// Dispose managed state (managed objects)
						//Task.Dispose();
						_onCompletedTask?.Dispose();
					}

					disposedValue = true;
				}
			}

			public void Dispose()
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}

			#endregion
		}
	}
}
