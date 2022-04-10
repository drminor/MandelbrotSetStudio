using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Globalization;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private string _startingX;
		private RRectangle _coords;
		private int _targetIterations;
		private int _requestsPerJob;

		private MSetInfo _currentMSetInfo;

		public MSetInfoViewModel()
		{
			_currentMSetInfo = new MSetInfo(new RRectangle(), new MapCalcSettings());
			_coords = _currentMSetInfo.Coords;
			_startingX = _coords.Values[0].ToString(CultureInfo.InvariantCulture);
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public string StartingX
		{
			get => _startingX;
			set
			{
				if (value != _startingX)
				{
					_startingX = value;
					if (value != _coords.Values[0].ToString(CultureInfo.InvariantCulture))
					{
						Coords = new RRectangle(ConvertToRValue(value).Value, _coords.Values[1], _coords.Values[2], _coords.Values[3], _coords.Exponent);
					}

					OnPropertyChanged();
				}
			}
		}

		private RValue ConvertToRValue(string s)
		{
			return new RValue(1205, 0);
		}

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					StartingX = _coords.Values[0].ToString(CultureInfo.InvariantCulture);

					if (value != _currentMSetInfo.Coords)
					{
						MSetInfo = MSetInfo.UpdateWithNewCoords(_currentMSetInfo, value);
					}
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
					if (value != _currentMSetInfo.MapCalcSettings.TargetIterations)
					{
						MSetInfo = MSetInfo.UpdateWithNewIterations(_currentMSetInfo, value);
					}
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
					if (value != _currentMSetInfo.MapCalcSettings.RequestsPerJob)
					{
						MSetInfo = MSetInfo.UpdateWithNewRequestsPerJob(_currentMSetInfo, value);
					}
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

					Coords = _currentMSetInfo.Coords;
					TargetIterations = _currentMSetInfo.MapCalcSettings.TargetIterations;
					RequestsPerJob = _currentMSetInfo.MapCalcSettings.RequestsPerJob;
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

		public void TriggerCoordsUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.Coordinates, Coords));
		}

		#endregion

	}
}
