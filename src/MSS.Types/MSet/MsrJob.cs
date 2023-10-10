using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MsrJob : IMsrJob
	{
		#region Private Fields

		private Action<MapSection> _callback;
		//private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private List<MapSectionRequest>? _mapSectionRequests;

		private bool _isStopping;
		private bool _isCompleted;

		private int _sectionsSubmitted;
		private int _sectionsRequested;
		private int _sectionsCompleted;

		private Stopwatch _stopwatch;

		#endregion

		public MsrJob() : this(mapLoaderJobNumber: 0, jobType: JobType.FullScale, jobId: "", ownerType: OwnerType.Project, subdivision: new Subdivision(), originalSourceSubdivisionId: "",
			jobBlockOffset: new VectorLong(), precision: 0, limbCount: 0, mapCalcSettings: new MapCalcSettings(), crossesXZero: false)
		{ }

		public MsrJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType ownerType, Subdivision subdivision, string originalSourceSubdivisionId, 
			VectorLong jobBlockOffset, int precision, int limbCount, MapCalcSettings mapCalcSettings, bool crossesXZero)
		{
			ObjectId test = new ObjectId(originalSourceSubdivisionId);

			if (test == ObjectId.Empty)
			{
				Debug.WriteLine($"The originalSourceSubdivisionId is blank during MapSectionRequest construction.");
			}

			MapLoaderJobNumber = mapLoaderJobNumber;
			
			JobType = jobType;
			JobId = jobId;
			OwnerType = ownerType;
			Subdivision = subdivision;
			OriginalSourceSubdivisionId = originalSourceSubdivisionId;
			JobBlockOffset = jobBlockOffset;
			Precision = precision;
			LimbCount = limbCount;
			MapCalcSettings = mapCalcSettings;
			CrossesYZero = crossesXZero;

			IsCancelled = false;
			CancellationTokenSource = new CancellationTokenSource();

			_callback = NoOpCallBack;
			//_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_mapSectionRequests = null;
			_isStopping = false;
			_isCompleted = false;
			_sectionsSubmitted = 0;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;

			_stopwatch = new Stopwatch();
			_stopwatch.Stop();

			AllocateMathCounts();

			_mapSectionRequests = null;

			//CancellationTokenSource = new CancellationTokenSource();
		}

		#region Events

		// Instead of having other windows subscribe to the MapLoader's SectionLoaded event,
		// have those other windows subscribe to an event provided by the class to which the callback method belongs.
		public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;
		public event EventHandler? JobHasCompleted;


		#endregion

		#region Public Properties

		public int MapLoaderJobNumber { get; set; }

		public int JobNumber => MapLoaderJobNumber;

		public JobType JobType { get; init; }
		public string JobId { get; init; }
		public OwnerType OwnerType { get; init; }
		public Subdivision Subdivision { get; init; }
		public string OriginalSourceSubdivisionId { get; init; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		public VectorLong JobBlockOffset { get; init; }

		public int Precision { get; set; }
		public int LimbCount { get; set; }

		public SizeInt BlockSize => Subdivision.BlockSize;
		public RSize SamplePointDelta => Subdivision.SamplePointDelta;
		public MapCalcSettings MapCalcSettings { get; init; }
		public bool CrossesYZero { get; init; }

		public bool IsCancelled { get; set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		#endregion

		#region Pubic Properties - Optional

		public List<MapSectionRequest>? MapSectionRequests => _mapSectionRequests;

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan ElaspedTime => _stopwatch.Elapsed;
		public TimeSpan TotalExecutionTime { get; set; }

		public MathOpCounts? MathOpCounts { get; private set; }

		public int SectionsSubmitted => _sectionsSubmitted;
		public int SectionsRequested => _sectionsRequested;
		public int SectionsCompleted => _sectionsCompleted;

		#endregion

		#region Public Methods

		public bool Start(List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, int numberOfMapSectionsRequested)
		{
			_mapSectionRequests = mapSectionRequests;
			_callback = callback;

			_sectionsRequested = numberOfMapSectionsRequested;
			_sectionsSubmitted = numberOfMapSectionsRequested; 

			//if (mapSectionRequests.Count == numberOfsectionsRequested)
			//{
			//	_isCompleted = true;
			//}

			return true;
		}

		//public bool UpdateReqPendingCount(int amount)
		//{
		//	_sectionsSubmitted = amount;

		//	var result = AllCompleted();

		//	return result;
		//}

		public void Cancel()
		{

		}

		public void MarkJobAsComplete()
		{
			if (!_isCompleted)
			{
				_isCompleted = true;
				JobHasCompleted?.Invoke(this, new EventArgs());
			}
		}

		public int GetNumberOfRequestsPendingSubmittal()
		{
			return 0;
		}

		public int GetNumberOfRequestsPendingGeneration()
		{
			return 0;
		}

		public override string ToString()
		{
			return $"Id: {JobId}, JobNumber: {MapLoaderJobNumber}.";
		}

		public void HandleResponse(MapSectionRequest mapSectionRequest, MapSection mapSection)
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

			if (AllCompleted())
			{
				_stopwatch.Stop();
				HandleLastResponse(mapSectionRequest, mapSection);

				//if (_tcs?.Task.IsCompleted == false) { _tcs.SetResult(); }
				JobHasCompleted?.Invoke(this, new EventArgs());

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

		private bool AllCompleted()
		{
			var result = _sectionsCompleted >= _sectionsSubmitted || (_isStopping && _sectionsCompleted >= _sectionsRequested);
			return result;
		}

		#endregion

		#region Private Methods

		private void HandleResponseInternal(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			mapSection.IsLastSection = false;

			// Call the callback, only if the MapSection is not Empty.
			if (!mapSection.IsEmpty)
			{
				_callback(mapSection);
				MapSectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: false));
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
				MapSectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest, isLastSection: true));
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

		private void NoOpCallBack(MapSection mapSection)
		{
			Debug.WriteLine("WARNING. The NoOpCallBack is being called.");
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
			var numberOfPendingRequests = 0; // _mapSectionRequestProcessor.NumberOfRequestsPending;
			var numberWaitingToBeGen = 0; // _mapSectionRequestProcessor.NumberOfSectionsPendingGeneration;

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
