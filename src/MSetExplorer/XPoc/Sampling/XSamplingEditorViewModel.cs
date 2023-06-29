using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer.XPoc
{
	internal class XSamplingEditorViewModel : ViewModelBase
	{
		private const double TOLERANCE_FACTOR = 10;

		//private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly SubdivisonProvider _subdivisonProvider;

		private readonly SizeInt _blockSize;

		private SizeInt _screenSize;
		private SizeDbl _selectionSize;

		private SizeInt _screenSizeNrm;
		private SizeDbl _selectionSizeNrm;


		private RRectangle _coords;
		private int _exponent;

		#region Constructor

		public XSamplingEditorViewModel(SubdivisonProvider subdivisonProvider)
		{
			//_mapSectionAdapter = mapSectionAdapter;

			_subdivisonProvider = subdivisonProvider;

			MapAreaInfoViewModelCanS = new MapAreaInfoViewModel(_subdivisonProvider);
			MapAreaInfoViewModelSelS = new MapAreaInfoViewModel(_subdivisonProvider);

			MapAreaInfoViewModelCanN = new MapAreaInfoViewModel(_subdivisonProvider);
			MapAreaInfoViewModelSelN = new MapAreaInfoViewModel(_subdivisonProvider);

			_blockSize = RMapConstants.BLOCK_SIZE;

			_screenSize = new SizeInt(1024);
			_selectionSize = new SizeDbl(16);

			_coords = new RRectangle();

			_exponent = -1;
			Exponent = 0;
		}

		#endregion

		#region Public Properties

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

					UpdateScreenCoords();
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
					_exponent = Math.Abs(value) * -1;
					Coords = new RRectangle(0, 1, 0, 1, value);
				}
			}
		}
		
		#endregion

		#region Public Properties - MapAreaInfo Canvas

		public MapAreaInfoViewModel MapAreaInfoViewModelCanS { get; }
		public MapAreaInfoViewModel MapAreaInfoViewModelCanN { get; }

		public int ScreenWidth
		{
			get => _screenSize.Width;
			set => ScreenSize = new SizeInt(value, ScreenHeight);
		}

		public int ScreenHeight
		{
			get => _screenSize.Height;
			set => ScreenSize = new SizeInt(ScreenWidth, value);
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

					UpdateScreenCoords();
					OnPropertyChanged(nameof(ScreenWidth));
					OnPropertyChanged(nameof(ScreenHeight));
				}
			}
		}

		public int ScreenWidthNrm
		{
			get => _screenSizeNrm.Width;
			set => ScreenSizeNrm = new SizeInt(value, ScreenHeightNrm);
		}

		public int ScreenHeightNrm
		{
			get => _screenSizeNrm.Height;
			set => ScreenSizeNrm = new SizeInt(ScreenWidthNrm, value);
		}

		public SizeInt ScreenSizeNrm
		{
			get => _screenSizeNrm;
			set
			{
				if (value != _screenSizeNrm)
				{
					_screenSizeNrm = value;

					if (_screenSizeNrm.Width == 0 || _screenSizeNrm.Height == 0)
					{
						return;
					}

					UpdateScreenCoordsNrm();
					OnPropertyChanged(nameof(ScreenWidthNrm));
					OnPropertyChanged(nameof(ScreenHeightNrm));
				}
			}
		}

		#endregion

		#region Public Properties - MapAreaInfo Selection

		public MapAreaInfoViewModel MapAreaInfoViewModelSelS { get; }
		public MapAreaInfoViewModel MapAreaInfoViewModelSelN { get; }

		public double SelectionWidth
		{
			get => _selectionSize.Width;
			set => SelectionSize = new SizeDbl(value, SelectionHeight);
		}

		public double SelectionHeight
		{
			get => _selectionSize.Height;
			set => SelectionSize = new SizeDbl(SelectionWidth, value);
		}

		public SizeDbl SelectionSize
		{
			get => _selectionSize;
			set
			{
				if (value != _selectionSize)
				{
					_selectionSize = value;

					if (_selectionSize.Width == 0 || _selectionSize.Height == 0)
					{
						return;
					}

					UpdateSelectionCoords();
					OnPropertyChanged(nameof(SelectionWidth));
					OnPropertyChanged(nameof(SelectionHeight));
				}
			}
		}

		public double SelectionWidthNrm
		{
			get => _selectionSizeNrm.Width;
			set => SelectionSizeNrm = new SizeDbl(value, SelectionHeightNrm);
		}

		public double SelectionHeightNrm
		{
			get => _selectionSizeNrm.Height;
			set => SelectionSizeNrm = new SizeDbl(SelectionWidthNrm, value);
		}

		public SizeDbl SelectionSizeNrm
		{
			get => _selectionSizeNrm;
			set
			{
				if (value != _selectionSizeNrm)
				{
					_selectionSizeNrm = value;

					if (_selectionSizeNrm.Width == 0 || _selectionSizeNrm.Height == 0)
					{
						return;
					}

					UpdateSelectionCoordsNrm();
					OnPropertyChanged(nameof(SelectionWidthNrm));
					OnPropertyChanged(nameof(SelectionHeightNrm));
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

		#region Private Methods

		private void UpdateScreenCoords()
		{
			var screenMapAreaInfo = GetMapAreaInfo(Coords, new SizeDbl(ScreenSize), _blockSize);
			MapAreaInfoViewModelCanS.UpdateMapAreaInfo(screenMapAreaInfo, Coords);

			UpdateSelectionCoords();
		}

		// TODO: Update GetSubdivision to use a SizeDbl
		private void UpdateSelectionCoords()
		{
			var screenMapAreaInfo = MapAreaInfoViewModelCanS.MapAreaInfo;
			var mapPosition = screenMapAreaInfo.Coords.Position;
			var samplePointDelta = screenMapAreaInfo.Subdivision.SamplePointDelta;

			var selectedRectangle = new RectangleInt(new PointInt(), SelectionSize.Round());
			var selectedCoords = GetMapCoords(selectedRectangle, mapPosition, samplePointDelta);

			var screenSize = screenMapAreaInfo.CanvasSize;
			var selectedMapAreaInfo = GetMapAreaInfo(selectedCoords, screenSize, _blockSize);

			MapAreaInfoViewModelSelS.UpdateMapAreaInfo(selectedMapAreaInfo, selectedCoords);
		}

		private void UpdateScreenCoordsNrm()
		{
			//var screenMapAreaInfo = GetMapAreaInfo(Coords, ScreenSize, _blockSize);
			//MapAreaInfoViewModelCanS.UpdateMapAreaInfo(screenMapAreaInfo, Coords);

			UpdateSelectionCoordsNrm();
		}

		// TODO: Update GetSubdivision to use a SizeDbl
		private void UpdateSelectionCoordsNrm()
		{
			//var screenMapAreaInfo = MapAreaInfoViewModelCanS.MapAreaInfo;
			//var mapPosition = screenMapAreaInfo.Coords.Position;
			//var samplePointDelta = screenMapAreaInfo.Subdivision.SamplePointDelta;

			//var selectedRectangle = new RectangleInt(new PointInt(), SelectionSize.Round());
			//var selectedCoords = RMapHelper.GetMapCoords(selectedRectangle, mapPosition, samplePointDelta);

			//var screenSize = screenMapAreaInfo.CanvasSize;
			//var selectedMapAreaInfo = GetMapAreaInfo(selectedCoords, screenSize, _blockSize);

			//MapAreaInfoViewModelSelS.UpdateMapAreaInfo(selectedMapAreaInfo, selectedCoords);
		}


		private MapAreaInfo GetMapAreaInfo(RRectangle coords, SizeDbl canvasSize, SizeInt blockSize)
		{
			// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
			var displaySize = canvasSize.Round();

			// Using the size of the new map and the map coordinates, calculate the sample point size
			//var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, canvasSize, TOLERANCE_FACTOR);

			// Using the size of the new map and the map coordinates, calculate the sample point size
			var samplePointDelta = GetSamplePointDelta(coords, displaySize, TOLERANCE_FACTOR, out var wToHRatio);

			// The samplePointDelta may require the coordinates to be adjusted.
			var updatedCoords = AdjustCoordsWithNewSPD(coords, samplePointDelta, displaySize);

			Debug.WriteLine($"\nThe new coords are : {updatedCoords},\n old = {coords}. (While calculating SamplePointDelta.)\n");

			var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
			RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, coords, updatedCoords, newDCoords);


			// Determine the amount to translate from our coordinates to the subdivision coordinates.
			//var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, samplePointDelta, blockSize, out var canvasControlOffset);

			var mapBlockOffset = GetMapBlockOffset(updatedCoords.Position, samplePointDelta, blockSize, out var canvasControlOffset);
			//var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

			var subdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

			var binaryPrecision = GetBinaryPrecision(updatedCoords, samplePointDelta, out var decimalPrecision);

			var result = new MapAreaInfo(updatedCoords, new SizeDbl(displaySize), subdivision, binaryPrecision, localMapBlockOffset, canvasControlOffset, subdivision.Id);

			return result;
		}

		public static RRectangle AdjustCoordsWithNewSPD(RRectangle coords, RSize samplePointDelta, SizeInt canvasSize)
		{
			// The size of the new map is equal to the product of the number of samples by the new samplePointDelta.
			var adjMapSize = samplePointDelta.Scale(canvasSize);

			// Calculate the new map coordinates using the existing position and the new size.
			var newCoords = CombinePosAndSize(coords.Position, adjMapSize);

			return newCoords;
		}

		public static RRectangle CombinePosAndSize(RPoint pos, RSize size)
		{
			var nrmPos = RNormalizer.Normalize(pos, size, out var nrmSize);
			var result = new RRectangle(nrmPos, nrmSize);

			return result;
		}

		public int GetBinaryPrecision(RRectangle coords, RSize samplePointDelta, out int decimalPrecision)
		{
			var binaryPrecision = RValueHelper.GetBinaryPrecision(coords.Right, coords.Left, out decimalPrecision);
			binaryPrecision = Math.Max(binaryPrecision, Math.Abs(samplePointDelta.Exponent));

			return binaryPrecision;
		}

		#endregion

		#region Get MapBlockOffset Methods 

		public BigVector GetMapBlockOffset(RPoint mapPosition, RSize samplePointDelta, SizeInt blockSize, out VectorInt canvasControlOffset/*, out RPoint newPosition*/)
		{
			// Determine the number of blocks we must add to our screen coordinates to retrieve a block from the respository.
			// The screen origin = left, bottom. Map origin = left, bottom.

			if (mapPosition.IsZero())
			{
				canvasControlOffset = new VectorInt();
				return new BigVector();
			}

			var distance = new RVector(mapPosition);
			var offsetInSamplePoints = GetNumberOfSamplePoints(distance, samplePointDelta/*, out newPosition*/);

			var result = RMapHelper.GetOffsetAndRemainder(offsetInSamplePoints, blockSize, out canvasControlOffset);

			return result;
		}

		private BigVector GetNumberOfSamplePoints(RVector distance, RSize samplePointDelta/*, out RPoint newPosition*/)
		{
			var nrmDistance = RNormalizer.Normalize(distance, samplePointDelta, out var nrmSamplePointDelta);

			// # of whole sample points between the source and destination origins.
			var offsetInSamplePoints = nrmDistance.Divide(nrmSamplePointDelta);

			return offsetInSamplePoints;
		}

		#endregion

		#region Map Area Support

		// Convert the screen coordinates given by screenArea into map coordinates,
		// then move these map coordiates by the x and y distances specified in the current MapPosition.
		private RRectangle GetMapCoords(RectangleInt screenArea, RPoint mapPosition, RSize samplePointDelta)
		{
			// Convert to map coordinates.

			//var rArea = ScaleByRsize(screenArea, samplePointDelta);
			var rArea = new RRectangle(screenArea);
			rArea = rArea.Scale(samplePointDelta);

			// Translate the area by the current map position
			var nrmArea = RNormalizer.Normalize(rArea, mapPosition, out var nrmPos);
			var result = nrmArea.Translate(nrmPos);

			//Debug.WriteLine($"GetMapCoords is receiving area: {screenArea}.");
			//Debug.WriteLine($"Calc Map Coords: Trans: {result}, Pos: {nrmPos}, Area: {nrmArea}, area rat: {GetAspectRatio(nrmArea)}, result rat: {GetAspectRatio(result)}");

			return result;
		}

		private RSize GetSamplePointDelta(RRectangle coords, SizeInt canvasSize, double toleranceFactor, out double wToHRatio)
		{
			var spdH = BigIntegerHelper.Divide(coords.Width, canvasSize.Width, toleranceFactor);
			var spdV = BigIntegerHelper.Divide(coords.Height, canvasSize.Height, toleranceFactor);

			var nH = RNormalizer.Normalize(spdH, spdV, out var nV);

			// Take the smallest value
			var result = new RSize(RValue.Min(nH, nV));

			wToHRatio = nH.DivideLimitedPrecision(nV);

			return result;
		}

		//public static int CalculatePitch(SizeInt displaySize, int pitchTarget)
		//{
		//	// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
		//	int result;

		//	var width = displaySize.Width;
		//	var height = displaySize.Height;

		//	if (double.IsNaN(width) || double.IsNaN(height) || width == 0 || height == 0)
		//	{
		//		return pitchTarget;
		//	}

		//	if (width >= height)
		//	{
		//		result = (int)Math.Round(width / Math.Pow(2, Math.Round(Math.Log2(width / pitchTarget))));
		//	}
		//	else
		//	{
		//		result = (int)Math.Round(height / Math.Pow(2, Math.Round(Math.Log2(height / pitchTarget))));
		//	}

		//	if (result < 0)
		//	{
		//		Debug.WriteLine($"WARNING: Calculating Pitch using Display Size: {displaySize} and Pitch Target: {pitchTarget}, produced {result}.");
		//		result = pitchTarget;
		//	}

		//	return result;
		//}

		#endregion

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
	}
}
