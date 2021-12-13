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

			// Calculate approximate samplePointDelta
			var samplePointDelta = GetSamplePointDelta(mSetInfo.Coords, canvasControlSize, _blockSize);

			// Find an existing subdivision record that has a SamplePointDelta "close to" the given samplePointDelta.
			var subdivision = GetSubdivision(samplePointDelta, _blockSize, _projectAdapter);

			// Use the SamplePointDelta found to get the size of the canvas in blocks.
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(mSetInfo.Coords, subdivision.SamplePointDelta, canvasControlSize, _blockSize);
			var canvasBlockOffset = GetCanvasBlockOffset(mSetInfo.Coords.LeftBot, subdivision.Position, subdivision.BlockSize);
			var canvasControlOffset = GetCanvasControlOffset(mSetInfo.Coords, subdivision.SamplePointDelta, canvasSizeInBlocks);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo, canvasSizeInBlocks, canvasBlockOffset, canvasControlOffset);
			return job;
		}

		private RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasControlSize, SizeInt blockSize)
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

		private PointInt GetCanvasBlockOffset(RPoint mapOrigin, RPoint subdivisionOrigin, SizeInt blockSize)
		{
			// The left-most, bottom-most block is 0, 0 in our cordinates
			// The canvasBlockOffset is the amount added to our block position to address the block in subdivison coordinates.

			// TODO: Use the subdivision RPoint origin and our Map coordinates to calculate the Canvas Block Offset.

			var result = new PointInt(-4, -3);

			return result;
		}

		private PointDbl GetCanvasControlOffset(RRectangle coords, RSize samplePointDelta, SizeInt canvasSizeInBlocks)
		{
			// TODO: Use the size of the canvas in blocks, the SamplePointDelta and our Map coordinates to calculate the CanvasControlOffset

			var result = new PointDbl(0, 0);
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
