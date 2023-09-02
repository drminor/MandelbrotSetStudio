using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MapCalcSettingsViewModel : ViewModelBase
	{
		private MapCalcSettings _mapCalcSettings;
		private double _targetIterationsAvailable;

		public MapCalcSettingsViewModel()
		{
			_mapCalcSettings = new MapCalcSettings();
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
					OnPropertyChanged(nameof(SaveTheZValues));
				}
			}
		}

		public int TargetIterations
		{
			get => _mapCalcSettings.TargetIterations;
			set
			{
				if (value != _mapCalcSettings.TargetIterations)
				{
					_mapCalcSettings = MapCalcSettings.UpdateTargetIterations(_mapCalcSettings, value);	// ADDED: 4/8/2023 -- not tested.
					TriggerIterationUpdate(value);
				}
			}
		}

		public int Threshold
		{
			get => _mapCalcSettings.Threshold;
			set
			{
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

		public bool SaveTheZValues
		{
			get => _mapCalcSettings.SaveTheZValues;
			set
			{
				if (value != _mapCalcSettings.SaveTheZValues)
				{
					_mapCalcSettings = MapCalcSettings.UpdateSaveTheZValues(_mapCalcSettings, value); // ADDED: 4/8/2023 -- not tested.
					TriggerIterationUpdate(value);
				}
			}
		}

		#endregion

		#region Public Methods

		public void TriggerIterationUpdate(int newValue)
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, newValue));
		}

		public void TriggerIterationUpdate(bool newValue)
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.SaveTheZValues, newValue));
		}

		#endregion
	}
}
