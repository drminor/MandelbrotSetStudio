using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Globalization;
using System.Text;

namespace MSetExplorer
{
	public class MapCoordsViewModel : ViewModelBase
	{
		private const int _numDigitsForDisplayExtent = 4;

		private string? _jobId;
		private MapAreaInfo? _currentMapAreaInfo;

		private RRectangle _coords;

		private string _x1;
		private string _x2;
		private string _y1;
		private string _y2;
		private int _coordsExp;

		private string _width;
		private string _height;

		private int _precisionX;
		private int _precisionY;

		private string _blockOffsetX;
		private string _blockOffsetY;

		private string _samplePointDelta;
		private int _samplePointDeltaExp;

		private string _zoom;

		#region Constructor

		public MapCoordsViewModel()
		{
			_jobId = null;
			_currentMapAreaInfo = null;

			_coords = new RRectangle();
			_x1 = string.Empty;
			_x2 = string.Empty;
			_y1 = string.Empty;
			_y2 = string.Empty;

			_precisionX = 0;
			_width = string.Empty;

			_precisionY = 0;
			_height = string.Empty;

			_blockOffsetX = string.Empty;
			_blockOffsetY = string.Empty;

			_samplePointDelta = string.Empty;
			_samplePointDeltaExp = 0;

			_zoom = "0";
		}

		#endregion

		#region Public Properties

		public string? JobId
		{
			get => _jobId;
			set
			{
				if (value != _jobId)
				{
					_jobId = value;
					OnPropertyChanged();
				}
			}
		}

		public MapAreaInfo CurrentMapAreaInfo
		{
			get => _currentMapAreaInfo ?? new MapAreaInfo();
			set
			{
				if (value != _currentMapAreaInfo)
				{
					_currentMapAreaInfo = value;
					UpdateCoords(_currentMapAreaInfo);
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
					OnPropertyChanged();
				}
			}
		}

		public string X1
		{
			get => _x1;
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					var cItems = value.Split('\n');
					if (cItems.Length == 4)
					{
						SetMulti(cItems);
					}
					else
					{
						value = cItems[0];
						if (value != _x1)
						{
							_x1 = value;
							OnPropertyChanged();
						}
					}
				}
			}
		}

		public string X2
		{
			get => _x2;
			set
			{
				if (value != _x2)
				{
					_x2 = value;
					OnPropertyChanged();
				}
			}
		}

		public string Y1
		{
			get => _y1;
			set
			{
				if (value != _y1)
				{
					_y1 = value;
					OnPropertyChanged();
				}
			}
		}

		public string Y2
		{
			get => _y2;
			set
			{
				if (value != _y2)
				{
					_y2 = value;
					OnPropertyChanged();
				}
			}
		}

		public int CoordsExp
		{
			get => _coordsExp;
			set
			{
				if (value != _coordsExp)
				{
					_coordsExp = value;
					OnPropertyChanged();
				}
			}
		}

		public string Width
		{
			get => _width;
			set
			{
				if (value != _width)
				{
					_width = value;
					OnPropertyChanged();
				}
			}
		}

		public string Height
		{
			get => _height;
			set
			{
				if (value != _height)
				{
					_height = value;
					OnPropertyChanged();
				}
			}
		}

		public string Zoom
		{
			get => _zoom;
			set
			{
				_zoom = value;
				OnPropertyChanged();
			}
		}

		public int PrecisionX
		{
			get => _precisionX;
			set
			{
				if (value != _precisionX)
				{
					_precisionX = value;
					OnPropertyChanged();
				}
			}
		}

		public int PrecisionY
		{
			get => _precisionY;
			set
			{
				if (value != _precisionY)
				{
					_precisionY = value;
					OnPropertyChanged();
				}
			}
		}

		public string BlockOffsetX
		{
			get => _blockOffsetX;
			set
			{
				if (value != _blockOffsetX)
				{
					_blockOffsetX = value;
					OnPropertyChanged();
				}
			}
		}

		public string BlockOffsetY
		{
			get => _blockOffsetY;
			set
			{
				if (value != _blockOffsetY)
				{
					_blockOffsetY = value;
					OnPropertyChanged();
				}
			}
		}

		public string SamplePointDelta
		{
			get => _samplePointDelta;
			set
			{
				if (value != _samplePointDelta)
				{
					_samplePointDelta = value;
					OnPropertyChanged();
				}
			}
		}

		public int SamplePointDeltaExp
		{
			get => _samplePointDeltaExp;
			set
			{
				if (value != _samplePointDeltaExp)
				{
					_samplePointDeltaExp = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public void Preview(MapAreaInfo mapAreaInfo)
		{
			UpdateCoords(mapAreaInfo);
		}

		public string GetStringValues()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Job: {JobId}");
			sb.AppendLine(X1);
			sb.AppendLine(X2);
			sb.AppendLine(Y1);
			sb.AppendLine(Y2);

			return sb.ToString();
		}

		#endregion

		#region Private Methods

		private void UpdateCoords(MapAreaInfo? mapAreaInfo)
		{
			if (mapAreaInfo != null)
			{
				Coords = mapAreaInfo.Coords;
				var rValues = _coords.GetRValues();

				var startingX = rValues[0];
				var endingX = rValues[1];
				var startingY = rValues[2];
				var endingY = rValues[3];

				X1 = RValueHelper.ConvertToString(startingX);
				X2 = RValueHelper.ConvertToString(endingX);
				Y1 = RValueHelper.ConvertToString(startingY);
				Y2 = RValueHelper.ConvertToString(endingY);
				CoordsExp = Coords.Exponent;

				PrecisionX = RValueHelper.GetPrecision(startingX, endingX, out var diffX) + _numDigitsForDisplayExtent;
				Width = RValueHelper.ConvertToString(diffX, useSciNotationForLengthsGe: 6);

				PrecisionY = RValueHelper.GetPrecision(startingY, endingY, out var diffY) + _numDigitsForDisplayExtent;
				Height = RValueHelper.ConvertToString(diffY, useSciNotationForLengthsGe: 6);

				BlockOffsetX = mapAreaInfo.MapBlockOffset.X.ToString(CultureInfo.InvariantCulture);
				BlockOffsetY = mapAreaInfo.MapBlockOffset.Y.ToString(CultureInfo.InvariantCulture);

				SamplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator.ToString(CultureInfo.InvariantCulture);
				SamplePointDeltaExp = mapAreaInfo.Subdivision.SamplePointDelta.Exponent;

				Zoom = RValueHelper.GetFormattedResolution(mapAreaInfo.Coords.Width);
			}
			else
			{
				Coords = new RRectangle();
				X1 = string.Empty;
				X2 = string.Empty;
				Y1 = string.Empty;
				Y2 = string.Empty;
				CoordsExp = 0;

				PrecisionX = 0;
				Width = string.Empty;

				PrecisionY = 0;
				Height = string.Empty;

				BlockOffsetX = string.Empty;
				BlockOffsetY = string.Empty;

				SamplePointDelta = string.Empty;
				SamplePointDeltaExp = 0;

				_zoom = "0";
			}
		}

		private void SetMulti(string[] mVals)
		{
			var work = _coords;

			var x1Updated = false;
			var x2Updated = false;
			var y1Updated = false;
			var y2Updated = false;

			var nValue = mVals[0];
			if (nValue != _x1)
			{
				_x1 = nValue;
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
				if (nValue != _x2)
				{
					_x2 = nValue;
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
				if (nValue != _y1)
				{
					_y1 = nValue;
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
				if (nValue != _y2)
				{
					_y2 = nValue;
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
				OnPropertyChanged(nameof(X1));
			}

			if (x2Updated)
			{
				OnPropertyChanged(nameof(X2));
			}

			if (y1Updated)
			{
				OnPropertyChanged(nameof(Y1));
			}

			if (y2Updated)
			{
				OnPropertyChanged(nameof(Y2));
			}
		}

		#endregion
	}
}
