using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class BoundedMapArea
	{
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapAreaInfo2 _mapAreaInfo;
		private double _baseFactor;
		private double _baseScale;
		private MapAreaInfo _scaledMapAreaInfo;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, MapAreaInfo2 mapAreaInfo, SizeDbl posterSize, SizeDbl viewportSize, double baseFactor = 0)
		{
			_mapJobHelper = mapJobHelper;
			_mapAreaInfo = mapAreaInfo;

			PosterSize = posterSize;
			ContentViewportSize = viewportSize;

			MapAreaInfoWithSize = GetMapAreaWithSizeForScale(_mapAreaInfo, PosterSize, 1);

			BaseFactor = baseFactor;
			BaseScale = Math.Pow(0.5, _baseFactor);

			
			_scaledMapAreaInfo = GetMapAreaWithSizeForScale(_mapAreaInfo, PosterSize, BaseScale);
		}

		//Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(MapAreaInfoWithSize.CanvasSize, posterSize), $"Since the scale factor = 1, the MapAreaInfoV1.CanvasSize should equal the PosterSize. Compare: {MapAreaInfoWithSize.CanvasSize} with {posterSize}.");

		#endregion

		#region Public Properties

		public SizeDbl PosterSize { get; init; }

		public SizeDbl ContentViewportSize { get; private set; }

		//public SizeDbl UnscaledViewportSize { get; private set; }	

		public MapAreaInfo MapAreaInfoWithSize { get; init; }

		public double BaseFactor
		{
			get => _baseFactor;
			private set => _baseFactor = value;
		}

		public double BaseScale
		{
			get => _baseScale;
			private set => _baseScale = value;
		}

		#endregion

		#region Public Methods

		// New Size and Position
		public void UpdateSizeAndScale(SizeDbl contentViewportSize, double baseFactor)
		{
			ContentViewportSize = contentViewportSize;
			BaseFactor = baseFactor;

			BaseScale = Math.Pow(0.5, BaseFactor);
			_scaledMapAreaInfo = GetMapAreaWithSizeForScale(_mapAreaInfo, PosterSize, BaseScale);
		}

		// New position, same size
		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			// -- Scale the Position and Size together.
			var invertedY = GetInvertedYPos(newDisplayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, ContentViewportSize);
			var scaledNewScreenArea = newScreenArea.Scale(BaseScale);

			var result = GetUpdatedMapAreaInfo(scaledNewScreenArea, _scaledMapAreaInfo);

			return result;
		}

		public RectangleDbl GetScaledScreenAreaNotUsed(VectorDbl displayPosition, SizeDbl viewportSize, out double unInvertedY)
		{
			var t = displayPosition.Scale(BaseScale);
			unInvertedY = t.Y;

			// Invert first, then scale
			var invertedY = GetInvertedYPos(displayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(displayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, viewportSize);
			var scaledNewScreenArea = newScreenArea.Scale(BaseScale);

			return scaledNewScreenArea;
		}

		// Locations in the Content coordinates only use the ContentScale, the Base / Relative breakdown is only for data and physical views.
		public VectorDbl GetScaledDisplayPosition(VectorDbl displayPosition, out double scaledButNotInvertedY)
		{
			var t = displayPosition.Scale(BaseScale);
			scaledButNotInvertedY = t.Y;

			// Invert first, then scale
			var invertedY = GetInvertedYPos(displayPosition.Y);
			var result = new VectorDbl(displayPosition.X, invertedY).Scale(BaseScale);

			return result;
		}

		public VectorDbl GetUnScaledDisplayPosition(VectorDbl scaledDisplayPosition)
		{
			var inverseBaseScale = Math.Pow(2, BaseFactor);

			// First unscale, then invert
			var unscaledDisplayPosition = scaledDisplayPosition.Scale(inverseBaseScale);
			var unInvertedY = GetInvertedYPos(unscaledDisplayPosition.Y);

			var result = new VectorDbl(unscaledDisplayPosition.X, unInvertedY);

			//var invertedY = GetInvertedYPos(scaledDisplayPosition.Y);
			//var result = new VectorDbl(scaledDisplayPosition.X, invertedY); //.Scale(BaseScale);

			return result;
		}


		#endregion

		#region Private Methods

		private MapAreaInfo GetMapAreaWithSizeForScale(MapAreaInfo2 mapAreaInfo, SizeDbl posterSize, double scale)
		{
			var adjustedMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoom(mapAreaInfo, scale, out var diaReciprocal);
			var displaySize = posterSize.Scale(scale);
			var result = _mapJobHelper.GetMapAreaWithSize(adjustedMapAreaInfo, displaySize);
			result.OriginalSourceSubdivisionId = mapAreaInfo.Subdivision.Id;

			return result;
		}

		private MapAreaInfo GetUpdatedMapAreaInfo(RectangleDbl newScreenArea, MapAreaInfo mapAreaInfoWithSize)
		{
			Debug.WriteLine($"Getting Updated MapAreaInfo. newPos: {newScreenArea.Position}, newSize: {newScreenArea.Size}. From MapAreaInfo: CanvasSize: {mapAreaInfoWithSize.CanvasSize}, BlockOffset: {mapAreaInfoWithSize.MapBlockOffset}, spd: {mapAreaInfoWithSize.SamplePointDelta.Width}.");

			var newCoords = _mapJobHelper.GetMapCoords(newScreenArea.Round(), mapAreaInfoWithSize.MapPosition, mapAreaInfoWithSize.SamplePointDelta);
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, mapAreaInfoWithSize.Subdivision, mapAreaInfoWithSize.OriginalSourceSubdivisionId, newScreenArea.Size);

			return mapAreaInfoV1;
		}

		public double GetInvertedYPos(double yPos)
		{
			// The yPos has not been scaled, use the same values, used by the PanAndZoomControl

			var maxV = PosterSize.Height - ContentViewportSize.Height; // UnscaledViewportSize.Height;
			var result = maxV - yPos;

			return result;
		}

		#endregion

		#region Experimental

		public ScaledDisplayPosition GetWorkingPosition(VectorDbl newDisplayPosition, double baseFactor)
		{
			var scaledDispPos = GetScaledDisplayPosition(newDisplayPosition, out var scaledButNotInvertedY);

			var invertedYPos = GetInvertedYPos(newDisplayPosition.Y);

			var result = new ScaledDisplayPosition(
				baseFactor: baseFactor,
				unscaled: newDisplayPosition,
				yInvertedAndScaled: scaledDispPos,
				invertedY: invertedYPos,
				scaledY: scaledButNotInvertedY);

			return result;
		}

		public class ScaledDisplayPosition
		{
			public ScaledDisplayPosition(double baseFactor,
				VectorDbl unscaled, VectorDbl yInvertedAndScaled,
				double invertedY, double scaledY)
			{
				BaseFactor = baseFactor;
				Unscaled = unscaled;
				YInvertedAndScaled = yInvertedAndScaled;
				InvertedY = invertedY;
				ScaledButNotInvertedY = scaledY;
			}

			public double BaseFactor { get; init; }

			public VectorDbl Unscaled { get; init; }
			public VectorDbl YInvertedAndScaled { get; init; }

			public double X => Unscaled.X;

			public double Y => Unscaled.Y;

			public double InvertedY { get; init; }

			public double InvertedAndScaledX => YInvertedAndScaled.X;
			public double InvertedAndScaledY => YInvertedAndScaled.Y;

			public double ScaledButNotInvertedY { get; init; }

		}


		#endregion
	}
}
