using System.ComponentModel;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
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
			MSetInfoViewModel.MapSettingsUpdateRequested += MSetInfoViewModel_MapSettingsUpdateRequested;
		}

		private void MSetInfoViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			if (e.MapSettingsUpdateType == MSS.Types.MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.HighCutOff = e.TargetIterations;
				MapProjectViewModel.UpdateTargetInterations(e.TargetIterations, e.RequestsPerJob);
			}
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public MSetInfoViewModel MSetInfoViewModel { get; }

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
				var curJob = MapProjectViewModel.CurrentJob;

				MSetInfoViewModel.MSetInfo = curJob?.MSetInfo;
				MapDisplayViewModel.CurrentJob = curJob;
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
			if (e.PropertyName == nameof(ColorBandSetViewModel.ColorBandSet))
			{
				MapProjectViewModel.CurrentColorBandSet = ColorBandSetViewModel.ColorBandSet;
			}
		}

		#endregion
	}
}
