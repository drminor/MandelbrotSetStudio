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
using System.Windows;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private Job _job;
		private MapLoader _mapLoader;
		private IProgress<MapSection> _progress;
		private Task _mapLoaderTask;

		private readonly object hmsLock = new();

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
		}

		public readonly SizeInt BlockSize;
		public bool IsLoadingComplete => _mapLoaderTask == null;

		public PointInt GetBlockPosition(Point screenPos)
		{
			var x = (int)Math.Round(screenPos.X);
			var l = Math.DivRem(x, BlockSize.Width, out var remainder);
			if (remainder == 0)
			{
				l--;
			}

			var invertedY = _job.CanvasSizeInBlocks.Height * BlockSize.Height - ((int)Math.Round(screenPos.Y));
			var b = Math.DivRem(invertedY, BlockSize.Height, out remainder);
			if (remainder == 0)
			{
				b--;
			}

			return new PointInt(l, b).Scale(BlockSize);
		}


		public void LoadMap(SizeInt canvasControlSize, MSetInfo mSetInfo, bool refreshMapSections, IProgress<MapSection> progress)
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

			//canvasControlSize = canvasControlSize.Scale(0.9);
			_job = BuildJob(canvasControlSize, mSetInfo);

			_mapLoader = new MapLoader(_mapSectionRequestProcessor);
			_mapLoaderTask = Task.Run(() => _mapLoader.LoadMap(_job, refreshMapSections, HandleMapSection));
			_ = _mapLoaderTask.ContinueWith(OnTaskComplete);
		}

		private Job BuildJob(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");

			// Determine how much of the canvas control can be covered by the new map.
			var canvasSize = RMapHelper.GetCanvasSize(mSetInfo.Coords, canvasControlSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = RMapHelper.GetSamplePointDelta(mSetInfo.Coords, canvasSize);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(mSetInfo.Coords.LeftBot, samplePointDelta, BlockSize, _projectAdapter);

			// Get the number of blocks
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, BlockSize);
			
			// Determine the amount to tranlate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(mSetInfo.Coords.LeftBot, subdivision.Position, samplePointDelta, BlockSize);
			
			// Since we can only fetch whole blocks, the image may not start at the bottom, right corner of the bottom, right block.
			// Determine the amount to move the canvas down and to the right so that the bottom, right sample is displayed at the bottom, right of the canvas control.
			var canvasControlOffset = GetCanvasControlOffset(mSetInfo.Coords, subdivision.SamplePointDelta, canvasSizeInBlocks);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset);
			return job;
		}

		private Subdivision GetSubdivision(RPoint position, RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			// Find an existing subdivision record that has a SamplePointDelta "close to" the given samplePointDelta
			// and that is "in the neighborhood of our Map Set.

			var result = projectAdapter.GetOrCreateSubdivision(position, samplePointDelta, blockSize);
			return result;
		}

		private SizeDbl GetCanvasControlOffset(RRectangle coords, RSize samplePointDelta, SizeInt canvasSizeInBlocks)
		{
			var result = new SizeDbl(0, 0);
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

		#region NOT USED

		public PointInt GetBlockPosition_Old(Point screenPos)
		{
			var x = (int)Math.Round(screenPos.X);
			var l = Math.DivRem(x, BlockSize.Width, out var remainder);
			if (remainder > 0)
			{
				l++;
			}

			var invertedY = _job.CanvasSizeInBlocks.Height * BlockSize.Height - ((int)Math.Round(screenPos.Y));
			var b = Math.DivRem(invertedY, BlockSize.Height, out remainder);
			if (remainder > 0)
			{
				b++;
			}

			return new PointInt(l, b).Scale(BlockSize).Diff(BlockSize);
		}

		#endregion
	}
}
