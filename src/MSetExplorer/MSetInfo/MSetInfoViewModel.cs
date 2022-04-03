using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private RRectangle _coords;
		private int _targetIterations;
		private int _requestsPerJob;

		private MSetInfo _currentMSetInfo;

		public MSetInfoViewModel()
		{
			_currentMSetInfo = new MSetInfo(new RRectangle(), new MapCalcSettings());
			_coords = _currentMSetInfo.Coords;
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					_currentMSetInfo = MSetInfo.UpdateWithNewCoords(_currentMSetInfo, value);
					OnPropertyChanged();
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
					_currentMSetInfo = MSetInfo.UpdateWithNewIterations(_currentMSetInfo, value);
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
					_currentMSetInfo = MSetInfo.UpdateWithNewRequestsPerJob(_currentMSetInfo, value);
					OnPropertyChanged();
				}
			}
		}

		public MSetInfo? MSetInfo
		{
			get => _currentMSetInfo;
			set
			{
				if (value != _currentMSetInfo)
				{
					_currentMSetInfo = value ?? new MSetInfo(new RRectangle(), new MapCalcSettings());
					OnPropertyChanged();

					Coords = _currentMSetInfo.Coords;
					TargetIterations = _currentMSetInfo.MapCalcSettings.TargetIterations;
					RequestsPerJob = _currentMSetInfo.MapCalcSettings.RequestsPerJob;

					//MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));
				}
			}
		}

		#endregion

		#region Public Methods

		//public MSetInfo GetMSetInfo()
		//{
		//	var result = new MSetInfo(_coords, new MapCalcSettings(_targetIterations, _requestsPerJob));
		//	return result;
		//}

		//public void SetMSetInfo(MSetInfo? value)
		//{
		//	if (value == null)
		//	{
		//		_coords = new RRectangle();
		//		_targetIterations = 0;
		//		_requestsPerJob = 0;
		//	}
		//	else
		//	{
		//		Coords = value.Coords;
		//		TargetIterations = value.MapCalcSettings.TargetIterations;
		//		RequestsPerJob = value.MapCalcSettings.RequestsPerJob;

		//		MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));

		//	}
		//}

		public void TriggerIterationUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));
		}

		#endregion

	}
}
