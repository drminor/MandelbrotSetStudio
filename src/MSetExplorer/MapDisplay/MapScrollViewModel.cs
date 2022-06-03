using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		private JobAreaAndCalcSettings? _currentJobAreaAndCalcSettings;

		private double _vMax;
		private double _hMax;

		private double _verticalPosition;
		private double _horizontalPosition;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
			VMax = 1024;
			HMax = 1024;
		}

		#endregion

		#region Public Properties 

		public IMapDisplayViewModel MapDisplayViewModel { get; init; }

		public JobAreaAndCalcSettings? CurrentJobAreaAndCalcSettings
		{
			get => _currentJobAreaAndCalcSettings;
			set
			{
				if (value != _currentJobAreaAndCalcSettings)
				{
					var previousValue = _currentJobAreaAndCalcSettings;
					_currentJobAreaAndCalcSettings = value?.Clone();

					MapDisplayViewModel.CurrentJobAreaAndCalcSettings = _currentJobAreaAndCalcSettings;

					HandleCurrentJobChanged(previousValue, _currentJobAreaAndCalcSettings);
					OnPropertyChanged(nameof(IMapScrollViewModel.CurrentJobAreaAndCalcSettings));
				}
			}
		}

		public double VerticalPosition
		{
			get => _verticalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_verticalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.VerticalPosition));
				}
			}
		}

		public double HorizontalPosition
		{
			get => _horizontalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_horizontalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.HorizontalPosition));
				}
			}
		}

		public double VMax
		{
			get => _vMax;
			set
			{
				if (value != _vMax)
				{
					_vMax = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.VMax));
				}
			}
		}

		public double HMax
		{
			get => _hMax;
			set
			{
				if (value != _hMax)
				{
					_hMax = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.HMax));
				}
			}
		}

		#endregion

		#region Public Methods

		public double VerticalViewPortSize => MapDisplayViewModel.CanvasSize.Height;

		public double HorizontalViewPortSize => MapDisplayViewModel.CanvasSize.Width;

		#endregion

		#region Private Methods

		private void HandleCurrentJobChanged(JobAreaAndCalcSettings? previousJob, JobAreaAndCalcSettings? newJob)
		{
			//Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
		}

		#endregion

	}
}
