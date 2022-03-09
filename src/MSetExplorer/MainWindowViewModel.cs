
using MSS.Types;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private int _targetIterations;
		private int _steps;
		private ColorMapEntry[] _colorMapEntries;

		#region Constructor

		public MainWindowViewModel(IMapProject jobStack, IMapDisplayViewModel mapDisplayViewModel)
		{
			JobStack = jobStack;
			JobStack.CurrentJobChanged += JobStack_CurrentJobChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			JobStack.CanvasSize = MapDisplayViewModel.CanvasSize;
		}

		private void JobStack_CurrentJobChanged(object sender, System.EventArgs e)
		{
			var curJob = JobStack.CurrentJob;

			if (curJob != null)
			{
				var mapCalcSettings = curJob.MSetInfo.MapCalcSettings;
				_targetIterations = mapCalcSettings.TargetIterations;
				OnPropertyChanged(nameof(TargetIterations));

				_steps = mapCalcSettings.IterationsPerRequest;
				OnPropertyChanged(nameof(Steps));

				_colorMapEntries = curJob.MSetInfo.ColorMapEntries;
				OnPropertyChanged(nameof(ColorMapEntries));
			}

			OnPropertyChanged(nameof(IMapProject.CanGoBack));
			OnPropertyChanged(nameof(IMapProject.CanGoForward));
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
		public IMapProject JobStack { get; }

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					JobStack.UpdateTargetInterations(value, Steps);
					OnPropertyChanged();
				}
			}
		}

		public int Steps
		{
			get => _steps;
			set { _steps = value; OnPropertyChanged(); }
		}

		public ColorMapEntry[] ColorMapEntries
		{
			get => _colorMapEntries;
			set
			{
				// TODO: Compare the new value of ColorMapEntries with the current value.
				_colorMapEntries = value;
				OnPropertyChanged();
			}
		}

		#endregion

	}
}
