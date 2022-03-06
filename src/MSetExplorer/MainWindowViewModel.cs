using MSetRepo;
using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private readonly ProjectAdapter _projectAdapter;

		private Project _currentProject;
		private int _iterations;
		private int _steps;

		#region Constructor

		public MainWindowViewModel(ProjectAdapter projectAdapter, IMapDisplayViewModel mapDisplayViewModel)
		{
			_projectAdapter = projectAdapter;

			CurrentProject = _projectAdapter.GetOrCreateProject("Home");

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.CurrentProject = CurrentProject;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
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

		public Project CurrentProject
		{
			get => _currentProject;
			set { _currentProject = value; OnPropertyChanged(); }
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

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public Job CurrentJob => MapDisplayViewModel.CurrentJob;
		public bool CanGoBack => MapDisplayViewModel.CanGoBack;
		public bool CanGoForward => MapDisplayViewModel.CanGoForward;

		#endregion

		#region Public Methods

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
	}
}
