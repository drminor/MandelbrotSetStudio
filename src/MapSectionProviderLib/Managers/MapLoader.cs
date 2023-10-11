using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapLoader
	{
		#region Private Fields

		private readonly Action<MapSection> _callback;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private IList<MapSectionRequest>? _mapSectionRequests;

		private bool _isStopping;

		private int _sectionsSubmitted;
		private int _sectionsRequested;
		private int _sectionsCompleted;
		private TaskCompletionSource? _tcs;

		private Stopwatch _stopwatch;

		//private int[] _requestNumbers;
		//private int[] _responseNumbers;

		#endregion

		#region Constructor

		public MapLoader(int jobNumber, Action<MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			JobNumber = jobNumber;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_mapSectionRequests = null;
			_isStopping = false;
			_sectionsSubmitted = 0;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;
			_tcs = null;

			_stopwatch = new Stopwatch();
			_stopwatch.Stop();

			//_requestNumbers = new int[2000];
			//_responseNumbers = new int[2000];

			AllocateMathCounts();
		}

		#endregion

		#region Events

		// Instead of having other windows subscribe to the MapLoader's SectionLoaded event,
		// have those other windows subscribe to an event provided by the class to which the callback method belongs.
		public event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		#endregion

		#region Public Properties

		public MathOpCounts? MathOpCounts { get; private set; }

		public int JobNumber { get; init; }

		public TimeSpan ElaspedTime => _stopwatch.Elapsed;

		public int SectionsSubmitted => _sectionsSubmitted;
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

			var numberOfPairs = 0;

			foreach (var mapSectionRequest in mapSectionRequests)
			{
				_sectionsSubmitted++;
				//_requestNumbers[mapSectionRequest.RequestNumber]++;

				if (mapSectionRequest.Mirror != null)
				{
					numberOfPairs++;
					_sectionsSubmitted++;
					//_requestNumbers[mapSectionRequest.Mirror.RequestNumber]++;
				}
				//_sectionsSubmitted += mapSectionRequest.Mirror != null ? 2 : 1;
			}

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
				//_mapSectionRequestProcessor.CancelJob(JobNumber);
				_isStopping = true;
			}
		}

		//public void MarkJobAsComplete()
		//{
		//	_mapSectionRequestProcessor.MarkJobAsComplete(JobNumber);
		//}

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

			foreach (var mapSectionRequest in _mapSectionRequests)
			{
				if (_isStopping)
				{
					if (_sectionsCompleted >= _sectionsRequested && _tcs?.Task.IsCompleted == false)
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

					_ = Interlocked.Increment(ref _sectionsRequested);

					if (mapSectionRequest.Mirror != null)
					{
						_ = Interlocked.Increment(ref _sectionsRequested);
					}

					_mapSectionRequestProcessor.AddWork(mapSectionRequest, HandleResponse);
					mapSectionRequest.Sent = true;
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

			UpdateMathCounts(mapSection);

			if (mapSectionRequest.Mirror != null)
			{
				mapSectionRequest.Mirror.ProcessingEndTime = DateTime.UtcNow;
			}
			else
			{
				mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;
			}

			ReportGeneration(mapSectionRequest, mapSection);

			_ = Interlocked.Increment(ref _sectionsCompleted);
			//_responseNumbers[mapSectionRequest.RequestNumber]++;

			//if (mapSectionRequest.Mirror != null)
			//{
			//	_ = Interlocked.Increment(ref _sectionsCompleted);
			//	_responseNumbers[mapSectionRequest.Mirror.RequestNumber]++;
			//}

			//if (_sectionsCompleted + 5 > _sectionsRequested && !(_sectionsCompleted >= _mapSectionRequests?.Count))
			//{
			//	Debug.WriteLine($"WARNING: Need to compare the numberOfMapSectionRequests {_mapSectionRequests?.Count ?? 0} with the number of [actual] sectionsRequested {_sectionsRequested}.");
			//}

			if (_sectionsCompleted >= _sectionsSubmitted || (_isStopping && _sectionsCompleted >= _sectionsRequested))
			{
				_stopwatch.Stop();
				HandleLastResponse(mapSectionRequest, mapSection);

				if (_tcs?.Task.IsCompleted == false)
				{
					_tcs.SetResult();
				}

				// Performance 
				ReportMathCounts(MathOpCounts);

				// Debug Job Details
				ReportStats();
			}
			else
			{
				HandleResponseInternal(mapSectionRequest, mapSection);
			}
		}

		private void HandleResponseInternal(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			mapSection.IsLastSection = false;

			// Call the callback, only if the MapSection is not Empty.
			if (!mapSection.IsEmpty)
			{
				_callback(mapSection);
				SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: false));
			}
			else
			{
				Debug.WriteLine($"Not calling the callback, the mapSection is empty. JobId: {mapSectionRequest.MapLoaderJobNumber}, " +
					$"Comp/Total: ({_sectionsCompleted}/{_mapSectionRequests?.Count ?? 0})," +
					$" Screen Position: {mapSectionRequest.ScreenPosition}.");
			}

			mapSectionRequest.Handled = true;
		}

		private void HandleLastResponse(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			mapSection.IsLastSection = true;

			// This is the last section -- call the callback if the MapSection is Empty or Not
			_callback(mapSection);

			if (!mapSection.IsEmpty)
			{
				SectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: true));
			}

			mapSectionRequest.Handled = true;
		}

		private MapSectionProcessInfo CreateMSProcInfo(MapSectionRequest msr, bool isLastSection)
		{
			var result = new MapSectionProcessInfo
				(
				JobNumber,
				msr.FoundInRepo,
				_sectionsCompleted,
				isLastSection,
				msr.TimeToCompleteGenRequest,
				msr.ProcessingDuration,
				msr.GenerationDuration
				//, msr.MathOpCounts
				);

			return result;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportGeneration(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			if (mapSectionRequest.ClientEndPointAddress != null && mapSectionRequest.TimeToCompleteGenRequest != null)
			{
				// TODO: All the property 'AllRowsEscaped' to the MapSection class.
				//var allEscaped = mapSectionResponse?.AllRowsHaveEscaped == true ? "DONE" : null;
				var allEscaped = "Undetermined.";

				var isEmpty = mapSection.IsEmpty;
				var msgPrefix = isEmpty ? string.Empty : "The empty ";
				Debug.WriteLine($"MapSection at screen position: {mapSectionRequest.ScreenPosition}, using client: {mapSectionRequest.ClientEndPointAddress}, took: {mapSectionRequest.TimeToCompleteGenRequest.Value.TotalSeconds}. All Points Escaped: {allEscaped}");
			}
		}

		[Conditional("DEBUG")]
		private void ReportStats()
		{
			var numberOfPendingRequests = _mapSectionRequestProcessor.NumberOfRequestsPending;
			var numberWaitingToBeGen = _mapSectionRequestProcessor.NumberOfSectionsPendingGeneration;

			var notHandled = _mapSectionRequests?.Count(x => !x.Handled) ?? 0;
			var notSent = _mapSectionRequests?.Count(x => !x.Sent) ?? 0;

			Debug.WriteLine($"MapLoader is done with Job: {JobNumber}. Completed {_sectionsCompleted} sections in {_stopwatch.Elapsed}. " +
				$"There are {numberOfPendingRequests} / {numberWaitingToBeGen} / {notHandled} / {notSent} requests still pending / not yet generated / not handled / not sent.");

			//Debug.WriteLine("Request / Response Tallies\n");
			//Debug.WriteLine(BuildReqResNumberTabulation(_requestNumbers, _responseNumbers, 90));
		}

		private string BuildReqResNumberTabulation(int[] requestNumbers, int[] responseNumbers, int size = 90)
		{
			var sb = new StringBuilder();

			for (var i = 0; i < size; i++)
			{
				var reqNumberOccurances = requestNumbers[i];
				var resNumberOccurances = responseNumbers[i];

				sb.Append(i).Append("\t");
				sb.Append(reqNumberOccurances).Append("\t");
				sb.Append(resNumberOccurances).Append("\t");

				sb.Append("\n");
			}

			return sb.ToString();
		}

		#endregion

		#region Performance / Metrics

		[Conditional("PERF")]
		private void AllocateMathCounts()
		{
			MathOpCounts = new MathOpCounts();
		}

		[Conditional("PERF")]
		private void UpdateMathCounts(MapSection mapSection)
		{
			if (MathOpCounts != null && mapSection?.MathOpCounts != null)
			{
				MathOpCounts.Update(mapSection.MathOpCounts);
			}
		}

		[Conditional("PERF")]
		private void ReportMathCounts(MathOpCounts? mathOpCounts)
		{
			if (mathOpCounts != null)
			{
				Debug.WriteLine($"Job completed: Totals: {mathOpCounts}");
			}
		}

		#endregion
	}

}
