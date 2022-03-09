
using MSS.Types;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private int _targetIterations;
		private int _steps;
		private ColorBandSet _colorBands;

		#region Constructor

		public MainWindowViewModel(IMapProjectViewModel jobStack, IMapDisplayViewModel mapDisplayViewModel)
		{
			MapProject = jobStack;
			MapProject.CurrentJobChanged += JobStack_CurrentJobChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			MapProject.CanvasSize = MapDisplayViewModel.CanvasSize;
		}

		private void JobStack_CurrentJobChanged(object sender, System.EventArgs e)
		{
			var curJob = MapProject.CurrentJob;

			if (curJob != null)
			{
				var mapCalcSettings = curJob.MSetInfo.MapCalcSettings;
				_targetIterations = mapCalcSettings.TargetIterations;
				OnPropertyChanged(nameof(TargetIterations));

				_steps = mapCalcSettings.IterationsPerRequest;
				OnPropertyChanged(nameof(Steps));

				_colorBands = curJob.MSetInfo.ColorBands;
				OnPropertyChanged(nameof(ColorMapEntries));
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object sender, MapViewUpdateRequestedEventArgs e)
		{
			MapProject.UpdateMapView(e.TransformType, e.NewArea);
		}

		private void MapDisplayViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				MapProject.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProject { get; }

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					MapProject.UpdateTargetInterations(value, Steps);
					OnPropertyChanged();
				}
			}
		}

		public int Steps
		{
			get => _steps;
			set { _steps = value; OnPropertyChanged(); }
		}

		public ColorBandSet ColorMapEntries
		{
			get => _colorBands;
			set
			{
				if(value != _colorBands)
				{
					_colorBands = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

	}
}
