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

		public MSetInfoViewModel()
		{
			_coords = new RRectangle();
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

		#endregion

		#region Public Methods

		public MSetInfo GetMSetInfo()
		{
			var result = new MSetInfo(_coords, new MapCalcSettings(_targetIterations, _requestsPerJob));
			return result;
		}

		public void SetMSetInfo(MSetInfo? value)
		{
			if (value == null)
			{
				_coords = new RRectangle();
				_targetIterations = 0;
				_requestsPerJob = 0;
			}
			else
			{
				Coords = value.Coords;
				TargetIterations = value.MapCalcSettings.TargetIterations;
				RequestsPerJob = value.MapCalcSettings.RequestsPerJob;

				MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));

			}
		}

		public void TriggerIterationUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations, RequestsPerJob));
		}

		#endregion

	}
}
