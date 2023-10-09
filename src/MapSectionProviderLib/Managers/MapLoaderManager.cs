using MSS.Common;
using MSS.Common.MSet;
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
		#region Private Fields

		private readonly CancellationTokenSource _cts;
		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requests;
		private readonly ReaderWriterLockSlim _requestsLock;

		private readonly Task _removeCompletedRequestsTask;

		private const int PRECSION_PADDING = 4;
		private const int MIN_LIMB_COUNT = 1;

		private int _currentPrecision;
		private int _currentLimbCount;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public MapLoaderManager(MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_cts = new CancellationTokenSource();
			_mapSectionBuilder = new MapSectionBuilder();
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requests = new List<GenMapRequestInfo>();

			_requestsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			_removeCompletedRequestsTask = Task.Run(() => RemoveCompletedRequests(_requests, _requestsLock, _cts.Token), _cts.Token);
		}

		#endregion

		#region Public Properties

		public event EventHandler<JobProgressInfo>? RequestAdded;

		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		//public long NumberOfCountValSwitches => _mapSectionBuilder.NumberOfCountValSwitches;

		#endregion

		#region Public Methods

		public int GetNextJobNumber() => _mapSectionRequestProcessor.GetNextJobNumber();

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);

			var limbCount = GetLimbCount(precision);

			var mapLoaderJobNumber = GetNextJobNumber();

			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset,
				precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesXZero);

			return msrJob;
		}

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId, VectorLong mapBlockOffset, int precision, MapCalcSettings mapCalcSettings, bool crossesXZero)
		{
			var limbCount = GetLimbCount(precision);
			var mapLoaderJobNumber = GetNextJobNumber();
			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, subdivision, originalSourceSubdivisionId, mapBlockOffset,	precision, limbCount, mapCalcSettings, crossesXZero);

			return msrJob;
		}

		public List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration)
		{
			var mapLoaderJobNumber = msrJob.MapLoaderJobNumber;
			var result = FetchResponses(mapSectionRequests);

			if (result.Count != mapSectionRequests.Count)
			{
				var requestsNotFound = mapSectionRequests.Where(x => !x.FoundInRepo).ToList();
				CheckNewRequestsForCancellation(requestsNotFound);

				_requestsLock.EnterWriteLock();

				try
				{
					//	CreateNewGenMapRequestInfo(mapLoaderJobNumber, requestsNotFound, callback, _mapSectionRequestProcessor, _cts.Token);
					var genMapRequestInfo = new GenMapRequestInfo(mapLoaderJobNumber, callback, _mapSectionRequestProcessor, requestsNotFound, _cts.Token);
					_requests.Add(genMapRequestInfo);
					genMapRequestInfo.MapSectionLoaded += GenMapRequestInfo_MapSectionLoaded;
				}
				finally
				{
					_requestsLock.ExitWriteLock();
				}

				pendingGeneration = requestsNotFound;
			}
			else
			{
				pendingGeneration = new List<MapSectionRequest>();
			}

			RequestAdded?.Invoke(this, new JobProgressInfo(mapLoaderJobNumber, "temp", DateTime.Now, mapSectionRequests.Count, result.Count));

			return result;
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

		public int GetPendingRequests(int jobNumber)
		{
			var result = DoWithReadLock(() =>
			{
				var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

				if (request != null)
				{
					return request.GetNumberOfRequestsPending();
				}
				else
				{
					return 0;
				}
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

		public void StopJobs(List<int> jobNumbers)
		{
			DoWithWriteLock(() =>
			{
				StopCurrentJobsInternal(jobNumbers);
			});

		}

		public int GetLimbCount(int precision)
		{
			if (precision != _currentPrecision)
			{
				var adjustedPrecision = precision + PRECSION_PADDING;
				var apFixedPointFormat = new ApFixedPointFormat(RMapConstants.BITS_BEFORE_BP, minimumFractionalBits: adjustedPrecision);

				var adjustedLimbCount = Math.Max(apFixedPointFormat.LimbCount, MIN_LIMB_COUNT);

				if (_currentLimbCount == adjustedLimbCount)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount remains the same at {adjustedLimbCount}.");
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Calculating the LimbCount. CurrentPrecision = {_currentPrecision}, new precision = {precision}. LimbCount is being updated to {adjustedLimbCount}.");
				}

				_currentLimbCount = adjustedLimbCount;
				_currentPrecision = precision;
			}

			return _currentLimbCount;
		}

		#endregion

		#region Private Methods

		private List<MapSection> FetchResponses(List<MapSectionRequest> mapSectionRequests)
		{
			var result = new List<MapSection>();

			var requestResponsePairs = _mapSectionRequestProcessor.FetchResponses(mapSectionRequests);

			foreach (var requestResponsePair in requestResponsePairs)
			{
				var request = requestResponsePair.Item1;
				var response = requestResponsePair.Item2;

				if (response.MapSectionVectors != null)
				{
					var mapSection = _mapSectionBuilder.CreateMapSection(request, response.MapSectionVectors);
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

		private void StopCurrentJobInternal(int jobNumber)
		{
			var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

			if (request != null)
			{
				request.Stop();
			}
		}

		private void StopCurrentJobsInternal(List<int> jobNumbers)
		{
			var requestsToStop = _requests.Where(x => jobNumbers.Contains(x.JobNumber));

			foreach(var request in requestsToStop)
			{
				request.Stop();
			}
		}

		private void RemoveCompletedRequests(List<GenMapRequestInfo> requestInfos, ReaderWriterLockSlim requestsLock, CancellationToken ct)
		{
			var timeToWait = TimeSpan.FromSeconds(140);
			var timeToWarn = TimeSpan.FromMinutes(3);

			var countToWarn = 0;

			try
			{
				var requestInfosToBeDisposed = new List<GenMapRequestInfo>();

				while (!ct.IsCancellationRequested)
				{
					Thread.Sleep(5 * 1000); // TODO: Can the RemoveCompletedRequests background thread be made avoid calls to Thread.Sleep.
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
									requestInfo.MarkJobAsComplete();
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

		[Conditional("DEBUG")]
		private void CheckNewRequestsForCancellation(List<MapSectionRequest> requestsNotFound)
		{
			if (requestsNotFound.Any(x => x.CancellationTokenSource.IsCancellationRequested))
			{
				Debug.WriteLine("MapLoaderManager: At least one MapSectionRequest is Cancelled.");
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

		#region Old code

		//public List<MapSection> Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, Action<MapSection> callback, out int jobNumber)
		//{
		//	var mapSectionRequests = _mapSectionBuilder.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, mapCalcSettings);
		//	var result = Push(mapSectionRequests, callback, out jobNumber);
		//	return result;
		//}

		//public List<MapSection> Push(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections, 
		//	Action<MapSection> callback, out int jobNumber, out IList<MapSection> mapSectionsPendingGeneration)
		//{
		//	Debug.WriteLine($"MapLoaderManager: Creating MapSections with SaveTheZValues: {mapCalcSettings.SaveTheZValues} and CalculateEscapeVelocities: {mapCalcSettings.CalculateEscapeVelocities}.");

		//	var mapSectionRequests = _mapSectionBuilder.CreateSectionRequestsFromMapSections(jobType, jobId, jobOwnerType, mapAreaInfo, mapCalcSettings, emptyMapSections);
		//	var result = Push(mapSectionRequests, callback, out jobNumber, out var pendingGeneration);

		//	mapSectionsPendingGeneration = new List<MapSection>();

		//	foreach(var mapSectionRequest in pendingGeneration)
		//	{
		//		//var mapSectionPending = emptyMapSections[mapSectionRequest.RequestNumber];
		//		var mapSectionPending = emptyMapSections.FirstOrDefault(x => x.RequestNumber == mapSectionRequest.RequestNumber);

		//		if (mapSectionPending != null)
		//		{
		//			mapSectionPending.JobNumber = jobNumber;
		//			mapSectionsPendingGeneration.Add(mapSectionPending);
		//		}
		//	}

		//	return result;
		//}

		//public void CancelRequests(IList<MapSection> sectionsToCancel)
		//{
		//	DoWithWriteLock(() =>
		//	{
		//		CancelRequestsInternal(sectionsToCancel);
		//	});
		//}

		//public void CancelRequests(IList<MapSectionRequest> requestsToCancel)
		//{
		//	DoWithWriteLock(() =>
		//	{
		//		CancelRequestsInternal(requestsToCancel);
		//	});
		//}

		//private void CancelRequestsInternal(IList<MapSection> sectionsToCancel)
		//{
		//	foreach (var section in sectionsToCancel)
		//	{
		//		var genMapRequestInfo = _requests.FirstOrDefault(x => x.JobNumber == section.JobNumber);

		//		if (genMapRequestInfo != null)
		//		{
		//			genMapRequestInfo.MapLoader.CancelRequest(section);
		//		}
		//		else
		//		{
		//			Debug.WriteLine($"MapLoaderManager::CancelRequestsInternal. Could not MapLoader Job with JobNumber: {section.JobNumber}.");
		//		}
		//	}
		//}

		//private void CancelRequestsInternal(IList<MapSectionRequest> requestsToCancel)
		//{
		//	foreach (var request in requestsToCancel)
		//	{
		//		var genMapRequestInfo = _requests.FirstOrDefault(x => x.JobNumber == request.MapLoaderJobNumber);

		//		if (genMapRequestInfo != null)
		//		{
		//			genMapRequestInfo.MapLoader.CancelRequest(request);
		//		}
		//		else
		//		{
		//			Debug.WriteLine($"MapLoaderManager::CancelRequestsInternal. Could not MapLoader Job with JobNumber: {request.MapLoaderJobNumber}.");
		//		}
		//	}
		//}

		#endregion
	}

	internal class GenMapRequestInfo 
	//: IDisposable
	{
		private readonly CancellationToken _ct;
		//private readonly Task? _onCompletedTask;

		private MapLoader _mapLoader;


		#region Constructor

		/*
			var mapLoader = new MapLoader(mapLoaderJobNumber, callback, mapSectionRequestProcessor);
			var startTask = mapLoader.Start(requestsNotFound);
			var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask, ct);

		*/

		//public GenMapRequestInfo(MapLoader mapLoader, Task task, CancellationToken ct)
		public GenMapRequestInfo(int mapLoaderJobNumber, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor,
			List<MapSectionRequest> requestsNotFound, CancellationToken ct)
		{
			_mapLoader = new MapLoader(mapLoaderJobNumber, callback, mapSectionRequestProcessor);
			Task = _mapLoader.Start(requestsNotFound);

			JobNumber = _mapLoader.JobNumber;

			_ct = ct;

			TaskStartedDate = DateTime.UtcNow;

			if (Task.IsCompleted)
			{
				TaskCompletedDate = DateTime.UtcNow;
				//_onCompletedTask = null;
			}
			else
			{
				//_onCompletedTask = task.ContinueWith(TaskCompleted, _ct);
				_ = Task.ContinueWith(TaskCompleted, _ct);
			}

			_mapLoader.SectionLoaded += MapLoader_SectionLoaded;
		}

		#endregion

		#region Public Properties

		public int JobNumber { get; init; }

		public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;

		public Task Task { get; init; }

		public DateTime TaskStartedDate { get; init; }
		public DateTime? TaskCompletedDate { get; private set; }

		public TimeSpan TotalExecutionTime => _mapLoader.ElaspedTime;

		#endregion


		#region Public Methods

		public void MarkJobAsComplete()
		{
			_mapLoader.MarkJobAsComplete();
		}

		public void Stop()
		{
			_mapLoader.Stop();
		}

		public int GetNumberOfRequestsPending()
		{
			var result = _mapLoader.SectionsRequested - _mapLoader.SectionsCompleted;

			return result;
		}

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


		//private GenMapRequestInfo CreateNewGenMapRequestInfo(int mapLoaderJobNumber, List<MapSectionRequest> requestsNotFound, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor, CancellationToken ct)
		//{
		//	var mapLoader = new MapLoader(mapLoaderJobNumber, callback, mapSectionRequestProcessor);
		//	var startTask = mapLoader.Start(requestsNotFound);
		//	var genMapRequestInfo = new GenMapRequestInfo(mapLoader, startTask, ct);

		//	return genMapRequestInfo;
		//}

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
