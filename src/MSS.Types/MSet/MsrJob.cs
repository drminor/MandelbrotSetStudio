using System;
using System.Diagnostics;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MsrJob
	{
		#region Private Fields

		private Action<MapSection> _mapSectionReadyCallback;
		private Action<int, bool> _mapViewUpdateCompleteCallback;
		private bool _isCompleted;
		private Stopwatch _stopwatch;

		private int _sectionsFoundInRepo;
		private int _sectionsGenerated;
		private int _sectionsCancelled;


		#endregion

		#region Constructors

		public MsrJob() : this(mapLoaderJobNumber: 0, jobType: JobType.FullScale, jobId: "", ownerType: OwnerType.Project, subdivision: new Subdivision(), originalSourceSubdivisionId: "",
			jobBlockOffset: new VectorLong(), precision: 0, limbCount: 0, mapCalcSettings: new MapCalcSettings(), crossesYZero: false)
		{ }

		public MsrJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType ownerType, Subdivision subdivision, string originalSourceSubdivisionId,
			VectorLong jobBlockOffset, int precision, int limbCount, MapCalcSettings mapCalcSettings, bool crossesYZero)
		{
			if (originalSourceSubdivisionId == string.Empty)
			{
				Debug.WriteLine($"MsrJob Constructor: The originalSourceSubdivisionId is blank.");
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
			CrossesYZero = crossesYZero;

			IsCancelled = false;
			CancellationTokenSource = new CancellationTokenSource();

			_mapSectionReadyCallback = NoOpMapSectionReadyCallBack;
			_mapViewUpdateCompleteCallback = NoOpMapViewUpdateCompleteCallBack;

			_sectionsFoundInRepo = 0;
			_sectionsGenerated = 0;
			_sectionsCancelled = 0;

			IsStarted = false;
			_isCompleted = false;

			_stopwatch = new Stopwatch();

			AllocateMathCounts();
		}

		#endregion

		#region Events

		public event EventHandler<MapSectionRequest>? MapSectionLoaded;

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

		public bool IsStarted { get; private set; } 
		public bool IsCancelled { get; set; }
		public CancellationTokenSource CancellationTokenSource { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan ElaspedTime { get; private set; }
		public TimeSpan TotalExecutionTime { get; set; }

		public MathOpCounts? MathOpCounts { get; private set; }

		public bool IsComplete { get; private set; }

		public int TotalNumberOfSectionsRequested { get; set; }

		public int SectionsFoundInRepo
		{
			get => _sectionsFoundInRepo;

			set
			{
				if (value != _sectionsFoundInRepo)
				{
					_sectionsFoundInRepo = value;
					if (SectionsPending <= 0)
					{
						MarkJobAsComplete();
					}
				}
			}
		}

		public int SectionsGenerated
		{
			get => _sectionsGenerated;

			set
			{
				if (value != _sectionsGenerated)
				{
					_sectionsGenerated = value;
					if (SectionsPending <= 0)
					{
						MarkJobAsComplete();
					}
				}
			}
		}

		public int SectionsCancelled
		{
			get => _sectionsCancelled;

			set
			{
				if (value != _sectionsCancelled)
				{
					_sectionsCancelled = value;
					if (SectionsPending <= 0)
					{
						MarkJobAsComplete();
					}
				}
			}
		}

		public int SectionsPending => TotalNumberOfSectionsRequested - SectionsFoundInRepo - SectionsGenerated - SectionsCancelled;

		#endregion

		#region Public Methods

		public bool Start(int sectionsRequested, int sectionsCancelled, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback)
		{
			//return Start(sectionsRequested, 0, 0, sectionsCancelled, mapSectionReadyCallback, mapViewUpdateCompleteCallback);

			if (IsStarted)
			{
				throw new InvalidOperationException("Cannot start this MsrJob, it has already been started.");
			}

			IsStarted = true;

			TotalNumberOfSectionsRequested = sectionsRequested;
			SectionsFoundInRepo = 0;
			SectionsGenerated = 0;
			SectionsCancelled = sectionsCancelled;

			_mapSectionReadyCallback = mapSectionReadyCallback;
			_mapViewUpdateCompleteCallback = mapViewUpdateCompleteCallback;

			if (SectionsPending == 0)
			{
				MarkJobAsComplete();
			}
			else
			{
				_stopwatch.Start();
			}

			return true;
		}

		public void Cancel()
		{
			if (IsCancelled)
			{
				Debug.WriteLine($"WARNING: MsrJob: Cancelling Job: {MapLoaderJobNumber} that has already been cancelled.");
			}
			else
			{
				IsCancelled = true;
				//MarkJobAsComplete();
			}
		}

		public void MarkJobAsComplete()
		{
			if (!_isCompleted)
			{
				_isCompleted = true;
				_stopwatch.Stop();
				ElaspedTime = _stopwatch.Elapsed;

				JobHasCompleted?.Invoke(this, new EventArgs());

				_mapViewUpdateCompleteCallback(JobNumber, IsCancelled);

				ReportMathCounts(MathOpCounts);

				//Debug.WriteLine($"MsrJob. MarkJobIsComplete for Job: {MapLoaderJobNumber} Total Requested: {TotalNumberOfSectionsRequested}, Found: {SectionsFoundInRepo}, Generated: {SectionsGenerated}, Cancelled: {SectionsCancelled}, Pending: {SectionsPending}.");
			}
			else
			{
				Debug.WriteLine($"WARNING!!: MsrJob. MarkJobIsComplete is being called after after IsComplete is set for Job: {MapLoaderJobNumber} Total Requested: {TotalNumberOfSectionsRequested}, Found: {SectionsFoundInRepo}, Generated: {SectionsGenerated}, Cancelled: {SectionsCancelled}, Pending: {SectionsPending}.");
			}
		}

		public void HandleResponse(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			Debug.Assert(mapSection.JobNumber == JobNumber, "The MapSection's JobNumber does not match the MapLoader's JobNumber as the MsrJobs's HandleResponse is being called from the Response Processor.");

			mapSectionRequest.ProcessingEndTime = DateTime.UtcNow;
			UpdateMathCounts(mapSection);
			ReportGeneration(mapSectionRequest, mapSection);

			if (SectionsPending < 0)
			{
				Debug.WriteLine($"WARNING!!: MsrJob. HandleResponse is still being called after IsComplete is set for Job: {MapLoaderJobNumber} Total Requested: {TotalNumberOfSectionsRequested}, Found: {SectionsFoundInRepo}, Generated: {SectionsGenerated}, Cancelled: {SectionsCancelled}, Pending: {SectionsPending}.");
			}

			if (SectionsPending <= 0)
			{
				ReportStats();
			}

			_mapSectionReadyCallback(mapSection);
			MapSectionLoaded?.Invoke(this, mapSectionRequest);
		}

		public override string ToString()
		{
			return $"Id: {JobId}, JobNumber: {MapLoaderJobNumber}.";
		}

		#endregion

		#region Private Methods

		//private void HandleResponseInternal(MapSectionRequest mapSectionRequest, MapSection mapSection)
		//{
		//	mapSection.IsLastSection = false;

		//	// Call the callback, only if the MapSection is not Empty.
		//	if (!mapSection.IsEmpty)
		//	{
		//		_mapSectionReadyCallback(mapSection);
		//		MapSectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest));
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"MsrJob: Not calling the callback, the mapSection is empty. JobId: {mapSectionRequest.MapLoaderJobNumber}, " +
		//			$"Pending/Total: ({SectionsPending}/{TotalNumberOfSectionsRequested}), " +
		//			$"Screen Position: {mapSection.ScreenPosition}.");
		//	}

		//	mapSectionRequest.Handled = true;
		//}

		//private void HandleLastResponse(MapSectionRequest mapSectionRequest, MapSection mapSection)
		//{
		//	mapSection.IsLastSection = true;

		//	// This is the last section -- call the callback if the MapSection is Empty or Not
		//	_mapSectionReadyCallback(mapSection);

		//	if (!mapSection.IsEmpty)
		//	{
		//		MapSectionLoaded?.Invoke(this, CreateMSProcInfo(mapSectionRequest));
		//	}

		//	mapSectionRequest.Handled = true;
		//}

		//private MapSectionProcessInfo CreateMSProcInfo(MapSectionRequest msr)
		//{
		//	var result = new MapSectionProcessInfo
		//		(
		//		jobNumber: msr.MapLoaderJobNumber,
		//		requestNumber: msr.RequestNumber,
		//		msr.FoundInRepo,
		//		msr.Completed,
		//		msr.IsCancelled,
		//		msr.TimeToCompleteGenRequest,
		//		msr.ProcessingDuration,
		//		msr.GenerationDuration
		//		);

		//	return result;
		//}

		private void NoOpMapSectionReadyCallBack(MapSection mapSection)
		{
			Debug.WriteLine("WARNING. MsrJob. The NoOpCallBack is being called.");
		}

		private void NoOpMapViewUpdateCompleteCallBack(int jobNumber, bool isCancelled)
		{
			Debug.WriteLine("WARNING. MsrJob. The NoOpCallBack is being called.");
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportGeneration(MapSectionRequest mapSectionRequest, MapSection mapSection)
		{
			if (mapSectionRequest.ClientEndPointAddress != null && mapSectionRequest.TimeToCompleteGenRequest != null)
			{
				var allEscaped = mapSectionRequest.AllRowsHaveEscaped == true ? "DONE" : null;
				Debug.WriteLine($"MsrJob: The MapSection at screen position: {mapSection.ScreenPosition}, using client: {mapSectionRequest.ClientEndPointAddress}, " +
					$"took: {mapSectionRequest.TimeToCompleteGenRequest.Value.TotalSeconds}. All Points Escaped: {allEscaped}");
			}
		}

		[Conditional("DEBUG")]
		private void ReportStats()
		{
			if (IsCancelled)
			{
				Debug.WriteLine($"MsrJob is done with Job: {JobNumber}. Generated {SectionsGenerated} sections in {ElaspedTime}. The job was cancelled. There are {SectionsPending} sections pending. " +
					$"Total Requested: {TotalNumberOfSectionsRequested}, Found: {SectionsFoundInRepo}, Generated: {SectionsGenerated}, Cancelled: {SectionsCancelled}");
			}
			else
			{
				Debug.WriteLine($"MsrJob is done with Job: {JobNumber}. Generated {SectionsGenerated} sections in {ElaspedTime}. There are {SectionsPending} sections pending. " +
					$"Total Requested: {TotalNumberOfSectionsRequested}, Found: {SectionsFoundInRepo}, Generated: {SectionsGenerated}, Cancelled: {SectionsCancelled}");
			}

			//Debug.WriteLine("Request / Response Tallies\n");
			//Debug.WriteLine(BuildReqResNumberTabulation(_requestNumbers, _responseNumbers, 90));
		}

		//private string BuildReqResNumberTabulation(int[] requestNumbers, int[] responseNumbers, int size = 90)
		//{
		//	var sb = new StringBuilder();

		//	for (var i = 0; i < size; i++)
		//	{
		//		var reqNumberOccurances = requestNumbers[i];
		//		var resNumberOccurances = responseNumbers[i];

		//		sb.Append(i).Append("\t");
		//		sb.Append(reqNumberOccurances).Append("\t");
		//		sb.Append(resNumberOccurances).Append("\t");

		//		sb.Append("\n");
		//	}

		//	return sb.ToString();
		//}

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
