using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapCalcSettingsViewModel : ViewModelBase
	{
		//private Job _currentJob;

		private MapCalcSettings _mapCalcSettings;

		private int _targetIterations;
		private double _targetIterationsAvailable;
		private int _requestsPerJob;

		public MapCalcSettingsViewModel()
		{
			//_currentJob = Job.Empty;
			_mapCalcSettings = new MapCalcSettings();

			_targetIterations = _mapCalcSettings.TargetIterations;
			_requestsPerJob = _mapCalcSettings.RequestsPerJob;
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		//public Job CurrentJob
		//{
		//	get => _currentJob;
		//	set
		//	{
		//		if (value != _currentJob)
		//		{
		//			_currentJob = value;
		//			TargetIterations = value.MapCalcSettings.TargetIterations;
		//			RequestsPerJob = value.MapCalcSettings.RequestsPerJob;
		//		}
		//	}
		//}

		public MapCalcSettings MapCalcSettings
		{
			get => _mapCalcSettings;
			set
			{
				if (value != _mapCalcSettings)
				{
					_mapCalcSettings = value;
					TargetIterations = _mapCalcSettings.TargetIterations;
					RequestsPerJob = _mapCalcSettings.RequestsPerJob;
				}
						
			}
		}

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					OnPropertyChanged();
				}
			}
		}

		public int RequestsPerJob
		{
			get => _requestsPerJob;
			set
			{
				if (value != _requestsPerJob)
				{
					_requestsPerJob = value;
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

		public void TriggerIterationUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations));
		}

		#endregion
	}
}
