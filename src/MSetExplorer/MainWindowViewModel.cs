using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private int _iterations;
		private int _steps;

		#region Constructor

		public MainWindowViewModel(IJobStack jobStack, IMapDisplayViewModel mapDisplayViewModel)
		{
			JobStack = jobStack;
			JobStack.PropertyChanged += JobStack_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapVewUpdateRequested;

			JobStack.CanvasSize = MapDisplayViewModel.CanvasSize;
		}

		private void MapDisplayViewModel_MapVewUpdateRequested(object sender, MapViewUpdateRequestedEventArgs e)
		{
			JobStack.UpdateMapView(e.TransformType, e.NewArea);
		}

		private void MapDisplayViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				JobStack.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		#endregion

		#region Event Handlers 

		private void JobStack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IJobStack.CanGoBack))
			{
				OnPropertyChanged(nameof(CanGoBack));
			}

			if (e.PropertyName == nameof(IJobStack.CanGoForward))
			{
				OnPropertyChanged(nameof(CanGoForward));
			}
		}

		#endregion

		#region Public Properties

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

	}
}
