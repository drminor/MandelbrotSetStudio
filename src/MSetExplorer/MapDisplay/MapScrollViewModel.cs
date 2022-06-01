using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		private JobAreaAndCalcSettings? _currentJobAreaAndCalcSettings;
		private double _verticalPosition;
		private double _horizontalPosition;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
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
					OnPropertyChanged();
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
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public double GetVMax()
		{
			return 1024;
		}

		public double GetHMax()
		{
			return 1024;
		}

		public double GetVerticalViewPortSize()
		{
			return 1024;
		}

		public double GetHorizontalViewPortSize()
		{
			return 1024;
		}

		#endregion

		#region Private Methods

		private void HandleCurrentJobChanged(JobAreaAndCalcSettings? previousJob, JobAreaAndCalcSettings? newJob)
		{
			//Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {newJob?.Id ?? ObjectId.Empty}");
		}

		#endregion

	}
}
