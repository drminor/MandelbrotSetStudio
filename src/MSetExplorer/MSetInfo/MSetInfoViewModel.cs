using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		//private RRectangle _coords;
		//private int _targetIterations;
		//private int _requestsPerJob;

		private MSetInfo _currentMSetInfo;

		public MSetInfoViewModel()
		{
			_currentMSetInfo = new MSetInfo(new RRectangle(), new MapCalcSettings());
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public RRectangle Coords
		{
			get => _currentMSetInfo.Coords;
			set
			{
				if (value != _currentMSetInfo.Coords)
				{
					_currentMSetInfo = MSetInfo.UpdateWithNewCoords(_currentMSetInfo, value);
					OnPropertyChanged();
				}
			}
		}
		
		public int TargetIterations
		{
			get => _currentMSetInfo.MapCalcSettings.TargetIterations;
			set
			{
				if (value != _currentMSetInfo.MapCalcSettings.TargetIterations)
				{
					_currentMSetInfo = MSetInfo.UpdateWithNewIterations(_currentMSetInfo, value, RequestsPerJob);
					OnPropertyChanged();
				}
			}
		}

		public int RequestsPerJob
		{
			get => _currentMSetInfo.MapCalcSettings.RequestsPerJob;
			set
			{
				if (value != _currentMSetInfo.MapCalcSettings.RequestsPerJob)
				{
					_currentMSetInfo = MSetInfo.UpdateWithNewIterations(_currentMSetInfo, TargetIterations, value);
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
					MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));
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
