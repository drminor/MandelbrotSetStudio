using MapSectionProviderLib;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMapJobViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapLoaderJobStack _navStack;

		#region Constructor

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			BlockSize = blockSize;
			_projectAdapter = projectAdapter;
			_canvasSize = new SizeInt();
			_navStack = new MapLoaderJobStack(mapSectionRequestProcessor, HandleMapSectionReady, HandleMapNav);

			MapSections = new ObservableCollection<MapSection>();
			Project = _projectAdapter.GetOrCreateProject("Home");
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		public Project Project { get; private set; }

		public Job CurrentJob => _navStack.CurrentJob;
		public bool CanGoBack => _navStack.CanGoBack;
		public bool CanGoForward => _navStack.CanGoForward;

		public ObservableCollection<MapSection> MapSections { get; init; }

		private SizeInt _canvasSize;
		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set { _canvasSize = value; OnPropertyChanged(); }
		}

		#endregion

		#region Public Methods

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			LoadMap(mSetInfo, TransformType.None, newArea: new SizeInt());
		}

		public void UpdateMapViewZoom(RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.LeftBot;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Zoom}.");
			UpdateMapView(TransformType.Zoom, newArea.Size, coords);
		}

		public void UpdateMapViewPan(SizeInt offset)
		{
			var curJob = CurrentJob;
			var coords = curJob.MSetInfo.Coords;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var newSize = curJob.NewArea; // The new area is not changing

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Scale(-1d);
			var updatedCoords = RMapHelper.GetMapCoords(invOffset, coords, samplePointDelta);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Pan}. SamplePointDelta: {samplePointDelta}");
			UpdateMapView(TransformType.Pan, newSize, updatedCoords);
		}

		private void UpdateMapView(TransformType transformType, SizeInt newSize, RRectangle coords)
		{
			var mSetInfo = _navStack.CurrentJob.MSetInfo;
			var updatedInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			LoadMap(updatedInfo, transformType, newSize);
		}

		public void GoBack()
		{
			if (_navStack.GoBack())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		public void GoForward()
		{
			if (_navStack.GoForward())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		//public Point GetBlockPosition(Point posYInverted)
		//{
		//	var pointInt = new PointDbl(posYInverted.X, posYInverted.Y).Round();

		//	var curJob = _navStack.CurrentJob;
		//	var mapBlockOffset = curJob?.MapBlockOffset ?? new SizeInt();

		//	var blockPos = RMapHelper.GetBlockPosition(pointInt, mapBlockOffset, BlockSize);

		//	return new Point(blockPos.X, blockPos.Y);
		//}

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			foreach (var genMapRequestInfo in _navStack.GenMapRequests)
			{
				var job = genMapRequestInfo.Job;
				if (job.Id.CreationTime > lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					_navStack.UpdateJob(genMapRequestInfo, updatedJob);
				}
			}
		}

		public void LoadProject()
		{
			var jobs = _projectAdapter.GetAllJobs(Project.Id);
			_navStack.LoadJobStack(jobs);
		}

		#endregion

		#region Private Methods 

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, SizeInt newArea)
		{
			var jobName = GetJobName(transformType);
			var parentJob = _navStack.CurrentJob;
			var job = MapWindowHelper.BuildJob(parentJob, Project, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);
			Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");

			_navStack.Push(job);
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
		}

		private string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			MapSections.Add(mapSection);
		}

		private void HandleMapNav()
		{
			MapSections.Clear();
		}

		#endregion
	}
}
