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
using System.Globalization;
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

			OnMapSectionReady = null;
		}

		#region Public Properties

		public Action<MapSection> OnMapSectionReady { get; set; }

		public readonly SizeInt BlockSize;

		private GenMapRequestInfo CurrentRequest => _requestStack.Count == 0 ? null : _requestStack[^1];
		private int? CurrentGenMapRequestId => CurrentRequest?.GenMapRequestId;

		public Job CurrentJob => CurrentRequest?.Job;

		#endregion

		#region Public Methods

		public void LoadMap(SizeInt canvasControlSize, MSetInfo mSetInfo, bool clearExistingMapSections)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();

			var job = BuildJob(canvasControlSize, mSetInfo, clearExistingMapSections);
			Debug.WriteLine($"The new job has a SamplePointDelta of {BigIntegerHelper.GetDisplay(job.Subdivision.SamplePointDelta)}.");

			var mapLoader = new MapLoader(job, HandleMapSection, _mapSectionRequestProcessor);
			var genMapRequestInfo = new GenMapRequestInfo(job, mapLoader.GenMapRequestId, mapLoader);

			lock (hmsLock)
			{
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
				_requestStack.Add(genMapRequestInfo);
			}
		}

		public Point GetBlockPositionOld(Point posYInverted)
		{
			var x = (int)Math.Round(posYInverted.X);
			var l = Math.DivRem(x, BlockSize.Width, out var remainder);
			if (remainder == 0 && l > 0)
			{
				l--;
			}

			var y = (int)Math.Round(posYInverted.Y);
			var b = Math.DivRem(y, BlockSize.Height, out remainder);
			if (remainder == 0 && b > 0)
			{
				b--;
			}

			var botRight = new PointInt(l, b).Scale(BlockSize);
			var center = botRight.Translate(new SizeInt(-2 + BlockSize.Width / 2, 2 + BlockSize.Height / 2));
			return new Point(center.X, center.Y);
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);
			var blockPosInt = RMapHelper.GetBlockPosition(pointInt, BlockSize);

			return new Point(blockPosInt.X, blockPosInt.Y);
		}

		public void ClearMapSections(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			_ = BuildJob(canvasControlSize, mSetInfo, clearExistingMapSections: true);
		}

		#endregion

		#region Private Methods 

		private void HandleMapSection(int jobId, MapSection mapSection)
		{
			lock (hmsLock)
			{
				if (jobId == CurrentGenMapRequestId)
				{
					OnMapSectionReady(mapSection);
				}
			}
		}

		private Job BuildJob(SizeInt canvasControlSize, MSetInfo mSetInfo, bool clearExistingMapSections)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");

			// Determine how much of the canvas control can be covered by the new map.
			var canvasSize = RMapHelper.GetCanvasSize(mSetInfo.Coords, canvasControlSize);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = RMapHelper.GetSamplePointDelta(mSetInfo.Coords, canvasSize);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(mSetInfo.Coords.LeftBot, samplePointDelta, BlockSize, _projectAdapter, deleteExisting: clearExistingMapSections);

			// Get the number of blocks
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, BlockSize);

			// Determine the amount to tranlate from our coordinates to the subdivision coordinates.
			var coords = mSetInfo.Coords;
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref coords, subdivision.Position, samplePointDelta, BlockSize, out var canvasControlOffset);

			//var updatedMSetInfo = new MSetInfo(mSetInfo, coords);
			var updatedMSetInfo = mSetInfo;

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", updatedMSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset);
			return job;
		}

		private Subdivision GetSubdivision(RPoint position, RSize samplePointDelta, SizeInt blockSize, ProjectAdapter projectAdapter, bool deleteExisting)
		{
			// Find an existing subdivision record that has a SamplePointDelta "close to" the given samplePointDelta
			// and that is "in the neighborhood of our Map Set.

			var result = projectAdapter.GetOrCreateSubdivision(position, samplePointDelta, blockSize, out var created);

			//while(deleteExisting && result.DateCreated < DateTime.Parse("1/25/2022 5:47", CultureInfo.InvariantCulture))
			//{
			//	_ = projectAdapter.DeleteSubdivision(result);
			//	result = projectAdapter.GetOrCreateSubdivision(position, samplePointDelta, blockSize, out var _);
			//}

			while (deleteExisting && !created)
			{
				_ = projectAdapter.DeleteSubdivision(result);
				result = projectAdapter.GetOrCreateSubdivision(position, samplePointDelta, blockSize, out created);
			}

			return result;
		}

		#endregion
	}
}
