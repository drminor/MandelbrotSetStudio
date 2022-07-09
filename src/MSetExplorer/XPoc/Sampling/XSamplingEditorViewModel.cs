using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;


namespace MSetExplorer.XPoc
{
	internal class XSamplingEditorViewModel : ViewModelBase
	{
		//private const int _numDigitsForDisplayExtent = 4;

		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SizeInt _blockSize;

		private int _canvasWidth;
		private int _canvasHeight;

		private SizeInt _mapSize;

		private RRectangle _coords;
		private RRectangle _newCoords;

		//private int? _precision;

		private long _samplePointDelta;
		private int _samplePointDeltaExp;

		private int _zoom;

		private long _newSamplePointDelta;
		private int _newSamplePointDeltaExp;

		private int _newZoom;


		#region Constructor

		public XSamplingEditorViewModel(IMapSectionAdapter mapSectionAdapter)
		{
			_blockSize = RMapConstants.BLOCK_SIZE;
			_mapSectionAdapter = mapSectionAdapter;

			_canvasWidth = 1024;
			_canvasHeight = 1024;
			_mapSize = new SizeInt(1024);

			_coords = new RRectangle();
			_newCoords = new RRectangle();

			Zoom = 1;
		}

		#endregion

		#region Public Properties

		public int CanvasWidth
		{
			get => _canvasWidth;
			set
			{
				if (value != _canvasWidth)
				{
					_canvasWidth = value;
					MapSize = new SizeInt(CanvasWidth, CanvasHeight);

					OnPropertyChanged();
				}
			}
		}

		public int CanvasHeight
		{
			get => _canvasHeight;
			set
			{
				if (value != _canvasHeight)
				{
					_canvasHeight = value;
					MapSize = new SizeInt(CanvasWidth, CanvasHeight);

					OnPropertyChanged();
				}
			}
		}

		public SizeInt MapSize
		{
			get => _mapSize;
			set
			{
				if (value != _mapSize)
				{
					_mapSize = value;

					if (_mapSize.Width == 0 || _mapSize.Height == 0)
					{
						return;
					}

					var mapAreaInfo = GetMapAreaInfo(Coords, MapSize, _blockSize);

					NewCoords = mapAreaInfo.Coords;
					NewSamplePointDelta = (long)mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator;
					NewSamplePointDeltaExp = mapAreaInfo.Subdivision.SamplePointDelta.Exponent;

					NewZoom = mapAreaInfo.Coords.Exponent * -1;

					OnPropertyChanged();
					OnPropertyChanged(nameof(NewWidth));
					OnPropertyChanged(nameof(NewHeight));
					OnPropertyChanged(nameof(NewSamplePointDelta));
					OnPropertyChanged(nameof(NewSamplePointDeltaExp));
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

					var mapAreaInfo = GetMapAreaInfo(Coords, MapSize, _blockSize);

					NewCoords = mapAreaInfo.Coords;
					NewSamplePointDelta = (long) mapAreaInfo.Subdivision.SamplePointDelta.WidthNumerator;
					NewSamplePointDeltaExp = mapAreaInfo.Subdivision.SamplePointDelta.Exponent;

					NewZoom = mapAreaInfo.Coords.Exponent * -1;

					OnPropertyChanged();
					OnPropertyChanged(nameof(NewWidth));
					OnPropertyChanged(nameof(NewHeight));
					OnPropertyChanged(nameof(NewSamplePointDelta));
					OnPropertyChanged(nameof(NewSamplePointDeltaExp));
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

					OnPropertyChanged();
				}
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

		public int Zoom
		{
			get => _zoom;
			set
			{
				if (value != _zoom)
				{
					_zoom = value;

					Coords = new RRectangle(0, 1, 0, 1, -1 * value);
					OnPropertyChanged();
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

		public int NewZoom
		{
			get => _newZoom;
			set { _newZoom = value; OnPropertyChanged(); }
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

		public long NewWidth => (long)_newCoords.Width.Value;
		public long NewHeight => (long)_newCoords.Height.Value;

		#endregion

		#region Private Methods

		private MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeInt mapSize, SizeInt blockSize)
		{
			// Using the size of the new map and the map coordinates, calculate the sample point size
			var updatedCoords = coords.Clone();
			var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, mapSize);


			//Debug.WriteLine($"\nThe new coords are : {coordsWork},\n old = {mSetInfo.Coords}. (While calculating SamplePointDelta.)\n");

			//var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			//RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, mSetInfo.Coords, coordsWork, newDCoords);

			// Get a subdivision record from the database.
			var subdivision = GetSubdivision(samplePointDelta, blockSize);

			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, subdivision, out var canvasControlOffset);

			var result = new MapAreaInfo(updatedCoords, mapSize, subdivision, mapBlockOffset, canvasControlOffset);

			return result;
		}

		// Find an existing subdivision record that the same SamplePointDelta
		private Subdivision GetSubdivision(RSize samplePointDelta, SizeInt blockSize)
		{
			if (!_mapSectionAdapter.TryGetSubdivision(samplePointDelta, blockSize, out var result))
			{
				result = new Subdivision(samplePointDelta, blockSize);
			}

			return result;
		}

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
