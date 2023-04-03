﻿using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapLoaderManager : IMapLoaderManager
	{
		private readonly CancellationTokenSource _cts;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requests;
		private readonly ReaderWriterLockSlim _requestsLock;

		private readonly Task _removeCompletedRequestsTask;

		#region Constructor

		public MapLoaderManager(MapSectionRequestProcessor mapSectionRequestProcessor, MapSectionHelper mapSectionHelper)
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

		public List<MapSection> Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, Action<MapSection> callback, out int jobNumber)
		{
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, mapCalcSettings);
			var result = Push(mapSectionRequests, callback, out jobNumber);
			return result;
		}

		public List<MapSection> Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections, Action<MapSection> callback, out int jobNumber)
		{
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequestsFromMapSections(ownerId, jobOwnerType, mapAreaInfo, mapCalcSettings, emptyMapSections);
			var result = Push(mapSectionRequests, callback, out jobNumber);
			return result;
		}

		public List<MapSection> Push(List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out int jobNumber)
		{
			var result = FetchResponses(mapSectionRequests, out jobNumber);

			if (result.Count != mapSectionRequests.Count)
			{
				var requestsNotFound = mapSectionRequests.Where(x => !x.FoundInRepo).ToList();

				var mapLoader = new MapLoader(jobNumber, callback, _mapSectionRequestProcessor);

				DoWithWriteLock(() =>
				{
					var startTask = mapLoader.Start(requestsNotFound);

					var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask, _cts.Token);
					_requests.Add(genMapRequestInfo);

					genMapRequestInfo.MapSectionLoaded += GenMapRequestInfo_MapSectionLoaded;
				});
			}

			RequestAdded?.Invoke(this, new JobProgressInfo(jobNumber, "temp", DateTime.Now, mapSectionRequests.Count, result.Count));

			return result;
		}

		private List<MapSection> FetchResponses(List<MapSectionRequest> mapSectionRequests, out int jobNumber)
		{
			var result = new List<MapSection>();

			jobNumber = _mapSectionRequestProcessor.GetNextRequestId();
			var requestResponsePairs = _mapSectionRequestProcessor.FetchResponses(mapSectionRequests);

			foreach (var requestResponsePair in requestResponsePairs)
			{
				var request = requestResponsePair.Item1;
				var response = requestResponsePair.Item2;

				if (response.MapSectionVectors != null)
				{
					var mapSection = _mapSectionHelper.CreateMapSection(request, response.MapSectionVectors, jobNumber);
					result.Add(mapSection);
				}
			}

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
			var result = DoWithReadLock(() =>
			{
				var t = _requests.FirstOrDefault(x => x.JobNumber == jobNumber)?.Task;
				return t;
			});

			return result;
		}

		public TimeSpan? GetExecutionTimeForJob(int jobNumber)
		{
			var result = DoWithReadLock(() =>
			{
				var t = _requests.FirstOrDefault(x => x.JobNumber == jobNumber)?.TotalExecutionTime;
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
			var timeToWait = TimeSpan.FromSeconds(40);
			var timeToWarn = TimeSpan.FromMinutes(3);

			var countToWarn = 0;

			try
			{
				var requestInfosToBeDisposed = new List<GenMapRequestInfo>();

				while (!ct.IsCancellationRequested)
				{
					Thread.Sleep(5 * 1000);
					 requestsLock.EnterUpgradeableReadLock();

					try
					{
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
								if (now - requestInfo.TaskStartedDate > timeToWarn)
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
									requestInfo.MapLoader.MarkJobAsComplete();
									_requests.Remove(requestInfo);
									//requestInfo.Dispose();
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
							countToWarn = 0;
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
						//foreach (var genMapRequestInfo in _requests)
						//{
						//	genMapRequestInfo.Dispose();
						//}

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

		private class GenMapRequestInfo //: IDisposable
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

			public TimeSpan TotalExecutionTime => MapLoader.ElaspedTime;

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

			//#region IDisposable Support

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

			//#endregion
		}
	}
}