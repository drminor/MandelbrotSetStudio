using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Globalization;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private Job _currentJob;

		private string _startingX;
		private string _endingX;
		private string _startingY;
		private string _endingY;

		private long _zoom;

		private RRectangle _coords;
		private bool _coordsAreDirty;

		private int _targetIterations;
		private double _targetIterationsAvailable;
		private int _requestsPerJob;

		public MSetInfoViewModel()
		{
			_currentJob = new Job();

			_coords = _currentJob.Coords;
			_targetIterations = _currentJob.MapCalcSettings.TargetIterations;
			_requestsPerJob = _currentJob.MapCalcSettings.RequestsPerJob;

			_startingX = RValueHelper.ConvertToString(_coords.Left);
			_endingX = RValueHelper.ConvertToString(_coords.Right);
			_startingY = RValueHelper.ConvertToString(_coords.Bottom);
			_endingY = RValueHelper.ConvertToString(_coords.Top);
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public Job CurrentJob
		{
			get => _currentJob;
			set
			{
				if (value != _currentJob)
				{
					_currentJob = value;
					Coords = value.Coords.Clone();
					TargetIterations = value.MapCalcSettings.TargetIterations;
					RequestsPerJob = value.MapCalcSettings.RequestsPerJob;
					

					CoordsAreDirty = false;
				}
			}
		}

		public long Zoom
		{
			get => _zoom;
			set
			{
				_zoom = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Precision));
			}
		}

		public int Precision
		{
			get => -1 * _currentJob?.Subdivision.SamplePointDelta.Exponent ?? 0;
			set => OnPropertyChanged();
		}

		public string StartingX
		{
			get => _startingX;
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					var cItems = value.Split('\n');
					if (cItems.Length == 4)
					{
						SetMulti(cItems);
						//TestMulti(cItems);
					}
					else
					{
						value = cItems[0];
						if (value != _startingX)
						{
							_startingX = value;
							//var rValue = RValueHelper.ConvertToRValue(value);
							//if (rValue != _coords.Left)
							//{
							//	Coords = RMapHelper.UpdatePointValue(_coords, 0, rValue);
							//}

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
					//var rValue = RValueHelper.ConvertToRValue(value);
					//if (rValue != _coords.Right)
					//{
					//	Coords = RMapHelper.UpdatePointValue(_coords, 1, rValue);
					//}

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
					//var rValue = RValueHelper.ConvertToRValue(value);
					//if (rValue != _coords.Bottom)
					//{
					//	Coords = RMapHelper.UpdatePointValue(_coords, 2, rValue);
					//}

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
					//var rValue = RValueHelper.ConvertToRValue(value);
					//if (rValue != _coords.Top)
					//{
					//	Coords = RMapHelper.UpdatePointValue(_coords, 3, rValue);
					//}

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

					CoordsAreDirty = _currentJob == null ? false : value == _currentJob.Coords;

					Zoom = RValueHelper.GetResolution(_coords.Width);
					//Precision = precision;

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

		public void SaveCoords() 
		{
			// TODO: Validate new values

			if (CurrentJob.IsEmpty)
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

			var x1Updated = false;
			var x2Updated = false;
			var y1Updated = false;
			var y2Updated = false;

			var nValue = mVals[0];
			if (nValue != _startingX)
			{
				_startingX = nValue;
				var rValue = RValueHelper.ConvertToRValue(nValue);
				x1Updated = rValue != _coords.Left;
				if (x1Updated)
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
					var rValue = RValueHelper.ConvertToRValue(nValue);
					x2Updated = rValue != _coords.Right;
					if (x2Updated)
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
					var rValue = RValueHelper.ConvertToRValue(nValue);
					y1Updated = rValue != _coords.Bottom;
					if (y1Updated)
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
					var rValue = RValueHelper.ConvertToRValue(nValue);
					y2Updated = rValue != _coords.Top;
					if (y2Updated)
					{
						work = RMapHelper.UpdatePointValue(work, 3, rValue);
					}
				}
			}

			Coords = work;

			if (x1Updated)
			{
				OnPropertyChanged(nameof(StartingX));
			}

			if (x2Updated)
			{
				OnPropertyChanged(nameof(EndingX));
			}

			if (y1Updated)
			{
				OnPropertyChanged(nameof(StartingY));
			}

			if (y2Updated)
			{
				OnPropertyChanged(nameof(EndingY));
			}

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
