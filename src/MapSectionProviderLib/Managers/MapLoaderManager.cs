using MSS.Common;
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

		private int GetNextJobNumber() => _mapSectionRequestProcessor.GetNextJobNumber();

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings)
		{
			//// TODO: Calling GetBinaryPrecision is temporary until we can update all Job records with a 'good' value for precision.
			//var precision = RMapHelper.GetBinaryPrecision(mapAreaInfo);
			//var limbCount = GetLimbCount(precision);
			//var mapLoaderJobNumber = GetNextJobNumber();

			//var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(), mapAreaInfo.MapBlockOffset,
			//	precision, limbCount, mapCalcSettings, mapAreaInfo.Coords.CrossesXZero);

			var msrJob = CreateMapSectionRequestJob(jobType, jobId, jobOwnerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(),
				mapAreaInfo.MapBlockOffset, mapAreaInfo.Precision, mapAreaInfo.Coords.CrossesXZero, mapCalcSettings);

			return msrJob;
		}

		public MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId,
			VectorLong mapBlockOffset, int precision, bool crossesXZero, MapCalcSettings mapCalcSettings)
		{
			var limbCount = GetLimbCount(precision);
			var mapLoaderJobNumber = GetNextJobNumber();
			var msrJob = new MsrJob(mapLoaderJobNumber, jobType, jobId, jobOwnerType, subdivision, originalSourceSubdivisionId, mapBlockOffset,	precision, limbCount, mapCalcSettings, crossesXZero);

			return msrJob;
		}

		public List<MapSection> PushOld(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration)
		{
			List<Tuple<MapSectionRequest, Tuple<MapSection, MapSection?>>> requestAndMapSectionPairs = _mapSectionRequestProcessor.FetchResponses(mapSectionRequests, out var requestsNotFound);

			if (requestsNotFound.Count > 0)
			{
				CheckNewRequestsForCancellation(requestsNotFound);

				_requestsLock.EnterWriteLock();

				try
				{
					//	CreateNewGenMapRequestInfo(mapLoaderJobNumber, requestsNotFound, callback, _mapSectionRequestProcessor, _cts.Token);

					var genMapRequestInfo = new GenMapRequestInfo(msrJob, callback, _mapSectionRequestProcessor, requestsNotFound, _cts.Token);
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

			var mapLoaderJobNumber = msrJob.MapLoaderJobNumber;

			var mapSections = GetMapSectionRecsFromResult(requestAndMapSectionPairs);

			RequestAdded?.Invoke(this, new JobProgressInfo(mapLoaderJobNumber, "temp", DateTime.Now, mapSectionRequests.Count, mapSections.Count));

			return mapSections;
		}

		public List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration)
		{
			//var postGenerationCallBack = msrJob.HandleResponse;
			//var mapSections = _mapSectionRequestProcessor.SubmitRequests(mapSectionRequests, postGenerationCallBack, ct, out pendingGeneration);

			//var mapSections = FetchResponses(mapSectionRequests);

			List<Tuple<MapSectionRequest, Tuple<MapSection, MapSection?>>> requestAndMapSectionPairs = _mapSectionRequestProcessor.FetchResponses(mapSectionRequests, out pendingGeneration);

			var (p, s) = _mapSectionBuilder.CountRequests(pendingGeneration);
			var numberOfMapSectionsRequested = p * 2 + s;

			msrJob.Start(pendingGeneration, callback, numberOfMapSectionsRequested);

			foreach (var mapSectionRequest in pendingGeneration)
			{
				_mapSectionRequestProcessor.AddWork(mapSectionRequest, msrJob.HandleResponse);
			}

			var mapSections = GetMapSectionRecsFromResult(requestAndMapSectionPairs);

			return mapSections;
		}

		// TODO: Have the caller create a Task that 'waits' for the JobCompleted Event to be raised.

		public Task? GetTaskForJob(int jobNumber)
		{
			//var result = DoWithReadLock(() =>
			//{
			//	var t = _requests.FirstOrDefault(x => x.JobNumber == jobNumber)?.Task;
			//	return t;
			//});

			//return result;
			return null;
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

		private List<MapSection> GetMapSectionRecsFromResult(List<Tuple<MapSectionRequest, Tuple<MapSection, MapSection?>>> requestAndMapSectionPairs)
		{
			var mapSections = new List<MapSection>();

			foreach (var reqAndMapSectionPair in requestAndMapSectionPairs)
			{
				var mapSectionPair = reqAndMapSectionPair.Item2;

				var request = mapSectionPair.Item1;
				var mirror = mapSectionPair.Item2;

				mapSections.Add(request);

				if (mirror != null)
				{
					mapSections.Add(mirror);
				}
			}

			return mapSections;
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
	}
}
