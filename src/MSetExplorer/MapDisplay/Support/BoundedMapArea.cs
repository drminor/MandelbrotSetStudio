using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class BoundedMapArea
	{
		private readonly MapJobHelper _mapJobHelper;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl viewPortSize, SizeInt posterSize, VectorDbl? displayPosition = null)
		{
			_mapJobHelper = mapJobHelper;

			//AreaColorAndCalcSettings = areaColorAndCalcSettings;
			ViewportSize = viewPortSize;
			PosterSize = posterSize;

			var dispPos = displayPosition ?? new VectorDbl(0, 0);

			DisplayPositionWithInverseY = new VectorDbl(dispPos.X, GetInvertedYPos(dispPos.Y));

			MapAreaInfoWithSize = _mapJobHelper.GetMapAreaWithSizeFat(areaColorAndCalcSettings.MapAreaInfo, PosterSize);
		}

		#endregion

		#region Public Properties

		//public AreaColorAndCalcSettings AreaColorAndCalcSettings { get; init; }

		//public MapAreaInfo2 MapAreaInfo => AreaColorAndCalcSettings.MapAreaInfo;
		public MapAreaInfo MapAreaInfoWithSize { get; init; }

		public SizeInt PosterSize { get; init; }

		public SizeDbl ViewportSize { get; private set; }
		public VectorDbl DisplayPositionWithInverseY { get; private set; }

		#endregion

		#region Public Methods

		// New Size and Position
		public MapAreaInfo GetView(SizeDbl viewportSize, VectorDbl newDisplayPosition)
		{
			ViewportSize = viewportSize;
			DisplayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, GetInvertedYPos(newDisplayPosition.Y));

			var newScreenArea = new RectangleDbl(new PointDbl(DisplayPositionWithInverseY), ViewportSize);
			var result = GetUpdatedMapAreaInfo(newScreenArea);

			return result;
		}

		// New postion, same size
		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			DisplayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, GetInvertedYPos(newDisplayPosition.Y));

			var newScreenArea = new RectangleDbl(new PointDbl(DisplayPositionWithInverseY), ViewportSize);
			var result = GetUpdatedMapAreaInfo(newScreenArea);

			return result;
		}

		#endregion

		#region Private Methods

		private MapAreaInfo GetUpdatedMapAreaInfo(RectangleDbl newScreenArea)
		{
			var newCoords = _mapJobHelper.GetMapCoords(newScreenArea.Round(), MapAreaInfoWithSize.MapPosition, MapAreaInfoWithSize.SamplePointDelta);
			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, MapAreaInfoWithSize.Subdivision, newScreenArea.Size.Round());

			return mapAreaInfoV1;
		}

		public double GetInvertedYPos(double yPos)
		{
			var maxV = PosterSize.Height; //Math.Max(ViewportSize.Height, PosterSize.Height - ViewportSize.Height);
			var result = maxV - (yPos + ViewportSize.Height);

			return result;
		}

		#endregion
	}
}
