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
	public class MapLoader
	{
		private readonly Action<MapSection> _callback;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private IList<MapSectionRequest>? _mapSectionRequests;
		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionsCompleted;
		private TaskCompletionSource? _tcs;

		private Stopwatch _stopwatch;

		#region Constructor

		public MapLoader(int jobNumber, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			JobNumber = jobNumber;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));
			//JobNumber = _mapSectionRequestProcessor.GetNextRequestId();

			_mapSectionRequests = null;
			_isStopping = false;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;
			_tcs = null;

			//MathOpCounts = new MathOpCounts();

			_stopwatch = new Stopwatch();
			_stopwatch.Stop();
		}

		#endregion

		#region Public Properties

		// Instead of having other windows subscribe to the MapLoader's SectionLoaded event,
		// have those other windows subscribe to an event provided by the class to which the callback method belongs.
		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		//public MathOpCounts MathOpCounts { get; private set; }

		public int JobNumber { get; init; }

		public TimeSpan ElaspedTime => _stopwatch.Elapsed;

		public int SectionsRequested => _sectionsRequested;
		public int SectionsCompleted => _sectionsCompleted;

		#endregion

		#region Public Methods

		public Task Start(IList<MapSectionRequest> mapSectionRequests)
		{
			if (_tcs != null)
			{
				throw new InvalidOperationException("This MapLoader has already been started.");
			}

			_mapSectionRequests = mapSectionRequests;

			_stopwatch.Start();
			_ = Task.Run(SubmitSectionRequests);

			_tcs = new TaskCompletionSource();
			return _tcs.Task;
		}

		public void Stop()
		{
			if (_tcs == null)
			{
				throw new InvalidOperationException("This MapLoader has not been started.");
			}

			if (!_isStopping && _tcs.Task.Status != TaskStatus.RanToCompletion)
			{
				_mapSectionRequestProcessor.CancelJob(JobNumber);
				_isStopping = true;
			}
		}

		public void MarkJobAsComplete()
		{
			_mapSectionRequestProcessor.MarkJobAsComplete(JobNumber);
		}

		//public void CancelRequest(MapSection mapSection)
		//{
		//	MapSectionRequest? req = _mapSectionRequests?.FirstOrDefault(x => x.RequestNumber == mapSection.RequestNumber);

		//	if (req != null)
		//	{
		//		if (req.TimeToCompleteGenRequest.HasValue)
		//		{
		//			Debug.WriteLine("WARNING: Cancelling a request that has already been completed.");
		//		}

		//		Debug.WriteLine($"Cancelling Generation Request: {JobNumber}/{mapSection.RequestNumber}.");

		//		req.CancellationTokenSource.Cancel();
		//	}
		//}

		//public void CancelRequest(MapSectionRequest mapSectionRequest)
		//{
		//	MapSectionRequest? req = _mapSectionRequests?.FirstOrDefault(x => x.RequestNumber == mapSectionRequest.RequestNumber);

		//	if (req != null)
		//	{
		//		if (req.TimeToCompleteGenRequest.HasValue)
		//		{
		//			Debug.WriteLine("WARNING: Cancelling a request that has already been completed.");
		//		}

		//		Debug.WriteLine($"Cancelling Generation Request: {JobNumber}/{mapSectionRequest.RequestNumber}.");

		//		req.CancellationTokenSource.Cancel();
		//	}
		//}

		#endregion

		#region Private Methods

		private void SubmitSectionRequests()
		{
			if (_mapSectionRequests == null)
			{
				return;
			}

			foreach(var mapSectionRequest in _mapSectionRequests)
			{
				if (_isStopping)
				{
					if (_sectionsCompleted == _sectionsRequested && _tcs?.Task.IsCompleted == false)
					{
						Debug.WriteLine($"The MapLoader is stopping and the completed cnt = requested cnt = {_sectionsCompleted}.");
						_tcs.SetResult();
					}
					break;
				}

				//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");

				if (!mapSectionRequest.CancellationTokenSource.IsCancellationRequested)
				{
					mapSectionRequest.ProcessingStartTime = DateTime.UtcNow;
					_mapSectionRequestProcessor.AddWork(mapSectionRequest, HandleResponse);
					mapSectionRequest.Sent = true;

					_ = Interlocked.Increment(ref _sectionsRequested);
				}
				else
				{
					var msg = $"The MapLoader is skipping request with JobId/Request#: {mapSectionRequest.JobId}/{mapSectionRequest.RequestNumber}.";
					msg += "MapSectionRequest's Cancellation Token is cancelled.";
					Debug.WriteLine($"{msg}");
				}
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			Debug.Assert(mapSection.JobNumber == JobNumber, "The MapSection's JobNumber does not match the MapLoader's JobNumber as the MapLoader's HandleResponse is being called from the Response Processor.");

			mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;

			//if (mapSectionResponse?.MathOpCounts != null)
			//{
			//	MathOpCounts.Update(mapSectionResponse.MathOpCounts);
			//}

			/****************************************************
			// UN COMMENT ME TO Report on generation duration.
			//if (mapSectionRequest.ClientEndPointAddress != null && mapSectionRequest.TimeToCompleteGenRequest != null)
			//{
				//var allEscaped = mapSectionResponse?.AllRowsHaveEscaped == true ? "DONE" : null;
				//Debug.WriteLine($"MapSection for {mapSectionResult.BlockPosition}, using client: {mapSectionRequest.ClientEndPointAddress}, took: {mapSectionRequest.TimeToCompleteGenRequest.Value.TotalSeconds}. {allEscaped}");
			//}
			****************************************************/

			_ = Interlocked.Increment(ref _sectionsCompleted);

			if (_sectionsCompleted >= _mapSectionRequests?.Count || (_isStopping && _sectionsCompleted >= _sectionsRequested))
			{
				// This is the last section -- call the callback if the MapSection is Empty or Not
				_stopwatch.Stop();

				mapSection.IsLastSection = true;
				_callback(mapSection);

				if (!mapSection.IsEmpty)
				{
					SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: true));
				}

				mapSectionRequest.Handled = true;

				if (_tcs?.Task.IsCompleted == false)
				{
					_tcs.SetResult();
				}

				//ReportStats();
			}
			else
			{
				// Call the callback, only if the MapSection is not Empty.

				if (!mapSection.IsEmpty)
				{
					mapSection.IsLastSection = false;
					_callback(mapSection);
					SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: false));
				}
				else
				{
					Debug.WriteLine($"Not calling the callback, the mapSection is empty. JobId: {mapSectionRequest.JobId}; Screen Position: {mapSectionRequest.ScreenPosition}.");
				}

				mapSectionRequest.Handled = true;

				//Debug.WriteLine($"Job completed: Totals: {MathOpCounts}");
			}
		}

		//private void ReportStats()
		//{
		//	var numberOfPendingRequests = _mapSectionRequestProcessor.GetNumberOfPendingRequests(JobNumber);
		//	var notHandled = _mapSectionRequests?.Count(x => !x.Handled) ?? 0;
		//	var notSent = _mapSectionRequests?.Count(x => !x.Sent) ?? 0;

		//	Debug.WriteLine($"MapLoader is done with Job: {JobNumber}. Completed {_sectionsCompleted} sections in {_stopwatch.Elapsed}. " +
		//		$"There are {numberOfPendingRequests}/{notHandled}/{notSent} requests still pending, not handled, not sent.");
		//}

		private MapSectionProcessInfo CreateMSProcInfo(MapSectionRequest msr, bool isLastSection)
		{
			var result = new MapSectionProcessInfo
				(
				JobNumber,
				msr.FoundInRepo,
				_sectionsCompleted,
				isLastSection
,
				msr.TimeToCompleteGenRequest,
				msr.ProcessingDuration,
				msr.GenerationDuration              //,
													//msr.MathOpCounts
				);
			return result;
		}

		#endregion
	}
}
