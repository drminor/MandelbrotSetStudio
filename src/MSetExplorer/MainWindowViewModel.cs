using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private readonly SizeInt _blockSize;
		private readonly ProjectAdapter _projectAdapter;

		private SizeInt _canvasSize;
		private int _iterations;
		private int _steps;

		#region Constructor

		public MainWindowViewModel(ProjectAdapter projectAdapter, IMapDisplayViewModel mapDisplayViewModel, IMapLoaderJobStack mapLoaderJobStack)
		{
			_projectAdapter = projectAdapter;
			_blockSize = mapDisplayViewModel.BlockSize;

			MapDisplayViewModel = mapDisplayViewModel;
			MapLoaderJobStack = mapLoaderJobStack;
			MapLoaderJobStack.CurrentJobChanged += MapLoaderJobStack_CurrentJobChanged;

			Project = _projectAdapter.GetOrCreateProject("Home");
		}

		#endregion

		private void MapLoaderJobStack_CurrentJobChanged(object sender, EventArgs e)
		{
			OnPropertyChanged("CanGoBack");
			OnPropertyChanged("CanGoForward");
		}

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapLoaderJobStack MapLoaderJobStack { get; }

		public Project Project { get; private set; }

		public Job CurrentJob => MapLoaderJobStack.CurrentJob;

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set { _canvasSize = value; OnPropertyChanged(); }
		}

		public int Iterations
		{
			get => _iterations;
			set { _iterations = value; OnPropertyChanged(); }
		}

		public int Steps
		{
			get => _steps;
			set { _steps = value; OnPropertyChanged(); }
		}

		public bool CanGoBack => MapLoaderJobStack.CanGoBack;
		public bool CanGoForward => MapLoaderJobStack.CanGoForward;
		#endregion

		#region Public Methods

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, TransformType.None, newArea);
		}

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var newArea = e.Area;
			UpdateMapView(TransformType.Zoom, newArea);
		}

		public void UpdateMapViewPan(ScreenPannedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var newArea = new RectangleInt(new PointInt(invOffset), CanvasSize);
			UpdateMapView(TransformType.Pan, newArea);
		}

		public void GoBack()
		{
			var _ = MapLoaderJobStack.GoBack();
		}

		public void GoForward()
		{
			var _ = MapLoaderJobStack.GoForward();
		}

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			foreach (var job in MapLoaderJobStack.Jobs)
			{
				if (job.Id.CreationTime > lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					MapLoaderJobStack.UpdateJob(job, updatedJob);
				}
			}
		}

		public void LoadProject()
		{
			var jobs = _projectAdapter.GetAllJobs(Project.Id);
			MapLoaderJobStack.LoadJobStack(jobs);
		}

		#endregion

		#region Private Methods 

		private void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Zoom}. SamplePointDelta: {samplePointDelta}");

			var mSetInfo = MapLoaderJobStack.CurrentJob.MSetInfo;

			var updatedInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
			{
				updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
			}

			LoadMap(updatedInfo, transformType, newArea);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			//CheckViewModel();
			var jobName = GetJobName(transformType);
			var parentJob = MapLoaderJobStack.CurrentJob;

			var job = MapWindowHelper.BuildJob(parentJob, Project, jobName, CanvasSize, mSetInfo, transformType, newArea, _blockSize, _projectAdapter);
			//Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");

			MapLoaderJobStack.Push(job);
		}

		private string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		//[Conditional("Debug")]
		//private void CheckViewModel()
		//{
		//	Debug.Assert(MapDisplayViewModel.CanvasSize == CanvasSize, "Canvas Sizes don't match on CheckViewModel.");
		//}

		#endregion
	}
}
