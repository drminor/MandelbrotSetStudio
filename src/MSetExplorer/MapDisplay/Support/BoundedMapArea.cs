using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	internal class BoundedMapArea
	{
		private readonly MapJobHelper _mapJobHelper;
		private readonly MapAreaInfo2 _mapAreaInfo;
		private double _baseScale;
		private MapAreaInfo _scaledMapAreaInfo;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, MapAreaInfo2 mapAreaInfo, SizeInt posterSize, SizeDbl viewportSize, double contentScale, double baseScale)
		{
			_mapJobHelper = mapJobHelper;
			_mapAreaInfo = mapAreaInfo;

			MapAreaInfoWithSize = _mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfo, posterSize);
			PosterSize = new SizeDbl(MapAreaInfoWithSize.CanvasSize);
			ViewportSize = viewportSize;

			ContentScale = contentScale;
			_baseScale = baseScale;

			ScaleFactor = Math.Pow(0.5, _baseScale);

			_scaledMapAreaInfo = MapAreaInfoWithSize;
		}

		#endregion

		#region Public Properties

		public MapAreaInfo MapAreaInfoWithSize { get; init; }

		public SizeDbl PosterSize { get; init; }

		public SizeDbl ViewportSize { get; private set; }

		public double BaseScale
		{
			get => _baseScale;
			set
			{
				if (value != _baseScale)
				{
					_baseScale = value;
					ScaleFactor = Math.Pow(0.5, _baseScale);

					if (_baseScale == 0)
					{
						_scaledMapAreaInfo = MapAreaInfoWithSize;
					}
					else
					{
						var mapArV2 = _mapJobHelper.GetMapAreaInfoZoomCenter(_mapAreaInfo, ScaleFactor);
						var newUnscaledExtent = PosterSize.Scale(ScaleFactor);
						_scaledMapAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(mapArV2, newUnscaledExtent.Round());
					}
				}
			}
		}

		public double ContentScale { get; set; }

		public double ScaleFactor { get; private set; }

		#endregion

		#region Public Methods

		// New Size and Position
		public MapAreaInfo GetView(SizeDbl viewportSize, VectorDbl newDisplayPosition, double contentScale, double baseScale)
		{
			ViewportSize = viewportSize;
			ContentScale = contentScale;
			BaseScale = baseScale;

			return GetView(newDisplayPosition);
		}

		// New postion, same size
		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			//var newScreenArea = new RectangleDbl(new PointDbl(DisplayPositionWithInverseY), ViewportSize);
			//var scaledNewScreenArea = newScreenArea.Scale(ScaleFactor);

			//var displayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, GetInvertedYPos(newDisplayPosition.Y));
			//var scaledDispPos = displayPositionWithInverseY.Scale(ScaleFactor);

			//// -- Scale the Position and Size separately
			//var scaledDispPos = GetScaledDisplayPosition(newDisplayPosition, out _);
			//var scaledViewportSize = ViewportSize.Scale(ScaleFactor);
			//var scaledNewScreenArea = new RectangleDbl(scaledDispPos, scaledViewportSize);


			// -- Scale the Position and Size together.
			var invertedY = GetInvertedYPos(newDisplayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, ViewportSize);
			var scaledNewScreenArea = newScreenArea.Scale(ScaleFactor);

			var result = GetUpdatedMapAreaInfo(scaledNewScreenArea, _scaledMapAreaInfo);

			return result;
		}

		public RectangleDbl GetScaledScreenAreaNotUsed(VectorDbl displayPosition, SizeDbl viewportSize, out double unInvertedY)
		{
			var t = displayPosition.Scale(ScaleFactor);
			unInvertedY = t.Y;

			// Invert first, then scale
			var invertedY = GetInvertedYPos(displayPosition.Y);
			var displayPositionWithInverseY = new VectorDbl(displayPosition.X, invertedY);
			var newScreenArea = new RectangleDbl(displayPositionWithInverseY, viewportSize);
			var scaledNewScreenArea = newScreenArea.Scale(ScaleFactor);

			return scaledNewScreenArea;
		}

		public VectorDbl GetScaledDisplayPosition(VectorDbl displayPosition, out double unInvertedY)
		{
			var t = displayPosition.Scale(ScaleFactor);
			unInvertedY = t.Y;

			// Invert first, then scale
			var invertedY = GetInvertedYPos(displayPosition.Y);
			var result = new VectorDbl(displayPosition.X, invertedY).Scale(ScaleFactor);

			return result;
		}

		#endregion

		#region Private Methods

		private MapAreaInfo GetUpdatedMapAreaInfo(RectangleDbl newScreenArea, MapAreaInfo mapAreaInfoWithSize)
		{
			var newCoords = _mapJobHelper.GetMapCoords(newScreenArea.Round(), mapAreaInfoWithSize.MapPosition, mapAreaInfoWithSize.SamplePointDelta);
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, mapAreaInfoWithSize.Subdivision, newScreenArea.Size.Round());

			return mapAreaInfoV1;
		}

		private double GetInvertedYPos(double yPos)
		{
			// The yPos has not been scaled, use the same values, used by the PanAndZoomControl

			var maxV = PosterSize.Height;

			// Calculate the physicalViewport Size using the ContentViewportSize.
			var physicalViewportHeight = ViewportSize.Height * ScaleFactor;
			var result = maxV - (yPos + physicalViewportHeight);

			//var result = maxV - (yPos + ViewportSize.Height);

			//var result = maxV - yPos;

			return result;
		}

		#endregion
	}
}
