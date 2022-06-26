using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapCalcSettingsViewModel : ViewModelBase
	{
		private MapCalcSettings _mapCalcSettings;

		//private int _targetIterations;
		//private int _requestsPerJob;
		private double _targetIterationsAvailable;

		public MapCalcSettingsViewModel()
		{
			_mapCalcSettings = new MapCalcSettings();

			//_targetIterations = _mapCalcSettings.TargetIterations;
			//_requestsPerJob = _mapCalcSettings.RequestsPerJob;
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public MapCalcSettings MapCalcSettings
		{
			get => _mapCalcSettings;
			set
			{
				if (value != _mapCalcSettings)
				{
					_mapCalcSettings = value;
					OnPropertyChanged(nameof(TargetIterations));
					OnPropertyChanged(nameof(RequestsPerJob));
					//TargetIterations = _mapCalcSettings.TargetIterations;
					//RequestsPerJob = _mapCalcSettings.RequestsPerJob;
				}
			}
		}

		//public int TargetIterations
		//{
		//	get => _targetIterations;
		//	set
		//	{
		//		if (value != _targetIterations)
		//		{
		//			_targetIterations = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		public int TargetIterations
		{
			get => _mapCalcSettings.TargetIterations;
			set
			{
				if (value != _mapCalcSettings.TargetIterations)
				{
					//_mapCalcSettings = new MapCalcSettings(value, _mapCalcSettings.RequestsPerJob);
					//OnPropertyChanged();
					TriggerIterationUpdate(value);
				}
			}
		}

		//public int RequestsPerJob
		//{
		//	get => _requestsPerJob;
		//	set
		//	{
		//		if (value != _requestsPerJob)
		//		{
		//			_requestsPerJob = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		public int RequestsPerJob
		{
			get => _mapCalcSettings.RequestsPerJob;
			set
			{
				if (value != RequestsPerJob)
				{
					_mapCalcSettings = new MapCalcSettings(_mapCalcSettings.TargetIterations, value);
					OnPropertyChanged();
				}
			}
		}

		public double TargetIterationsAvailable
		{
			get => _targetIterationsAvailable;
			set
			{
				if (value != _targetIterationsAvailable)
				{
					_targetIterationsAvailable = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public void TriggerIterationUpdate(int newValue)
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, newValue));
		}

		#endregion
	}
}
