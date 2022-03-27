
using MSS.Common;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private int _targetIterations;
		private int _steps;

		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;

		#region Constructor

		public MainWindowViewModel(IMapProjectViewModel mapProjectViewModel, IMapDisplayViewModel mapDisplayViewModel, ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, ColorBandSetViewModel colorBandViewModel)
		{
			MapProjectViewModel = mapProjectViewModel;
			MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;

			_projectOpenSaveViewModelCreator = projectOpenSaveViewModelCreator;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					ColorBandSetViewModel.HighCutOff = value;
					MapProjectViewModel.UpdateTargetInterations(value, Steps);
					OnPropertyChanged();
				}
			}
		}

		public int Steps
		{
			get => _steps;
			set { _steps = value; OnPropertyChanged(); }
		}

		#endregion

		#region Public Methods

		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string initalName, DialogType dialogType)
		{
			var result = _projectOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProject))
			{
				ColorBandSetViewModel.CurrentProject = MapProjectViewModel.CurrentProject;

				// TODO: Use the ColorBandSet from the MapProjectViewModel or use the ColorBandSet from the ColorBandSetViewModel.
				MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				var curJob = MapProjectViewModel.CurrentJob;

				if (curJob != null)
				{
					MapDisplayViewModel.CurrentJob = curJob;

					var mapCalcSettings = curJob.MSetInfo.MapCalcSettings;

					if (mapCalcSettings.TargetIterations != _targetIterations)
					{
						_targetIterations = mapCalcSettings.TargetIterations;

						// TODO: Update the HighCutOff for the current ColorBandSet property of the ColorBandSetViewModel

						OnPropertyChanged(nameof(TargetIterations));
					}

					if (mapCalcSettings.IterationsPerRequest != _steps)
					{
						_steps = mapCalcSettings.IterationsPerRequest;
						OnPropertyChanged(nameof(Steps));
					}
				}
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object sender, MapViewUpdateRequestedEventArgs e)
		{
			MapProjectViewModel.UpdateMapView(e.TransformType, e.NewArea);
		}

		private void MapDisplayViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		private void ColorBandViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ColorBandSet")
			{
				MapProjectViewModel.CurrentColorBandSet = ColorBandSetViewModel.ColorBandSet;
			}
		}

		#endregion
	}
}
