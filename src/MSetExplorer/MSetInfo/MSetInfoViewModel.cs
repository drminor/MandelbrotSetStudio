using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private int _targetIterations;
		private int _iterationsPerRequest;

		public MSetInfoViewModel()
		{
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public int TargetIterations
		{
			get => _targetIterations;
			set
			{
				if (value != _targetIterations)
				{
					_targetIterations = value;
					OnPropertyChanged();
					MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, value));
				}
			}
		}

		public int IterationsPerRequest
		{
			get => _iterationsPerRequest;
			set
			{
				if (value != _iterationsPerRequest)
				{
					_iterationsPerRequest = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public MSetInfo GetMSetInfo()
		{
			var result = new MSetInfo(new RRectangle(), new MapCalcSettings(_targetIterations, _targetIterations));
			return result;
		}

		public void SetMSetInfo(MSetInfo? value)
		{
			if (value == null)
			{
				_targetIterations = 0;
				_iterationsPerRequest = 0;
			}
			else
			{
				TargetIterations = value.MapCalcSettings.TargetIterations;
				IterationsPerRequest = value.MapCalcSettings.IterationsPerRequest;
			}
		}

		#endregion

	}
}
