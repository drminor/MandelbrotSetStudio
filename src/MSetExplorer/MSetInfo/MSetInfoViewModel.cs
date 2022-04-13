using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private static readonly MSetInfo NULL_MSET_INFO = new(new RRectangle(), new MapCalcSettings());

		private Job? _currentJob;

		private string _startingX;
		private string _endingX;
		private string _startingY;
		private string _endingY;

		private RRectangle _coords;
		private bool _coordsAreDirty;

		private int _targetIterations;
		private int _requestsPerJob;

		private MSetInfo _currentMSetInfo;

		public MSetInfoViewModel()
		{
			_currentMSetInfo = new MSetInfo
				(
				new RRectangle(), 
				new MapCalcSettings()
				);

			_coords = _currentMSetInfo.Coords;

			_startingX = RValueHelper.ConvertToString(_coords.Left);
			_endingX = RValueHelper.ConvertToString(_coords.Right);
			_startingY = RValueHelper.ConvertToString(_coords.Bottom);
			_endingY = RValueHelper.ConvertToString(_coords.Top);
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public Job? CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value is null)
				{
					if (_currentJob != null)
					{
						_currentJob = value;
						MSetInfo = null;
						CoordsAreDirty = false;
					}
				}
				else
				{
					if (value != _currentJob)
					{
						_currentJob = value;
						MSetInfo = value.MSetInfo.Clone();
						CoordsAreDirty = false;
					}
				}
			}
		}

		public string StartingX
		{
			get => _startingX;
			set
			{
				//var cItems = Array.Empty<string>();
				if (!string.IsNullOrEmpty(value))
				{
					var cItems = value.Split('\n');
					if (cItems.Length > 0)
					{
						SetMulti(cItems);
					}
					else
					{
						if (value != _startingX)
						{
							_startingX = value;
							var rValue = RValueHelper.ConvertToRValue(value, 0);
							if (rValue != _coords.Left)
							{
								Coords = RMapHelper.UpdatePointValue(_coords, 0, rValue);
							}

							OnPropertyChanged();
						}
					}
				}
			}
		}

		public string EndingX
		{
			get => _endingX;
			set
			{
				if (value != _endingX)
				{
					_endingX = value;
					var rValue = RValueHelper.ConvertToRValue(value, 0);
					if (rValue != _coords.Right)
					{
						Coords = RMapHelper.UpdatePointValue(_coords, 1, rValue);
					}

					OnPropertyChanged();
				}
			}
		}

		public string StartingY
		{
			get => _startingY;
			set
			{
				if (value != _startingY)
				{
					_startingY = value;
					var rValue = RValueHelper.ConvertToRValue(value, 0);
					if (rValue != _coords.Bottom)
					{
						Coords = RMapHelper.UpdatePointValue(_coords, 2, rValue);
					}

					OnPropertyChanged();
				}
			}
		}

		public string EndingY
		{
			get => _endingY;
			set
			{
				if (value != _endingY)
				{
					_endingY = value;
					var rValue = RValueHelper.ConvertToRValue(value, 0);
					if (rValue != _coords.Top)
					{
						Coords = RMapHelper.UpdatePointValue(_coords, 3, rValue);
					}

					OnPropertyChanged();
				}
			}
		}

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					StartingX = RValueHelper.ConvertToString(_coords.Left);
					EndingX = RValueHelper.ConvertToString(_coords.Right);
					StartingY = RValueHelper.ConvertToString(_coords.Bottom);
					EndingY = RValueHelper.ConvertToString(_coords.Top);

					CoordsAreDirty = value != (_currentJob?.MSetInfo ?? NULL_MSET_INFO).Coords;

					if (value != _currentMSetInfo.Coords)
					{
						MSetInfo = MSetInfo.UpdateWithNewCoords(_currentMSetInfo, value);
					}

					OnPropertyChanged();
				}
			}
		}

		public bool CoordsAreDirty
		{
			get => _coordsAreDirty;

			private set
			{
				if (value != _coordsAreDirty)
				{
					_coordsAreDirty = value;
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
					_currentMSetInfo = value ?? NULL_MSET_INFO;

					Coords = _currentMSetInfo.Coords;
					TargetIterations = _currentMSetInfo.MapCalcSettings.TargetIterations;
					RequestsPerJob = _currentMSetInfo.MapCalcSettings.RequestsPerJob;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public string Test(string s, int exp)
		{
			if (RValueHelper.TryConvertToRValue(s, exp, out var rValue))
			{
				//var s3 = rValue.ToString();
				var s2 = RValueHelper.ConvertToString(rValue);
				return s2;
			}
			else
			{
				return "bad RVal";
			}
		}

		public void SaveCoords() 
		{
			// TODO: Validate new values

			if (CurrentJob is null)
			{
				if (TargetIterations == 0)
				{
					TargetIterations = 700;
				}

				if (RequestsPerJob == 0)
				{
					RequestsPerJob = 100;
				}

				MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.NewProject, Coords, TargetIterations, RequestsPerJob));
			}
			else
			{
				MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.Coordinates, Coords));
			}
		}

		public void TriggerIterationUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations));
		}

		#endregion

		#region Private Methods

		private void SetMulti(string[] mVals)
		{
			var work = _coords;

			var nValue = mVals[0];
			if (nValue != _startingX)
			{
				_startingX = nValue;
				var rValue = RValueHelper.ConvertToRValue(nValue, 0);
				if (rValue != _coords.Left)
				{
					work = RMapHelper.UpdatePointValue(work, 0, rValue);
				}
			}

			if (mVals.Length > 1)
			{
				nValue = mVals[1];
				if (nValue != _endingX)
				{
					_endingX = nValue;
					var rValue = RValueHelper.ConvertToRValue(nValue, 0);
					if (rValue != _coords.Right)
					{
						work = RMapHelper.UpdatePointValue(work, 1, rValue);
					}
				}
			}

			if (mVals.Length > 2)
			{
				nValue = mVals[2];
				if (nValue != _startingY)
				{
					_startingY = nValue;
					var rValue = RValueHelper.ConvertToRValue(nValue, 0);
					if (rValue != _coords.Bottom)
					{
						work = RMapHelper.UpdatePointValue(work, 2, rValue);
					}
				}
			}

			if (mVals.Length > 3)
			{
				nValue = mVals[3];
				if (nValue != _endingY)
				{
					_endingY = nValue;
					var rValue = RValueHelper.ConvertToRValue(nValue, 0);
					if (rValue != _coords.Top)
					{
						work = RMapHelper.UpdatePointValue(work, 3, rValue);
					}
				}
			}

			Coords = work;
			OnPropertyChanged(nameof(StartingX));
			OnPropertyChanged(nameof(EndingX));
			OnPropertyChanged(nameof(StartingY));
			OnPropertyChanged(nameof(EndingY));

			if (TargetIterations == 0)
			{
				TargetIterations = 700;
			}

			if (RequestsPerJob == 0)
			{
				RequestsPerJob = 100;
			}
		}

		#endregion
	}
}
