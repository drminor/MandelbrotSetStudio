using MSS.Common;
using MSS.Types.MSet;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Diagnostics;
using MapSectionProviderLib;
using System.Threading;
using System.Printing;

namespace MSetExplorer.XPoc.PerformanceHarness
{
    class PerformanceHarnessMainWinViewModel : ViewModelBase
	{
		private readonly SynchronizationContext? _synchronizationContext;

		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionHelper _mapSectionHelper;

		private MapLoader? _mapLoader;
		private Task? _startTask;

		public JobProgressInfo? JobProgressInfo;
		public List<MapSectionProcessInfo> MapSectionProcessInfos;

		public MathOpCounts MathOpCounts { get; set; } 

		private Stopwatch _stopWatch;
		private bool _receviedTheLastOne;

		public PerformanceHarnessMainWinViewModel(MapSectionRequestProcessor mapSectionRequestProcessor, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper)
        {
			_synchronizationContext = SynchronizationContext.Current;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
			_mapSectionRequestProcessor.UseRepo = false;

			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;

			MapSectionProcessInfos = new List<MapSectionProcessInfo>();
			_stopWatch = new Stopwatch();
		}

		#region Public Properties

		private int _generatedCount;
		public int GeneratedCount
		{
			get => _generatedCount;
			set => _generatedCount = value;
		}

		private long _elasped;
		public long Elasped
		{
			get => _elasped;
			set => _elasped = value;
		}


		private long _multiplications;
		public long Multiplications
		{
			get => _multiplications;
			set => _multiplications = value;
		}

		private long _calcs;
		public long Calcs
		{
			get => _calcs;
			set => _calcs = value;
		}

		#endregion


		#region Public Methods

		public void RunBaseLine()
		{
			_receviedTheLastOne = false;
			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);

			var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN;
			var mapCalcSettings = new MapCalcSettings(targetIterations: 400, requestsPerJob: 100);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

			JobProgressInfo = RunJob(job, out _mapLoader, out _startTask);
		}

		private JobProgressInfo RunJob(Job job, out MapLoader mapLoader, out Task startTask)
		{
			var ownerId = job.ProjectId.ToString();
			var jobOwnerType = JobOwnerType.Project;

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(job.Coords, job.CanvasSize, RMapConstants.BLOCK_SIZE);
			var mapSectionRequests = _mapSectionHelper.CreateSectionRequests(ownerId, jobOwnerType, mapAreaInfo, job.MapCalcSettings);

			mapLoader = new MapLoader(mapAreaInfo.MapBlockOffset, MapSectionReady, _mapSectionHelper, _mapSectionRequestProcessor);

			_stopWatch = Stopwatch.StartNew();
			startTask = mapLoader.Start(mapSectionRequests);

			var result = new JobProgressInfo(mapLoader.JobNumber, "temp", DateTime.Now, mapSectionRequests.Count);

			mapLoader.SectionLoaded += MapLoader_SectionLoaded;

			return result;
		}

		private void MapLoader_SectionLoaded(object? sender, MapSectionProcessInfo e)
		{
			MapSectionProcessInfos.Add(e);

			if (JobProgressInfo != null)
			{
				if (e.FoundInRepo)
				{
					JobProgressInfo.FetchedCount++;

				}
				else
				{
					JobProgressInfo.GeneratedCount++;
				}

				if (JobProgressInfo.IsComplete || _receviedTheLastOne)
				{
					_stopWatch.Stop();
					var mops = new MathOpCounts();

					foreach(var x in MapSectionProcessInfos)
					{
						if (x.MathOpCounts.HasValue)
						{
							mops.Update(x.MathOpCounts.Value);
						}
					}


					MathOpCounts = mops;

					Debug.WriteLine($"Generated {JobProgressInfo.GeneratedCount} sections in {_stopWatch.ElapsedMilliseconds}. Performed: {mops.NumberOfMultiplications}." +
						$"{mops.NumberOfCalcs} iteration calculation used; {mops.NumberOfUnusedCalcs} unused.");

					GeneratedCount = JobProgressInfo.GeneratedCount;
					Elasped = _stopWatch.ElapsedMilliseconds;
					Multiplications = mops.NumberOfMultiplications;
					Calcs = (long)mops.NumberOfCalcs;

					_synchronizationContext?.Post((o) => HandleRunComplete(), null);

				}
			}
		}

		private void HandleRunComplete()
		{
			OnPropertyChanged(nameof(GeneratedCount));
			OnPropertyChanged(nameof(Elasped));
			OnPropertyChanged(nameof(Multiplications));
			OnPropertyChanged(nameof(Calcs));
		}


		private void MapSectionReady(MapSection mapSection, int jobId, bool isLast)
		{
			if (isLast)
			{
				_receviedTheLastOne = true;
				Debug.WriteLine($"{jobId} is complete. Received {MapSectionProcessInfos.Count} process infos.");
			}
			else
			{
				//Debug.WriteLine($"Got a mapSection.");
			}
		}


		public void StopJob(int jobNumber)
		{
			//DoWithWriteLock(() =>
			//{
			//	StopCurrentJobInternal(jobNumber);
			//});
		}


		private void StopCurrentJobInternal(int jobNumber)
		{
			//var request = _requests.FirstOrDefault(x => x.JobNumber == jobNumber);

			//if (request != null)
			//{
			//	request.MapLoader.Stop();
			//}
		}


		#endregion


	}
}
