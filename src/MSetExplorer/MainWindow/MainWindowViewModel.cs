using System.ComponentModel;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		//private int _targetIterations;
		//private int _steps;

		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;
		private readonly ColorBandSetOpenSaveViewModelCreator _colorBandSetOpenSaveViewModelCreator;

		#region Constructor

		public MainWindowViewModel(IMapProjectViewModel mapProjectViewModel, IMapDisplayViewModel mapDisplayViewModel, 
			ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, ColorBandSetOpenSaveViewModelCreator colorBandSetOpenSaveViewModelCreator, 
			ColorBandSetViewModel colorBandViewModel)
		{
			MapProjectViewModel = mapProjectViewModel;
			MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;

			_projectOpenSaveViewModelCreator = projectOpenSaveViewModelCreator;
			_colorBandSetOpenSaveViewModelCreator = colorBandSetOpenSaveViewModelCreator;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;

			MSetInfoViewModel = new MSetInfoViewModel();
			//MSetInfoViewModel.SetMSetInfo(MapProjectViewModel.CurrentJob?.MSetInfo);
			MSetInfoViewModel.MapSettingsUpdateRequested += MSetInfoViewModel_MapSettingsUpdateRequested;
		}

		private void MSetInfoViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			if (e.MapSettingsUpdateType == MSS.Types.MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.HighCutOff = e.TargetIterations;
				MapProjectViewModel.UpdateTargetInterations(e.TargetIterations, 0);
			}
		}

		#endregion

		// TODO: Create a UserControl to display / edit the MapCalcSettings

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

		public MSetInfoViewModel MSetInfoViewModel { get; }


		//public int TargetIterations
		//{
		//	get => _targetIterations;
		//	set
		//	{
		//		if (value != _targetIterations)
		//		{
		//			_targetIterations = value;
		//			ColorBandSetViewModel.HighCutOff = value;
		//			MapProjectViewModel.UpdateTargetInterations(value, Steps);
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public int Steps
		//{
		//	get => _steps;
		//	set { _steps = value; OnPropertyChanged(); }
		//}

		#endregion

		#region Public Methods

		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _projectOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public IColorBandSetOpenSaveViewModel CreateAColorBandSetOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _colorBandSetOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProject))
			{
				ColorBandSetViewModel.CurrentProject = MapProjectViewModel.CurrentProject;

				MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				MSetInfoViewModel.SetMSetInfo(MapProjectViewModel.CurrentJob?.MSetInfo);

				var curJob = MapProjectViewModel.CurrentJob;

				if (curJob != null)
				{
					MapDisplayViewModel.CurrentJob = curJob;

					//var mapCalcSettings = curJob.MSetInfo.MapCalcSettings;

					//if (mapCalcSettings.TargetIterations != _targetIterations)
					//{
					//	_targetIterations = mapCalcSettings.TargetIterations;
					//	OnPropertyChanged(nameof(TargetIterations));
					//}

					//if (mapCalcSettings.IterationsPerRequest != _steps)
					//{
					//	_steps = mapCalcSettings.IterationsPerRequest;
					//	OnPropertyChanged(nameof(Steps));
					//}
				}
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			MapProjectViewModel.UpdateMapView(e.TransformType, e.NewArea);
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ColorBandSet")
			{
				MapProjectViewModel.CurrentColorBandSet = ColorBandSetViewModel.ColorBandSet;
			}
		}

		#endregion
	}
}
