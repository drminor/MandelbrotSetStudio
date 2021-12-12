using MapSectionProviderLib;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private SizeInt _blockSize;
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private Job _job;
		private MapLoader _mapLoader;
		private IProgress<MapSection> _progress;
		private Task _mapLoaderTask;

		private readonly object hmsLock = new();

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
		}

		public void LoadMap(SizeInt canvasControlSize, MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			if (!(_job is null))
			{
				Debug.WriteLine("Warning, not saving current job.");

				if (!(_mapLoader is null))
				{
					throw new InvalidOperationException("Cannot call GenerateMapSections until the current task is complete.");
				}
			}

			_progress = progress;

			_job = BuildJob(canvasControlSize, mSetInfo);


			_mapLoader = new MapLoader(_mapSectionRequestProcessor);
			_mapLoaderTask = Task.Run(() => _mapLoader.LoadMap(_job, HandleMapSection));
			_ = _mapLoaderTask.ContinueWith(OnTaskComplete);
		}

		private Job BuildJob(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");

			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(mSetInfo.Coords, canvasControlSize, _blockSize);

			var samplePointDelta = GetSamplePointDelta(mSetInfo.Coords, canvasSizeInBlocks, _blockSize);
			var subdivision = GetSubdivision(samplePointDelta, _blockSize, _projectAdapter);

			// The left-most, bottom-most block is 0, 0 in our cordinates
			// The canvasBlockOffset is the amount added to our block position to address the block in subdivison coordinates.
			var canvasBlockOffset = new PointInt(-4, -3);
			var canvasControlOffset = new PointDbl(0, 0);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo, canvasSizeInBlocks, canvasBlockOffset, canvasControlOffset);
			return job;
		}

		private RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSizeInBlocks, SizeInt blockSize)
		{
			var result = new RSize(1, 1, -8);

			return result;
		}

		private Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			//var temp = JobHelper.GetSubdivision(mSetInfo, blockSize);
			var temp = new Subdivision(ObjectId.GenerateNewId(), new RPoint(), blockSize, samplePointDelta);
			var result = projectAdapter.GetOrCreateSubdivision(temp);

			return result;
		}

		private void HandleMapSection(MapSection mapSection)
		{
			lock (hmsLock)
			{
				_progress.Report(mapSection);
			}
		}

		private void OnTaskComplete(Task t)
		{
			_mapLoader.Stop();
			_mapLoader = null;
			_mapLoaderTask = null;
		}

	}
}
