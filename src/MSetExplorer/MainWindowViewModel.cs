using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	// TODO: Consider adding a property to make a JobCreator delegate availble by use by the MapLoaderJobStack.
	//public delegate Job JobCreator(MSetInfo mSetInfo, TransformType transformType, SizeInt newArea);
	//JobCreator jobCreator = (m, t, a) => { return new Job(); };

	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private readonly ProjectAdapter _projectAdapter;

		#region Constructor

		public MainWindowViewModel(ProjectAdapter projectAdapter, IMapDisplayViewModel mapDisplayViewModel, IMapLoaderJobStack mapLoaderJobStack)
		{
			_projectAdapter = projectAdapter;

			MapDisplayViewModel = mapDisplayViewModel;
			MapLoaderJobStack = mapLoaderJobStack;

			Project = _projectAdapter.GetOrCreateProject("Home");
		}

		#endregion

		#region Public Properties

		public Project Project { get; private set; }

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapLoaderJobStack MapLoaderJobStack { get; }


		public Job CurrentJob => MapLoaderJobStack.CurrentJob;
		public bool CanGoBack => MapLoaderJobStack.CanGoBack;
		public bool CanGoForward => MapLoaderJobStack.CanGoForward;

		private SizeInt _canvasSize;
		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set { _canvasSize = value; OnPropertyChanged(); }
		}

		private int _iterations;
		public int Iterations
		{
			get => _iterations;
			set { _iterations = value; OnPropertyChanged(); }
		}

		private int _steps;
		public int Steps
		{
			get => _steps;
			set { _steps = value; OnPropertyChanged(); }
		}

		#endregion


		#region Public Methods

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			LoadMap(mSetInfo, TransformType.None, newArea: new SizeInt());
		}

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			RectangleInt newArea = e.Area;
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Zoom}. SamplePointDelta: {samplePointDelta}");
			UpdateMapView(TransformType.Zoom, newArea.Size, coords);
		}

		public void UpdateMapViewPan(ScreenPannedEventArgs e)
		{
			SizeInt offset = e.Offset;
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
			var mSetInfo = MapLoaderJobStack.CurrentJob.MSetInfo;
			
			var updatedInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
			if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
			{
				updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
			}

			LoadMap(updatedInfo, transformType, newSize);
		}

		public void GoBack()
		{
			if (MapLoaderJobStack.GoBack())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		public void GoForward()
		{
			if (MapLoaderJobStack.GoForward())
			{
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			}
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

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, SizeInt newArea)
		{
			var jobName = GetJobName(transformType);
			var parentJob = MapLoaderJobStack.CurrentJob;
			var blockSize = MapDisplayViewModel.BlockSize;

			var job = MapWindowHelper.BuildJob(parentJob, Project, jobName, CanvasSize, mSetInfo, transformType, newArea, blockSize, _projectAdapter);
			//Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");

			MapLoaderJobStack.Push(job);
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
		}

		private string GetJobName(TransformType transformType)
		{
			var result = transformType == TransformType.None ? "Home" : transformType.ToString();
			return result;
		}

		#endregion
	}
}
