using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;


namespace MSetExplorer.XPoc
{
	internal class XSamplingEditorViewModel : ViewModelBase
	{
		//private const int _numDigitsForDisplayExtent = 4;

		//private readonly SizeInt _displaySize;
		//private readonly SizeInt _blockSize;

		private RRectangle _coords;

		private int? _precision;
		private long _zoom;
		private bool _coordsAreDirty;

		#region Constructor

		public XSamplingEditorViewModel(long left, long right, long bottom, long top, int exponent, int? precision )
		{
			_coords = new RRectangle(left, right, bottom, top, exponent);
			_precision = precision;
		}

		public XSamplingEditorViewModel(RRectangle coords)
		{
			_coords = coords;
			_precision = null;
		}

		#endregion


		public void DoIt()
		{

		}

		#region Public Properties

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					CoordsAreDirty = true;

					Zoom = RValueHelper.GetResolution(_coords.Width);

					OnPropertyChanged();
				}
			}
		}

		public long Zoom
		{
			get => _zoom;
			set
			{
				if (value != _zoom)
				{
					_zoom = value;
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

		public long Left
		{
			get => (long)_coords.Left.Value;
			set
			{
				if (value != Left)
				{
					_coords.Values[0] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Width));
					OnPropertyChanged(nameof(Coords));
				}
			}
		}

		public long Right
		{
			get => (long)_coords.Right.Value;
			set
			{
				if (value != Right)
				{
					_coords.Values[1] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Width));
					OnPropertyChanged(nameof(Coords));
				}
			}
		}


		public long Bottom
		{
			get => (long)_coords.Bottom.Value;
			set
			{
				if (value != Bottom)
				{
					_coords.Values[2] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Height));
					OnPropertyChanged(nameof(Coords));
				}
			}
		}


		public long Top
		{
			get => (long)_coords.Top.Value;
			set
			{
				if (value != Top)
				{
					_coords.Values[3] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(Height));
					OnPropertyChanged(nameof(Coords));
				}
			}
		}


		public long Width => (long)_coords.Width.Value;
		public long Height => (long)_coords.Height.Value;

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		#endregion
	}
}
