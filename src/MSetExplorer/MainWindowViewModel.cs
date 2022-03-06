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

		public MainWindowViewModel(ProjectAdapter projectAdapter, IJobStack jobStack, IMapDisplayViewModel mapDisplayViewModel)
		{
			_projectAdapter = projectAdapter;
			CurrentProject = _projectAdapter.GetOrCreateProject("Home");

			JobStack = jobStack;
			JobStack.PropertyChanged += JobStack_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.CurrentProject = CurrentProject;
		}

		#endregion

		#region Event Handlers 

		private void JobStack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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

		public IJobStack JobStack { get; }

		public Job CurrentJob => JobStack.CurrentJob;
		public bool CanGoBack => JobStack.CanGoBack;
		public bool CanGoForward => JobStack.CanGoForward;

		#endregion

		#region Public Methods

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(CurrentProject.Id);

			foreach (var job in JobStack.Jobs)
			{
				if (job.Id.CreationTime > lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					JobStack.UpdateJob(job, updatedJob);
				}
			}
		}

		public void LoadProject()
		{
			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);
			JobStack.LoadJobStack(jobs);
		}

		#endregion
	}
}
