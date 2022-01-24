using MapSectionProviderLib;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly List<GenMapRequestInfo> _requestStack;

		private readonly object hmsLock = new();

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requestStack = new List<GenMapRequestInfo>();

			Progress = null;
		}

		#region Public Properties

		public IProgress<MapSection> Progress { get; set; }

		public readonly SizeInt BlockSize;

		private GenMapRequestInfo CurrentRequest => _requestStack.Count == 0 ? null : _requestStack[^1];
		private int? CurrentGenMapRequestId => CurrentRequest?.GenMapRequestId;

		public Job CurrentJob => CurrentRequest?.Job;

		#endregion

		#region Public Methods

		//public PointInt GetBlockPosition(Point screenPos)
		//{
		//	var x = (int)Math.Round(screenPos.X);
		//	var l = Math.DivRem(x, BlockSize.Width, out var remainder);
		//	if (remainder == 0 && l > 0)
		//	{
		//		l--;
		//	}

		//	var job = CurrentRequest.Job;

		//	var invertedY = job.CanvasSizeInBlocks.Height * BlockSize.Height - ((int)Math.Round(screenPos.Y));
		//	var b = Math.DivRem(invertedY, BlockSize.Height, out remainder);
		//	if (remainder == 0 && b > 0)
		//	{
		//		b--;
		//	}

		//	return new PointInt(l, b).Scale(BlockSize);
		//}

		public long? ClearMapSections(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			var job = BuildJob(canvasControlSize, mSetInfo);

			var numberDeleted = _mapSectionRequestProcessor.ClearMapSections(job.Subdivision.Id.ToString());
			return numberDeleted;
		}

		#endregion

		#region Map Support

		public void LoadMap(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();

			var job = BuildJob(canvasControlSize, mSetInfo);
			var mapLoader = new MapLoader(job, HandleMapSection, _mapSectionRequestProcessor);
			var genMapRequestInfo = new GenMapRequestInfo(job, mapLoader.GenMapRequestId, mapLoader);
			_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);

			_requestStack.Add(genMapRequestInfo);
		}

		private void HandleMapSection(int jobId, MapSection mapSection)
		{
			lock (hmsLock)
			{
				if (jobId == CurrentGenMapRequestId)
				{
					Progress.Report(mapSection);
				}
			}
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

		#endregion
	}
}
