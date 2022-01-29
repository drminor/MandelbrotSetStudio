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

			Project = new Project(ObjectId.GenerateNewId(), "uncommitted");
			OnMapSectionReady = null;
		}

		#region Public Properties

		public Project Project { get; private set; }
		public readonly SizeInt BlockSize;
		public Action<MapSection> OnMapSectionReady { get; set; }

		private GenMapRequestInfo CurrentRequest => _requestStack.Count == 0 ? null : _requestStack[^1];
		private int? CurrentGenMapRequestId => CurrentRequest?.GenMapRequestId;

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => _requestStack.Count > 1;

		#endregion

		#region Public Methods

		public void LoadMap(string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, bool clearExistingMapSections)
		{
			var curReq = CurrentRequest;
			curReq?.MapLoader?.Stop();

			var job = MapWindowHelper.BuildJob(Project, jobName, canvasControlSize, mSetInfo, BlockSize, _projectAdapter, clearExistingMapSections);
			var t = BigIntegerHelper.ConvertToDouble(mSetInfo.Coords.X1);
			var tes = DoubleHelper.ToExactString(t, out var tExp);
			Debug.WriteLine($"The new job has a SamplePointDelta of {BigIntegerHelper.GetDisplay(job.Subdivision.SamplePointDelta)}. Coord exp: {mSetInfo.Coords.Exponent}. X1: {tes}");

			var mapLoader = new MapLoader(job, HandleMapSection, _mapSectionRequestProcessor);
			var genMapRequestInfo = new GenMapRequestInfo(job, mapLoader.GenMapRequestId, mapLoader);

			lock (hmsLock)
			{
				_ = mapLoader.Start().ContinueWith(genMapRequestInfo.LoadingComplete);
				_requestStack.Add(genMapRequestInfo);
			}
		}

		public void GoBack(SizeInt canvasControlSize, bool clearExistingMapSections)
		{
			// Remove the current request
			_requestStack.RemoveAt(_requestStack.Count - 1);

			// Remove and then reload the one prior to that
			var prevRequest = _requestStack[^1];
			_requestStack.RemoveAt(_requestStack.Count - 1);
			var mSetInfo = prevRequest.Job.MSetInfo;

			LoadMap(prevRequest.Job.Label, canvasControlSize, mSetInfo, clearExistingMapSections);
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);

			var curReq = CurrentRequest;
			var mapBlockOffset = curReq?.Job?.MapBlockOffset ?? new SizeInt();

			var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

			return new Point(blockPos.X, blockPos.Y);
		}

		public void ClearMapSections(SizeInt canvasControlSize, MSetInfo mSetInfo)
		{
			_ = MapWindowHelper.BuildJob(Project, "temp", canvasControlSize, mSetInfo, BlockSize, _projectAdapter, clearExistingMapSections: true);
		}

		#endregion

		#region Private Methods 

		private void HandleMapSection(int genMapRequestId, MapSection mapSection)
		{
			lock (hmsLock)
			{
				if (genMapRequestId == CurrentGenMapRequestId)
				{
					OnMapSectionReady(mapSection);
				}
			}
		}

		#endregion
	}
}
