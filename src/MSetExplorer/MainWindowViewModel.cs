using MSetRepo;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private readonly SizeInt _blockSize;
		private readonly ProjectAdapter _projectAdapter;

		private Project _currentProject;
		private int _iterations;
		private int _steps;

		#region Constructor

		public MainWindowViewModel(ProjectAdapter projectAdapter, IMapDisplayViewModel mapDisplayViewModel)
		{
			_projectAdapter = projectAdapter;
			_blockSize = mapDisplayViewModel.BlockSize;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;

			CurrentProject = _projectAdapter.GetOrCreateProject("Home");
		}

		#endregion

		#region Event Handlers 

		private void MapDisplayViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CanGoBack))
			{
				OnPropertyChanged(nameof(CanGoBack));
			}

			if (e.PropertyName == nameof(CanGoForward))
			{
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }

		public Project CurrentProject
		{
			get => _currentProject; 
			set { _currentProject = value; OnPropertyChanged(); }
		}

		public Job CurrentJob => MapDisplayViewModel.CurrentJob;

		//public SizeInt CanvasSize
		//{
		//	get => _canvasSize;
		//	set { _canvasSize = value; OnPropertyChanged(); }
		//}

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

		public bool CanGoBack => MapDisplayViewModel.CanGoBack;
		public bool CanGoForward => MapDisplayViewModel.CanGoForward;

		#endregion

		#region Public Methods

		public void SetMapInfo(MSetInfo mSetInfo)
		{
			MapDisplayViewModel.SetMapInfo(mSetInfo);
		}

		//public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		//{
		//	MapDisplayViewModel.UpdateMapViewZoom(e);
		//}

		//public void UpdateMapViewPan(ScreenPannedEventArgs e)
		//{
		//	MapDisplayViewModel.UpdateMapViewPan(e);
		//}

		public void GoBack()
		{
			MapDisplayViewModel.GoBack();
		}

		public void GoForward()
		{
			MapDisplayViewModel.GoForward();
		}

		public void Test()
		{
			OnPropertyChanged("TestingScreenSections");
		}

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(CurrentProject.Id);

			foreach (var job in MapDisplayViewModel.Jobs)
			{
				if (job.Id.CreationTime > lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					MapDisplayViewModel.UpdateJob(job, updatedJob);
				}
			}
		}

		public void LoadProject()
		{
			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);
			MapDisplayViewModel.LoadJobStack(jobs);
		}

		#endregion

		//#region Private Methods 

		//private void UpdateMapView(TransformType transformType, RectangleInt newArea)
		//{
		//	var curJob = CurrentJob;
		//	var position = curJob.MSetInfo.Coords.Position;
		//	var samplePointDelta = curJob.Subdivision.SamplePointDelta;
		//	var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
		//	var mSetInfo = CurrentJob.MSetInfo;
		//	var updatedInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);

		//	if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
		//	{
		//		updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
		//	}

		//	Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {TransformType.Zoom}. SamplePointDelta: {samplePointDelta}");
		//	LoadMap(updatedInfo, transformType, newArea);
		//}

		//private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		//{
		//	//CheckViewModel();
		//	var parentJob = CurrentJob;
		//	var jobName = GetJobName(transformType);
		//	var canvasSize = MapDisplayViewModel.CanvasSize;
		//	var job = MapWindowHelper.BuildJob(parentJob, CurrentProject, jobName, canvasSize, mSetInfo, transformType, newArea, _blockSize, _projectAdapter);

		//	//Debug.WriteLine($"\nThe new job has a SamplePointDelta of {job.Subdivision.SamplePointDelta} and an Offset of {job.CanvasControlOffset}.\n");
		//	MapLoaderJobStack.Push(job);
		//}

		//private string GetJobName(TransformType transformType)
		//{
		//	var result = transformType == TransformType.None ? "Home" : transformType.ToString();
		//	return result;
		//}

		////[Conditional("Debug")]
		////private void CheckViewModel()
		////{
		////	Debug.Assert(MapDisplayViewModel.CanvasSize == CanvasSize, "Canvas Sizes don't match on CheckViewModel.");
		////}

		//#endregion
	}
}
