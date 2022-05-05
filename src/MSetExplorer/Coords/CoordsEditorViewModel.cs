using MSS.Common;
using MSS.Types;

namespace MSetExplorer
{
	public class CoordsEditorViewModel : ViewModelBase
	{
		private const int _numDigitsForDisplayExtent = 4;

		private string _x1;
		private string _x2;
		private string _y1;
		private string _y2;

		private string _width;

		private string _height;

		private int _precisionX;
		private int _precisionY;

		private long _zoom;

		//private RRectangle _coords;
		//private bool _coordsAreDirty;

		#region Constructor

		public CoordsEditorViewModel(RRectangle coords)
		{
			StartingX = new SingleCoordEditorViewModel(coords.Left);
			EndingX = new SingleCoordEditorViewModel(coords.Right);
			StartingY = new SingleCoordEditorViewModel(coords.Bottom);
			EndingY = new SingleCoordEditorViewModel(coords.Top);

			_x1 = string.Empty;
			_x2 = string.Empty;
			_y1 = string.Empty;
			_y2 = string.Empty;
			_width = string.Empty;
			_height = string.Empty;

			GetCoords();

			AddEventHandlers();
		}

		public CoordsEditorViewModel(string x1, string x2, string y1, string y2)
		{
			StartingX = new SingleCoordEditorViewModel(x1);
			EndingX = new SingleCoordEditorViewModel(x2);
			StartingY = new SingleCoordEditorViewModel(y1);
			EndingY = new SingleCoordEditorViewModel(y2);

			_x1 = string.Empty;
			_x2 = string.Empty;
			_y1 = string.Empty;
			_y2 = string.Empty;
			_width = string.Empty;
			_height = string.Empty;

			GetCoords();
			
			AddEventHandlers();
		}

		private void AddEventHandlers()
		{
			StartingX.PropertyChanged += StartingX_PropertyChanged;
			EndingX.PropertyChanged += EndingX_PropertyChanged;
			StartingY.PropertyChanged += StartingY_PropertyChanged;
			EndingY.PropertyChanged += EndingY_PropertyChanged;
		}

		#endregion

		#region Event Handlers

		private void StartingX_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void EndingX_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void StartingY_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void EndingY_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		#endregion

		#region Public Properties

		public SingleCoordEditorViewModel StartingX { get; init; }
		public SingleCoordEditorViewModel EndingX { get; init; }
		public SingleCoordEditorViewModel StartingY { get; init; }
		public SingleCoordEditorViewModel EndingY { get; init; }

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

		//public RRectangle Coords
		//{
		//	get => _coords;
		//	set
		//	{
		//		if (value != _coords)
		//		{
		//			_coords = value;
		//			//StartingX = RValueHelper.ConvertToString(_coords.Left);
		//			//EndingX = RValueHelper.ConvertToString(_coords.Right);
		//			//StartingY = RValueHelper.ConvertToString(_coords.Bottom);
		//			//EndingY = RValueHelper.ConvertToString(_coords.Top);

		//			CoordsAreDirty = true;

		//			Zoom = RValueHelper.GetResolution(_coords.Width, out var precision);
		//			Precision = precision;

		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public bool CoordsAreDirty
		//{
		//	get => _coordsAreDirty;

		//	private set
		//	{
		//		if (value != _coordsAreDirty)
		//		{
		//			_coordsAreDirty = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		#endregion

		#region Public Methods

		public RRectangle GetCoords()
		{
			_precisionX = RValueHelper.GetPrecision(StartingX.RValue, EndingX.RValue, out var diffX);
			_width = RValueHelper.ConvertToString(diffX, useSciNotationForLengthsGe: 6);

			_precisionX += _numDigitsForDisplayExtent;
			var newX1Sme = StartingX.SignManExp.ReducePrecisionTo(_precisionX);
			var newX2Sme = EndingX.SignManExp.ReducePrecisionTo(_precisionX);
			_x1 = newX1Sme.GetValueAsString();
			_x2 = newX2Sme.GetValueAsString();

			_precisionY = RValueHelper.GetPrecision(StartingX.RValue, EndingX.RValue, out var diffY);
			_height = RValueHelper.ConvertToString(diffY, useSciNotationForLengthsGe: 6);

			_precisionY += _numDigitsForDisplayExtent;
			var newY1Sme = StartingY.SignManExp.ReducePrecisionTo(_precisionY);
			var newY2Sme = EndingY.SignManExp.ReducePrecisionTo(_precisionY);
			_y1 = newY1Sme.GetValueAsString();
			_y2 = newY2Sme.GetValueAsString();

			var result = RValueHelper.BuildRRectangleFromStrings(new string[] { _x1, _x2, _y1, _y2 });

			OnPropertyChanged(nameof(X1));
			OnPropertyChanged(nameof(X2));
			OnPropertyChanged(nameof(Y1));
			OnPropertyChanged(nameof(Y2));
			OnPropertyChanged(nameof(Width));
			OnPropertyChanged(nameof(Height));
			OnPropertyChanged(nameof(PrecisionX));
			OnPropertyChanged(nameof(PrecisionY));

			return result;
		}

		#endregion

		#region Private Methods

		#endregion
	}
}
