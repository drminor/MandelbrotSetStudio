using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;


namespace MSetExplorer.XPoc
{
	internal class XSamplingEditorViewModel : ViewModelBase
	{
		//private const int _numDigitsForDisplayExtent = 4;

		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SizeInt _blockSize;

		private SizeInt _screenSize;

		private RRectangle _coords;

		private int _exponent;

		private RRectangle _newCoords;

		private long _newSamplePointDelta;
		private int _newSamplePointDeltaExp;

		//private int _newZoom;


		#region Constructor

		public XSamplingEditorViewModel(IMapSectionAdapter mapSectionAdapter)
		{
			_blockSize = RMapConstants.BLOCK_SIZE;
			_mapSectionAdapter = mapSectionAdapter;

			_screenSize = new SizeInt(1024);

			_coords = new RRectangle();
			_newCoords = new RRectangle();

			_exponent = -1;
			Exponent = 0;
		}

		#endregion

		#region Public Properties

		public int ScreenWidth
		{
			get => _screenSize.Width;
			set
			{
				ScreenSize = new SizeInt(value);
			}
		}

		public int ScreenHeight
		{
			get => _screenSize.Height;
			set
			{
				ScreenSize = new SizeInt(value);
			}
		}

		public SizeInt ScreenSize
		{
			get => _screenSize;
			set
			{
				if (value != _screenSize)
				{
					_screenSize = value;

					if (_screenSize.Width == 0 || _screenSize.Height == 0)
					{
						return;
					}

					UpdateNewCoords();
					OnPropertyChanged(nameof(ScreenWidth));
					OnPropertyChanged(nameof(ScreenHeight));
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

					OnPropertyChanged(nameof(Left));
					OnPropertyChanged(nameof(Bottom));
					OnPropertyChanged(nameof(Width));
					OnPropertyChanged(nameof(Height));
					OnPropertyChanged(nameof(Exponent));

					UpdateNewCoords();
				}
			}
		}

		public int Exponent
		{
			get => _exponent;
			set
			{
				if (value != _exponent)
				{
					_exponent = value;

					Coords = new RRectangle(0, 1, 0, 1, -1 * value);
				}
			}
		}

		public RRectangle NewCoords
		{
			get => _newCoords;
			set
			{
				if (value != _newCoords)
				{
					_newCoords = value;

					OnPropertyChanged(nameof(NewLeft));
					OnPropertyChanged(nameof(NewBottom));
					OnPropertyChanged(nameof(NewWidth));
					OnPropertyChanged(nameof(NewHeight));
					OnPropertyChanged(nameof(NewExponent));
				}
			}
		}

		public long NewSamplePointDelta
		{
			get => _newSamplePointDelta;
			set { _newSamplePointDelta = value; OnPropertyChanged(); }
		}

		public int NewSamplePointDeltaExp
		{
			get => _newSamplePointDeltaExp;
			set { _newSamplePointDeltaExp = value; OnPropertyChanged(); }
		}

		private double _mapWidthDiff;
		public double MapWidthDiff
		{
			get => _mapWidthDiff;
			set
			{
				if (value != _mapWidthDiff)
				{
					_mapWidthDiff = value;
					OnPropertyChanged();
				}
			}
		}

		private double _screenWidthDiff;
		public double ScreenWidthDiff
		{
			get => _screenWidthDiff;
			set
			{
				if (value != _screenWidthDiff)
				{
					_screenWidthDiff = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region The Coords Component Properties

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
				}
			}
		}

		public int Precision
		{
			get => _coords.Precision;
			set
			{
				if (value != _coords.Precision)
				{
					_coords.Precision = value;
					OnPropertyChanged();
				}
			}
		}

		public long Width => (long)_coords.Width.Value;
		public long Height => (long)_coords.Height.Value;

		#endregion

		#region The New Coords Component Properties

		public long NewLeft
		{
			get => (long)_newCoords.Left.Value;
			set
			{
				if (value != NewLeft)
				{
					_newCoords.Values[0] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(NewWidth));
					OnPropertyChanged(nameof(NewCoords));
				}
			}
		}

		public long NewRight
		{
			get => (long)_newCoords.Right.Value;
			set
			{
				if (value != NewRight)
				{
					_newCoords.Values[1] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(NewWidth));
					OnPropertyChanged(nameof(NewCoords));
				}
			}
		}

		public long NewBottom
		{
			get => (long)_newCoords.Bottom.Value;
			set
			{
				if (value != NewBottom)
				{
					_newCoords.Values[2] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(NewHeight));
					OnPropertyChanged(nameof(NewCoords));
				}
			}
		}

		public long NewTop
		{
			get => (long)_newCoords.Top.Value;
			set
			{
				if (value != NewTop)
				{
					_newCoords.Values[3] = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(NewHeight));
					OnPropertyChanged(nameof(NewCoords));
				}
			}
		}

		public int NewExponent
		{
			get => _newCoords.Exponent;
			set { }
		}

		public int NewPrecision
		{
			get => _newCoords.Precision;
			set
			{
				if (value != _newCoords.Precision)
				{
					_newCoords.Precision = value;
					OnPropertyChanged();
				}
			}
		}

		public long NewWidth => (long)_newCoords.Width.Value;
		public long NewHeight => (long)_newCoords.Height.Value;

		#endregion

		#region Private Methods

		private void UpdateNewCoords()
		{
			var mapAreaInfo = GetMapAreaInfo(Coords, ScreenSize, _blockSize);

			NewCoords = mapAreaInfo.Coords;

			NewSamplePointDelta = (long)mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator;
			NewSamplePointDeltaExp = mapAreaInfo.Subdivision.SamplePointDelta.Exponent;

			OnPropertyChanged(nameof(NewSamplePointDelta));
			OnPropertyChanged(nameof(NewSamplePointDeltaExp));

			if (BigIntegerHelper.TryConvertToDouble(mapAreaInfo.Subdivision.SamplePointDelta.Width, out var sampleWidth))
			{
				if (BigIntegerHelper.TryConvertToDouble(Coords.Width, out var mapWidth))
				{
					MapWidthDiff = GetCoordDiff(sampleWidth, ScreenSize.Width, mapWidth);
					ScreenWidthDiff = MapWidthDiff * ScreenSize.Width;
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
			var rWidth = screenWidth * sampleWidth;
			var result = Math.Abs(mapWidth - rWidth);

			return result;
		}

		private MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt mapSize, SizeInt blockSize)
		{
			// Using the size of the new map and the map coordinates, calculate the sample point size
			var updatedCoords = coords.Clone();
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, mapSize);

			//Debug.WriteLine($"\nThe new coords are : {coordsWork},\n old = {mSetInfo.Coords}. (While calculating SamplePointDelta.)\n");

			//var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			//RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, mSetInfo.Coords, coordsWork, newDCoords);

			// Get a subdivision record from the database.
			//var subdivision = GetSubdivision(samplePointDelta, blockSize);
			var subdivision = new Subdivision(samplePointDelta, blockSize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, subdivision, out var canvasControlOffset);

			var result = new MapAreaInfo(updatedCoords, mapSize, subdivision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		//private Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize)
		//{
		//	if (!_mapSectionAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var result))
		//	{
		//		result = new Subdivision(samplePointDelta, blockSize);
		//	}

		//	return result;
		//}

		/*
		
		public XSamplingEditorViewModel GetXSamplingEditorViewModel()
		{
			var spdNumerator = new long[] { 0, 1545950080965521 };
			var spdExponent = -89;

			var samplePointDelta = new RSize(
				BigIntegerHelper.FromLongs(spdNumerator),
				BigIntegerHelper.FromLongs(spdNumerator),
				spdExponent
				);

			var bPosXHi = 0;
			var bPosXLo = 1492162821;

			var bPosYHi = 0;
			var bPosYLo = 1675271270;

			var mapBlockPosition = new BigVector(
				BigIntegerHelper.FromLongs(new long[] { bPosXHi, bPosXLo }),
				BigIntegerHelper.FromLongs(new long[] { bPosYHi, bPosYLo })
				);

			Debug.WriteLine($"SPD: {samplePointDelta}, MapBlockPosition: {mapBlockPosition}.");

			var rect = new RRectangle(new RPoint(), new RPoint());

			var result = new XSamplingEditorViewModel(rect);

			return result;
		}



			SubdivisionId : 625a6b2db475dbd8fc268fcb
			BlockPosXHi	: 0
			BlockPosXLo : -1492162821
			BlockPosYHi : 0
			BlockPosYLo : 1675271270
		*/

		//private MapAreaInfo? GetUpdatedMapAreaInfo(MapAreaInfo mapAreaInfo, RectangleInt screenArea, SizeInt mapSize)
		//{
		//	if (screenArea == new RectangleInt())
		//	{
		//		return mapAreaInfo;
		//	}
		//	else
		//	{
		//		var mapPosition = mapAreaInfo.Coords.Position;
		//		var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
		//		var blockSize = mapAreaInfo.Subdivision.BlockSize;

		//		var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);
		//		var updatedMapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, mapSize, blockSize);

		//		return updatedMapAreaInfo;
		//	}
		//}

		#endregion
	}
}
