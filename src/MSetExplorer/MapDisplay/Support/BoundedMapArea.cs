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

		public BoundedMapArea(MapJobHelper mapJobHelper, MapAreaInfo2 mapAreaInfo, SizeDbl posterSize, SizeDbl viewportSize)
		{
			_mapJobHelper = mapJobHelper;
			_mapAreaInfo = mapAreaInfo;

			_baseFactor = 0;
			_baseScale = 1;

			PosterSize = posterSize;
			ViewportSize = viewportSize;

			//MapAreaInfoWithSize = _mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfo, posterSize);

			//Debug.Assert(MapAreaInfoWithSize.CanvasSize == posterSize, $"GetMapAreaWithSizeFat is returning a CanvasSize: {MapAreaInfoWithSize.CanvasSize} different from the posterSize: {posterSize}.");

			MapAreaInfoWithSize = GetScaledMapAreaInfoV1(mapAreaInfo, posterSize, _baseScale);

			Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(MapAreaInfoWithSize.CanvasSize, posterSize), $"Since the scale factor = 1, the MapAreaInfoV1.CanvasSize should equal the PosterSize. Compare: {MapAreaInfoWithSize.CanvasSize} with {posterSize}.");

			_scaledMapAreaInfo = MapAreaInfoWithSize;
		}

		#endregion

		#region Public Properties

		public MapAreaInfo MapAreaInfoWithSize { get; init; }

		public SizeDbl PosterSize { get; init; }

		public SizeDbl ViewportSize { get; private set; }

		public double BaseFactor
		{
			get => _baseFactor;
			set
			{
				if (value != _baseFactor)
				{
					_baseFactor = value;
					BaseScale = Math.Pow(0.5, _baseFactor);
					_scaledMapAreaInfo = GetScaledMapAreaInfoV1(_mapAreaInfo, PosterSize, BaseScale);
				}
			}
		}

		public double BaseScale
		{
			get => _baseScale;
			private set => _baseScale = value;
		}

		#endregion

		#region Public Methods

		// New Size and Position
		public MapAreaInfo GetView(SizeDbl viewportSize, VectorDbl newDisplayPosition, double baseFactor)
		{
			ViewportSize = viewportSize;
			BaseFactor = baseFactor;

			return GetView(newDisplayPosition);
		}

		// New position, same size
		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			// -- Scale the Position and Size together.
			var invertedY = GetInvertedYPos(newDisplayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, ViewportSize);
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

		public VectorDbl GetScaledDisplayPosition(VectorDbl displayPosition, out double unInvertedY)
		{
			var t = displayPosition.Scale(BaseScale);
			unInvertedY = t.Y;

			// Invert first, then scale
			var invertedY = GetInvertedYPos(displayPosition.Y);
			var result = new VectorDbl(displayPosition.X, invertedY).Scale(BaseScale);

			return result;
		}

		public VectorDbl GetUnScaledDisplayPosition(VectorDbl scaledDisplayPosition, double baseFactor, out double unInvertedY)
		{
			var inverseBaseScale = Math.Pow(2, baseFactor);

			// First unscale, then invert
			var t = scaledDisplayPosition.Scale(inverseBaseScale);
			unInvertedY = GetInvertedYPos(t.Y);

			var result = new VectorDbl(t.X, unInvertedY);

			return result;
		}


		#endregion

		#region Private Methods

		private MapAreaInfo GetScaledMapAreaInfoV1(MapAreaInfo2 mapAreaInfo, SizeDbl posterSize, double scaleFactor)
		{
			var adjustedMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomCenter(mapAreaInfo, scaleFactor, out var diaReciprocal);
			var displaySize = posterSize.Scale(scaleFactor);
			var result = _mapJobHelper.GetMapAreaWithSizeFat(adjustedMapAreaInfo, displaySize);
			result.OriginalSourceSubdivisionId = _mapAreaInfo.Subdivision.Id;

			return result;
		}

		private MapAreaInfo GetUpdatedMapAreaInfo(RectangleDbl newScreenArea, MapAreaInfo mapAreaInfoWithSize)
		{
			var newCoords = _mapJobHelper.GetMapCoords(newScreenArea.Round(), mapAreaInfoWithSize.MapPosition, mapAreaInfoWithSize.SamplePointDelta);
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, mapAreaInfoWithSize.Subdivision, mapAreaInfoWithSize.OriginalSourceSubdivisionId, newScreenArea.Size);

			return mapAreaInfoV1;
		}

		public double GetInvertedYPos(double yPos)
		{
			// The yPos has not been scaled, use the same values, used by the PanAndZoomControl

			var maxV = PosterSize.Height - ViewportSize.Height;
			var result = maxV - yPos;

			return result;
		}

		#endregion
	}
}
