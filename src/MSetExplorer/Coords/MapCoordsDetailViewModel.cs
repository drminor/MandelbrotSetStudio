using MSetRepo;
using MSS.Common;
using MSS.Types;

namespace MSetExplorer
{
	public class MapCoordsDetailViewModel : ViewModelBase
	{
		private const int _numDigitsForDisplayExtent = 4;

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

		private long _zoom;

		#region Constructor

		public MapCoordsDetailViewModel(RValue[] values)
		{
			_xR1 = values[0];
			_xR2 = values[1];
			_yR1 = values[2];
			_yR2 = values[3];

			_x1 = RValueHelper.ConvertToString(_xR1);
			_x2 = RValueHelper.ConvertToString(_xR2);
			_y1 = RValueHelper.ConvertToString(_yR1);
			_y2 = RValueHelper.ConvertToString(_yR2);

			var coords = GetCoords();

			_width = RValueHelper.ConvertToString(coords.Width);
			_height = RValueHelper.ConvertToString(coords.Height);
		}

		#endregion

		#region Event Handlers

		#endregion

		#region Public Properties

		public RValue StartingX
		{
			get => _xR1;
			set
			{
				if (value != _xR1)
				{
					_xR1 = value;
					OnPropertyChanged();
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
					OnPropertyChanged();
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
					OnPropertyChanged();
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
					OnPropertyChanged();
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

		public long Zoom
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

		#endregion

		#region Public Methods

		public RRectangle GetCoords()
		{
			var result = new RRectangle(StartingX.Value, EndingX.Value, StartingY.Value, EndingY.Value, StartingX.Exponent);
			return result;
		}

		#endregion

		#region Private Methods


		#endregion
	}
}
