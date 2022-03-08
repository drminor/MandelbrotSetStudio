
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
			//JobStack.PropertyChanged += JobStack_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			JobStack.CanvasSize = MapDisplayViewModel.CanvasSize;
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object sender, MapViewUpdateRequestedEventArgs e)
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

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IJobStack JobStack { get; }

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

		#endregion

	}
}
