using MapSectionProviderLib;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class MapNavSimViewModel : ViewModelBase
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly List<GenMapRequestInfo> _requestStack;

		private readonly object hmsLock = new();

		public MapNavSimViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestProcessor = mapSectionRequestProcessor;

			_requestStack = new List<GenMapRequestInfo>();

			Project = new Project(ObjectId.GenerateNewId(), "uncommitted");
		}

		#region Public Properties

		public Project Project { get; private set; }
		public readonly SizeInt BlockSize;

		private GenMapRequestInfo CurrentRequest => _requestStack.Count == 0 ? null : _requestStack[^1];
		private int? CurrentGenMapRequestId => CurrentRequest?.GenMapRequestId;

		public Job CurrentJob => CurrentRequest?.Job;
		public bool CanGoBack => _requestStack.Count > 1;


		#endregion

		#region Public Methods

		//public void LoadMap(string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, bool clearExistingMapSections)
		//{
		//	//var curReq = CurrentRequest;

		//	var job = MapWindowHelper.BuildJob(Project, jobName, canvasControlSize, mSetInfo, BlockSize, _projectAdapter, clearExistingMapSections);
		//	Debug.WriteLine($"The new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta}.");

		//	_requestStack.Add(job);
		//}

		public void LoadMap(string jobName, SizeInt canvasControlSize, MSetInfo mSetInfo, SizeInt newArea, bool clearExistingMapSections)
		{
			var job = MapWindowHelper.BuildJob(Project, jobName, canvasControlSize, mSetInfo, newArea, BlockSize, _projectAdapter, clearExistingMapSections);
			Debug.WriteLine($"The new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta}.");
			var genMapRequestInfo = new GenMapRequestInfo(job, newArea, 0, null);

			_requestStack.Add(genMapRequestInfo);
		}

		public void GoBack(SizeInt canvasControlSize, bool clearExistingMapSections)
		{
			// Remove the current request
			_requestStack.RemoveAt(_requestStack.Count - 1);

			// Remove and then reload the one prior to that
			var prevRequest = _requestStack[^1];
			_requestStack.RemoveAt(_requestStack.Count - 1);
			var mSetInfo = prevRequest.Job.MSetInfo;
			var newArea = prevRequest.NewArea;

			LoadMap(prevRequest.Job.Label, canvasControlSize, mSetInfo, newArea, clearExistingMapSections);
		}

		public Point GetBlockPosition(Point posYInverted)
		{
			var pointInt = new PointInt((int)posYInverted.X, (int)posYInverted.Y);

			var curReq = CurrentRequest;
			var mapBlockOffset = curReq?.Job.MapBlockOffset ?? new SizeInt();

			var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

			return new Point(blockPos.X, blockPos.Y);
		}

		#endregion
	}
}
