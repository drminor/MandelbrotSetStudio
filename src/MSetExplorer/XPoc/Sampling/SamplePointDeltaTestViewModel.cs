using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Numerics;

namespace MSetExplorer.XPoc
{
	internal class SamplePointDeltaTestViewModel : ViewModelBase
	{
		#region Private Fields

		private const double TOLERANCE_FACTOR = 10;

		private readonly SizeInt _blockSize;

		private SizeInt _screenSize;
		private SizeInt _screenSizeNrm;

		private double _selectionWidthPercentage;
		private double _zoomFactor;
		private double _zoomFactorNrm;

		private int _selectionWidthPerNumerator;
		private int _selectionWidthPerDenominator;


		private long _currentSpdNumerator;
		private int _currentSpdExponent;

		private long _resultantSpdNumerator;
		private int _resultantSpdExponent;


		private SizeDbl _selectionSize;
		private SizeDbl _selectionSizeNrm;

		//private RRectangle _coords;
		//private int _exponent;

		#endregion

		#region Constructor

		public SamplePointDeltaTestViewModel()
		{
			_blockSize = RMapConstants.BLOCK_SIZE;

			_screenSize = new SizeInt(1024);
			_selectionSize = new SizeDbl(16);

			_selectionWidthPerNumerator = 16;
			_selectionWidthPerDenominator = 128;

			_selectionWidthPercentage = 12.5;

			_zoomFactor = 16;
			_zoomFactorNrm = 16;

			_currentSpdNumerator = 1;
			_currentSpdExponent = -8;

			_resultantSpdNumerator = 1;
			_resultantSpdExponent = -12;

			//_coords = new RRectangle();

			//_exponent = -1;
			//Exponent = 0;
		}

		#endregion

		#region Public Properties Coords / Exp

		//public RRectangle Coords
		//{
		//	get => _coords;
		//	set
		//	{
		//		if (value != _coords)
		//		{
		//			_coords = value;

		//			OnPropertyChanged(nameof(Left));
		//			OnPropertyChanged(nameof(Bottom));
		//			OnPropertyChanged(nameof(Width));
		//			OnPropertyChanged(nameof(Height));
		//			OnPropertyChanged(nameof(Exponent));

		//			UpdateScreenCoords();
		//		}
		//	}
		//}

		//public int Exponent
		//{
		//	get => _exponent;
		//	set
		//	{
		//		if (value != _exponent)
		//		{
		//			_exponent = Math.Abs(value) * -1;
		//			Coords = new RRectangle(0, 1, 0, 1, value);
		//		}
		//	}
		//}

		#endregion

		#region Public Properties - MapAreaInfo Canvas

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

					SelectionSize = GetSelectionSize(ScreenSize, SelectionWidthPercentage, out var newZoomFactor);
					ZoomFactor = newZoomFactor;

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

					OnPropertyChanged(nameof(ScreenWidthNrm));
					OnPropertyChanged(nameof(ScreenHeightNrm));
				}
			}
		}

		#endregion

		#region Public Properties Zoom Factor

		public int SelectionWidthPerNumerator
		{
			get => _selectionWidthPerNumerator;
			set
			{
				if (value != _selectionWidthPerNumerator)
				{
					_selectionWidthPerNumerator = value;

					SelectionWidthPercentage = GetSelectionWidthPercentage(SelectionWidthPerNumerator, SelectionWidthPerDenominator);
					OnPropertyChanged();
				}
			}
		}

		public int SelectionWidthPerDenominator
		{
			get => _selectionWidthPerDenominator;
			set
			{
				if (value != _selectionWidthPerDenominator)
				{
					_selectionWidthPerDenominator = value;

					SelectionWidthPercentage = GetSelectionWidthPercentage(SelectionWidthPerNumerator, SelectionWidthPerDenominator);
					OnPropertyChanged();
				}
			}
		}

		public double SelectionWidthPercentage
		{
			get => _selectionWidthPercentage;
			set
			{
				if (value != _selectionWidthPercentage)
				{
					_selectionWidthPercentage = value;

					SelectionSize = GetSelectionSize(ScreenSize, SelectionWidthPercentage, out var newZoomFactor);
					ZoomFactor = newZoomFactor;

					OnPropertyChanged();
				}
			}
		}

		public double ZoomFactor
		{
			get => _zoomFactor;
			set
			{
				if (value != _zoomFactor)
				{
					_zoomFactor = value;

					var (n, e) = GetNewSamplePointDelta(CurrentSpdNumerator, CurrentSpdExponent, ZoomFactor, out double diagReciprocal);

					Debug.WriteLine($"The Reciprocal is {diagReciprocal}.");

					ResultantSpdNumerator = n;
					ResultantSpdExponent = e;

					OnPropertyChanged();
				}
			}
		}

		public double ZoomFactorNrm
		{
			get => _zoomFactorNrm;
			set
			{
				if (value != _zoomFactorNrm)
				{
					_zoomFactorNrm = value;

					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Properties - MapAreaInfo Selection

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

					OnPropertyChanged(nameof(SelectionWidthNrm));
					OnPropertyChanged(nameof(SelectionHeightNrm));
				}
			}
		}

		#endregion

		#region Public Properties Sample Point Deltas

		public long CurrentSpdNumerator
		{
			get => _currentSpdNumerator;
			set
			{
				if (value != _currentSpdNumerator)
				{
					_currentSpdNumerator = value;

					OnPropertyChanged();
				}
			}
		}

		public int CurrentSpdExponent
		{
			get => _currentSpdExponent;
			set
			{
				if (value != _currentSpdExponent)
				{
					_currentSpdExponent = value;

					OnPropertyChanged();
				}
			}
		}

		public long ResultantSpdNumerator
		{
			get => _resultantSpdNumerator;
			set
			{
				if (value != _resultantSpdNumerator)
				{
					_resultantSpdNumerator = value;

					OnPropertyChanged();
				}
			}
		}

		public int ResultantSpdExponent
		{
			get => _resultantSpdExponent;
			set
			{
				if (value != _resultantSpdExponent)
				{
					_resultantSpdExponent = value;

					OnPropertyChanged();
				}
			}
		}
		#endregion

		#region The Coords Component Properties

		//public long Left
		//{
		//	get => (long)_coords.Left.Value;
		//	set
		//	{
		//		if (value != Left)
		//		{
		//			_coords.Values[0] = value;
		//			OnPropertyChanged();
		//			OnPropertyChanged(nameof(Width));
		//		}
		//	}
		//}

		//public long Right
		//{
		//	get => (long)_coords.Right.Value;
		//	set
		//	{
		//		if (value != Right)
		//		{
		//			_coords.Values[1] = value;
		//			OnPropertyChanged();
		//			OnPropertyChanged(nameof(Width));
		//		}
		//	}
		//}

		//public long Bottom
		//{
		//	get => (long)_coords.Bottom.Value;
		//	set
		//	{
		//		if (value != Bottom)
		//		{
		//			_coords.Values[2] = value;
		//			OnPropertyChanged();
		//			OnPropertyChanged(nameof(Height));
		//		}
		//	}
		//}

		//public long Top
		//{
		//	get => (long)_coords.Top.Value;
		//	set
		//	{
		//		if (value != Top)
		//		{
		//			_coords.Values[3] = value;
		//			OnPropertyChanged();
		//			OnPropertyChanged(nameof(Height));
		//		}
		//	}
		//}

		//public int Precision
		//{
		//	get => _coords.Precision;
		//	set
		//	{
		//		if (value != _coords.Precision)
		//		{
		//			_coords.Precision = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public long Width
		//{
		//	get => (long)_coords.Width.Value;
		//	set { }
		//}

		//public long Height
		//{
		//	get => (long)_coords.Height.Value;
		//	set { }
		//}

		#endregion

		#region Private Methods

		private double GetSelectionWidthPercentage(double n, double d)
		{
			return 100 * n / d;
		}

		private SizeDbl GetSelectionSize(SizeInt screenSize, double selectionWidthPercentage, out double zoomFactor)
		{
			// 128 / 16 = 8
			// screen width / selection width = scale factor

			// selection width = screen width / scale factor

			var selectionSizeWidth = GetSelectionSizeWidth(screenSize, selectionWidthPercentage);
			var result = new SizeDbl(selectionSizeWidth);

			zoomFactor = ScreenWidth / selectionSizeWidth;

			return result;
		}

		private double GetSelectionSizeWidth(SizeInt screenSize, double selectionWidthPercentage)
		{
			if (selectionWidthPercentage == 0)
			{
				return 1;
			}

			var result = screenSize.Width * selectionWidthPercentage / 100;

			return result;
		}

		private (long numerator, int exponent) GetNewSamplePointDelta(long numerator, int exponent, double factor, out double diagReciprocal)
		{
			RValue curSamplePointDelta;

			if (numerator == -1)
			{
				curSamplePointDelta = new RValue(1, 1);
			}
			else
			{
				curSamplePointDelta = new RValue(numerator, exponent);
			}

			var newSamplePointDelta = GetNewSamplePointDelta(curSamplePointDelta, ZoomFactor, out diagReciprocal);

			var resultNumerator = ConvertToLong(newSamplePointDelta.Value);
			var resultExponent = newSamplePointDelta.Exponent;

			return (resultNumerator, resultExponent);
		}

		private long ConvertToLong(BigInteger n)
		{
			if (n < long.MaxValue && n > long.MinValue)
			{
				return (long)n;
			}
			else
			{
				return -1;
			}
		}

		private RValue GetNewSamplePointDelta(RValue currentSamplePointDelta, double factor, out double diagReciprocal)
		{
			//var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPanThenZoom(currentMapAreaInfo, panAmount, factor, out var diaReciprocal);

			var pos = new RPoint(1, 1, currentSamplePointDelta.Exponent);
			var curRpointAndDelta = new RPointAndDelta(pos, new RSize(currentSamplePointDelta));

			var scaledPd = RMapHelper.GetNewSamplePointDelta(curRpointAndDelta, factor, out diagReciprocal);

			var result = scaledPd.SamplePointDelta.Width;

			return result;
		}

		#endregion

		#region Not Used 1

		private (VectorInt zoomPoint, double factor) GetAreaSelectedParams(RectangleDbl area, SizeDbl displaySize)
		{
			Debug.Assert(area.Width > 0 && area.Height > 0, "Selction Rectangle has a zero or negative value its width or height.");

			var selectionCenter = area.GetCenter();
			var zoomPoint = GetCenterOffset(selectionCenter, displaySize);

			//CheckSelectedCenterPosition(selectionCenter);

			var factor = RMapHelper.GetSmallestScaleFactor(area.Size, displaySize);

			CheckScaleFactor(area, displaySize, factor);

			return (zoomPoint, factor);
		}

		// Return the distance from the Canvas Center to the new mouse position.
		private VectorInt GetCenterOffset(PointDbl selectionCenter, SizeDbl canvasSize)
		{
			var startP = new PointDbl(canvasSize.Width / 2, canvasSize.Height / 2);

			//var endP = new PointDbl(selectionCenter.X, _canvas.ActualHeight - selectionCenter.Y);
			var endP = new PointDbl(selectionCenter.X, selectionCenter.Y);

			var vectorDbl = endP.Diff(startP);

			var result = vectorDbl.Round();

			return result;
		}

		//[Conditional("DEBUG2")]
		//private void CheckSelectedCenterPosition(PointDbl selectionCenter)
		//{
		//	var selCenterPos = ScreenTypeHelper.ConvertToPointDbl(SelectedCenterPosition);

		//	if (!ScreenTypeHelper.IsPointDblChanged(selCenterPos, selectionCenter, threshold: 0.001))
		//	{
		//		Debug.WriteLine("Yes, we can use the Selected Position, instead of calling area.GetCenter().");
		//	}
		//}

		[Conditional("DEBUG2")]
		private void CheckScaleFactor(RectangleDbl area, SizeDbl displaySize, double factor)
		{
			var factor2D = displaySize.Divide(area.Size);
			var factorCheck = Math.Min(factor2D.Width, factor2D.Height);

			Debug.Assert(factorCheck == factor, "GetSmallestScaleFactor is not the same.");
		}

		private void UpdateScreenCoords()
		{
			//var screenMapAreaInfo = GetMapAreaInfo(Coords, new SizeDbl(ScreenSize), _blockSize);
			//MapAreaInfoViewModelCanS.UpdateMapAreaInfo(screenMapAreaInfo, Coords);

			//UpdateSelectionCoords();
		}

		private void UpdateSelectionCoords()
		{
			//var screenMapAreaInfo = MapAreaInfoViewModelCanS.MapAreaInfo;
			//var mapPosition = screenMapAreaInfo.Coords.Position;
			//var samplePointDelta = screenMapAreaInfo.Subdivision.SamplePointDelta;

			//var selectedRectangle = new RectangleInt(new PointInt(), SelectionSize.Round());
			//var selectedCoords = GetMapCoords(selectedRectangle, mapPosition, samplePointDelta);

			//var screenSize = screenMapAreaInfo.CanvasSize;
			//var selectedMapAreaInfo = GetMapAreaInfo(selectedCoords, screenSize, _blockSize);

			//MapAreaInfoViewModelSelS.UpdateMapAreaInfo(selectedMapAreaInfo, selectedCoords);
		}

		private void UpdateScreenCoordsNrm()
		{
			//var screenMapAreaInfo = GetMapAreaInfo(Coords, ScreenSize, _blockSize);
			//MapAreaInfoViewModelCanS.UpdateMapAreaInfo(screenMapAreaInfo, Coords);

			UpdateSelectionCoordsNrm();
		}

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


		//private MapPositionSizeAndDelta GetMapAreaInfo(RRectangle coords, SizeDbl canvasSize, SizeInt blockSize)
		//{
		//	// Use the exact canvas size -- do not adjust based on aspect ratio of the newArea.
		//	var displaySize = canvasSize.Round();

		//	// Using the size of the new map and the map coordinates, calculate the sample point size
		//	//var samplePointDelta = RMapHelper.GetSamplePointDelta(ref updatedCoords, canvasSize, TOLERANCE_FACTOR);

		//	// Using the size of the new map and the map coordinates, calculate the sample point size
		//	var samplePointDelta = GetSamplePointDelta(coords, displaySize, TOLERANCE_FACTOR, out var wToHRatio);

		//	// The samplePointDelta may require the coordinates to be adjusted.
		//	var updatedCoords = AdjustCoordsWithNewSPD(coords, samplePointDelta, displaySize);

		//	Debug.WriteLine($"\nThe new coords are : {updatedCoords},\n old = {coords}. (While calculating SamplePointDelta.)\n");

		//	var samplePointDeltaD = RMapHelper.GetSamplePointDiag(coords, displaySize, out var newDCoords);
		//	RMapHelper.ReportSamplePointDiff(samplePointDelta, samplePointDeltaD, coords, updatedCoords, newDCoords);


		//	// Determine the amount to translate from our coordinates to the subdivision coordinates.
		//	//var mapBlockOffset = RMapHelper.GetMapBlockOffset(ref updatedCoords, samplePointDelta, blockSize, out var canvasControlOffset);

		//	var mapBlockOffset = GetMapBlockOffset(updatedCoords.Position, samplePointDelta, blockSize, out var canvasControlOffset);
		//	//var newCoords = RMapHelper.CombinePosAndSize(newPosition, updatedCoords.Size);

		//	var subdivision = _subdivisonProvider.GetSubdivision(samplePointDelta, mapBlockOffset, out var localMapBlockOffset);

		//	var binaryPrecision = GetBinaryPrecision(updatedCoords, samplePointDelta, out var decimalPrecision);
		//	var binaryPrecisionRounded = (int)binaryPrecision;
		//	var result = new MapPositionSizeAndDelta(updatedCoords, new SizeDbl(displaySize), subdivision, binaryPrecisionRounded, localMapBlockOffset, canvasControlOffset, subdivision.Id);

		//	return result;
		//}

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

			var result = RMapHelper.GetOffsetInBlockSizeUnits(offsetInSamplePoints, blockSize, out canvasControlOffset);

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
