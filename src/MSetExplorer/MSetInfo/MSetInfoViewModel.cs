using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MSetExplorer
{
	public class MSetInfoViewModel : ViewModelBase
	{
		private string _startingX;
		private string _endingX;
		private string _startingY;
		private string _endingY;

		private RRectangle _coords;
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

			_startingX =  ConvertToString(_coords.Left);
			_endingX = ConvertToString(_coords.Right);
			_startingY = ConvertToString(_coords.Bottom);
			_endingY = ConvertToString(_coords.Top);
		}

		#region Public Properties

		public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		private bool _coordsAreDirty;

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

		private Job? _currentJob;

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
				if (value != _startingX)
				{
					_startingX = value;
					var rValue = ConvertToRValue(value, 0);
					if (rValue != _coords.Left)
					{
						Coords = RMapHelper.UpdatePointValue(_coords, 0, rValue);
					}

					OnPropertyChanged();
				}

				//if (value != _currentJob?.MSetInfo.Coords.Left)
				//{

				//}

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
					var rValue = ConvertToRValue(value, 0);
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
					var rValue = ConvertToRValue(value, 0);
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
					var rValue = ConvertToRValue(value, 0);
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
					StartingX = ConvertToString(_coords.Left);
					EndingX = ConvertToString(_coords.Right);
					StartingY = ConvertToString(_coords.Bottom);
					EndingY = ConvertToString(_coords.Top);

					CoordsAreDirty = _currentJob != null && _currentJob.MSetInfo.Coords != value;

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

		public string Test(string s, int exp)
		{
			if (TryConvertToRValue(s, exp, out var rValue))
			{
				//var s3 = rValue.ToString();
				var s2 = ConvertToString(rValue);
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
			//TriggerCoordsUpdate();
		}

		public void TriggerIterationUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations));
		}

		public void TriggerCoordsUpdate()
		{
			MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.Coordinates, Coords));
		}

		#endregion

		#region Private Methods


		private RValue ConvertToRValue(string s, int exponent)
		{
			if (double.TryParse(s, out var dValue))
			{
				return ConvertToRValue(dValue, exponent);
			}
			else
			{
				return new RValue();
			}
		}

		private string ConvertToString(RValue rValue)
		{
			var dVals = BigIntegerHelper.ConvertToDoubles(rValue.Value, rValue.Exponent);
			var nsis = dVals.Select(x => new NumericStringInfo(x)).ToArray();

			var stage = nsis[0];

			for (var i = 1; i < nsis.Length; i++)
			{
				var t = stage.Add(nsis[i]);
				var st = t.GetString();
				stage = t;
			}

			var result = stage.GetString();

			return result;
		}

		//private string ConvertToString(RValue rValue)
		//{
		//	string result;

		//	if (BigIntegerHelper.TryConvertToDouble(rValue, out var dValue))
		//	{
		//		result = dValue.ToString("G20", CultureInfo.InvariantCulture);
		//	}
		//	else
		//	{
		//		result = "error";
		//	}

		//	return result;
		//}

		private bool TryConvertToRValue(string s, int exponent, out RValue value)
		{
			if (double.TryParse(s, out var dValue))
			{
				value = ConvertToRValue(dValue, exponent);
				return true;
			}
			else
			{
				value = new RValue();
				return false;
			}
		}

		private RValue ConvertToRValue(double d, int exponent)
		{
			//var wp = Math.Truncate(d);
			//d = d - wp;
			//var l2 = Math.Log2(d);
			//var l2a = (int) Math.Round(l2, MidpointRounding.AwayFromZero);
			//var newD = d * Math.Pow(2, -1 * l2a);
			//newD += wp;

			var origD = d;

			while (Math.Abs(d - Math.Truncate(d)) > 0.000001)
			{
				//var t = Math.Abs(d - Math.Truncate(d));

				//var ds = d.ToString("G12");

				Debug.WriteLine($"Still multiplying. NewD: {d:G12}, StartD: {origD:G12}.");

				d *= 2;
				exponent--;
			}

			var result = new RValue((BigInteger)d, exponent);
			return result;
		}

		#endregion

	}
}
