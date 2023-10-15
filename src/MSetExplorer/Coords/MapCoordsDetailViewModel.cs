using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Globalization;

namespace MSetExplorer
{
	public class MapCoordsDetailViewModel : ViewModelBase
	{
		private const int _numDigitsForDisplayExtent = 4;

		private RRectangle _coords;

		private string _headerName;
		private string _x1;
		private string _x2;
		private string _y1;
		private string _y2;


		private RValue _xR1;
		private RValue _xR2;
		private RValue _yR1;
		private RValue _yR2;

		private string _width;
		private string _height;

		private int _precisionX;
		private int _precisionY;

		private string _blockOffsetX;
		private string _blockOffsetY;

		private string _samplePointDelta;
		private int _samplePointDeltaExp;

		private string _zoom;

		public bool HaveMapAreaInfo { get; init; }

		#region Constructor

		public MapCoordsDetailViewModel(MapPositionSizeAndDelta mapAreaInfo) : this(mapAreaInfo.Coords)
		{
			_blockOffsetX = mapAreaInfo.MapBlockOffset.X.ToString(CultureInfo.InvariantCulture);
			_blockOffsetY = mapAreaInfo.MapBlockOffset.Y.ToString(CultureInfo.InvariantCulture);

			_samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator.ToString(CultureInfo.InvariantCulture);
			_samplePointDeltaExp = mapAreaInfo.Subdivision.SamplePointDelta.Exponent;

			_zoom = RValueHelper.GetFormattedResolution(mapAreaInfo.Coords.Width);

			HaveMapAreaInfo = true;
		}

		public MapCoordsDetailViewModel(RRectangle coords)
		{
			_coords = coords;
			_headerName = "Coordinates";
			var rValues = coords.GetRValues();

			_xR1 = rValues[0];
			_xR2 = rValues[1];
			_yR1 = rValues[2];
			_yR2 = rValues[3];

			_x1 = RValueHelper.ConvertToString(_xR1);
			_x2 = RValueHelper.ConvertToString(_xR2);
			_y1 = RValueHelper.ConvertToString(_yR1);
			_y2 = RValueHelper.ConvertToString(_yR2);

			_precisionX = RValueHelper.GetPrecision(StartingX, EndingX, out var diffX);
			_precisionX += _numDigitsForDisplayExtent;
			_width = RValueHelper.ConvertToString(diffX, useSciNotationForLengthsGe: 6);

			_precisionY = RValueHelper.GetPrecision(StartingY, EndingY, out var diffY);
			_precisionY += _numDigitsForDisplayExtent;
			_height = RValueHelper.ConvertToString(diffY, useSciNotationForLengthsGe: 6);

			_blockOffsetX = string.Empty;
			_blockOffsetY = string.Empty;

			_samplePointDelta = string.Empty;
			_samplePointDeltaExp = 0;
			HaveMapAreaInfo = false;

			_zoom = RValueHelper.GetFormattedResolution(coords.Width);
		}

		#endregion

		#region Event Handlers

		#endregion

		#region Public Properties

		public string HeaderName
		{
			get => _headerName;
			set
			{
				if (value != _headerName)
				{
					_headerName = value;
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

					StartingX = _coords.Left;
					EndingX = _coords.Right;
					StartingY = _coords.Bottom;
					EndingY = _coords.Top;
				}
			}
		}

		public RValue StartingX
		{
			get => _xR1;
			set
			{
				if (value != _xR1)
				{
					_xR1 = value;
					_x1 = RValueHelper.ConvertToString(value);

					OnPropertyChanged();
					OnPropertyChanged(nameof(X1));
				}
			}
		}

		public RValue EndingX
		{
			get => _xR2;
			set
			{
				if (value != _xR2)
				{
					_xR2 = value;
					_x2 = RValueHelper.ConvertToString(value);

					OnPropertyChanged();
					OnPropertyChanged(nameof(X2));
				}
			}
		}

		public RValue StartingY
		{
			get => _yR1;
			set
			{
				if (value != _yR1)
				{
					_yR1 = value;
					_y1 = RValueHelper.ConvertToString(value);

					OnPropertyChanged();
					OnPropertyChanged(nameof(Y1));
				}
			}
		}

		public RValue EndingY
		{
			get => _yR2;
			set
			{
				if (value != _yR2)
				{
					_yR2 = value;
					_y2 = RValueHelper.ConvertToString(value);

					OnPropertyChanged();
					OnPropertyChanged(nameof(Y2));
				}
			}
		}

		public string X1
		{
			get => _x1;
			set
			{
				if (value != _x1)
				{
					_x1 = value;
					OnPropertyChanged();
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

	}
}
