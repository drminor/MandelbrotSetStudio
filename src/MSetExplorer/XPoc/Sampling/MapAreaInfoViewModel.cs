using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer.XPoc
{
	internal class MapAreaInfoViewModel : ViewModelBase
	{
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private string _sectionTitle;
		private MapAreaInfo _mapAreaInfo;
		private RRectangle _coords;

		private long _samplePointDelta;
		private int _samplePointDeltaExp;
		private bool _samplePointDeltaOnFile;

		private double _mapWidthDiff;
		private double _screenWidthDiff;

		#region Constructor

		public MapAreaInfoViewModel(IMapSectionAdapter mapSectionAdapter)
		{
			_mapSectionAdapter = mapSectionAdapter;
			_sectionTitle = "Section Title";
			_mapAreaInfo = new MapAreaInfo();
			_coords = new RRectangle();
		}

		#endregion

		#region Public Properties

		public string SectionTitle
		{
			get => _sectionTitle;
			set { _sectionTitle = value; OnPropertyChanged(); }
		}

		public MapAreaInfo MapAreaInfo
		{
			get => _mapAreaInfo;
			set
			{
				_mapAreaInfo = value;
				Coords = _mapAreaInfo.Coords;

				SamplePointDelta = (long)_mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator;
				SamplePointDeltaExp = _mapAreaInfo.Subdivision.SamplePointDelta.Exponent;
				SamplePointDeltaOnFile = IsSubdivisionOnFile(_mapAreaInfo.Subdivision);
			}
		}

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				_coords = value;
				OnPropertyChanged(nameof(Left));
				OnPropertyChanged(nameof(Bottom));
				OnPropertyChanged(nameof(Width));
				OnPropertyChanged(nameof(Height));
				OnPropertyChanged(nameof(Exponent));
				OnPropertyChanged(nameof(Precision));
			}
		}

		public long SamplePointDelta
		{
			get => _samplePointDelta;
			set { _samplePointDelta = value; OnPropertyChanged(); }
		}

		public int SamplePointDeltaExp
		{
			get => _samplePointDeltaExp;
			set { _samplePointDeltaExp = value; OnPropertyChanged(); }
		}

		public bool SamplePointDeltaOnFile
		{
			get => _samplePointDeltaOnFile;
			set { _samplePointDeltaOnFile = value; OnPropertyChanged(); }
		}

		public double MapWidthDiff
		{
			get => _mapWidthDiff;
			set { _mapWidthDiff = value; OnPropertyChanged(); }
		}

		public double ScreenWidthDiff
		{
			get => _screenWidthDiff;
			set { _screenWidthDiff = value; OnPropertyChanged(); }
		}

		#endregion

		#region The Coords Component Properties

		public long Left
		{
			get => (long)_coords.Left.Value;
			set { }
		}

		public long Right
		{
			get => (long)_coords.Right.Value;
			set { }
		}

		public long Bottom
		{
			get => (long)_coords.Bottom.Value;
			set { }
		}

		public long Top
		{
			get => (long)_coords.Top.Value;
			set { }
		}

		public int Exponent
		{
			get => _coords.Exponent;
			set { }
		}

		public int Precision
		{
			get => _coords.Precision;
			set { }
		}

		public long Width
		{
			get => (long)_coords.Width.Value;
			set { }
		}

		public long Height
		{
			get => (long)_coords.Height.Value;
			set { }
		}

		#endregion

		#region Public Methods

		public void UpdateMapAreaInfo(MapAreaInfo mapAreaInfo, RRectangle targetCoords)
		{
			MapAreaInfo = mapAreaInfo;
			UpdateDiffs(mapAreaInfo, targetCoords);
		}

		#endregion

		#region Private Methods

		private void UpdateDiffs(MapAreaInfo mapAreaInfo, RRectangle targetCoords)
		{
			if (BigIntegerHelper.TryConvertToDouble(mapAreaInfo.Subdivision.SamplePointDelta.Width, out var sampleWidth))
			{
				if (BigIntegerHelper.TryConvertToDouble(targetCoords.Width, out var mapWidth))
				{
					var canvasSize = mapAreaInfo.CanvasSize;
					MapWidthDiff = GetCoordDiff(sampleWidth, canvasSize.Width, mapWidth);
					ScreenWidthDiff = MapWidthDiff * canvasSize.Width;
				}
				else
				{
					MapWidthDiff = double.NaN;
					ScreenWidthDiff = double.NaN;
				}
			}
			else
			{
				MapWidthDiff = double.NaN;
				ScreenWidthDiff = double.NaN;
			}
		}

		private double GetCoordDiff(double sampleWidth, int screenWidth, double mapWidth)
		{
			var rWidth = sampleWidth * screenWidth;
			var result = mapWidth - rWidth;

			return result;
		}

		private bool IsSubdivisionOnFile(Subdivision subdivision)
		{
			var result = _mapSectionAdapter.TryGetSubdivision(subdivision.SamplePointDelta, subdivision.BaseMapPosition, out var _);

			return result;
		}

		#endregion
	}
}
