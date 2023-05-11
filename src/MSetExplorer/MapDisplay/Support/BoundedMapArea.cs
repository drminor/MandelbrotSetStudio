using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	internal class BoundedMapArea
	{
		private readonly MapJobHelper _mapJobHelper;
		private MapAreaInfo _virtualScreenAreaInfo;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl viewPortSize, SizeInt posterSize, VectorDbl? displayPosition = null)
		{
			_mapJobHelper = mapJobHelper;

			AreaColorAndCalcSettings = areaColorAndCalcSettings;
			ViewportSize = viewPortSize;
			PosterSize = posterSize;

			var dispPos = displayPosition ?? new VectorDbl(0, 0);

			DisplayPositionWithInverseY = new VectorDbl(dispPos.X, GetInvertedYPos(dispPos.Y));

			_virtualScreenAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(MapAreaInfo, PosterSize);
		}

		#endregion

		#region Public Properties

		public AreaColorAndCalcSettings AreaColorAndCalcSettings { get; init; }

		public MapAreaInfo2 MapAreaInfo => AreaColorAndCalcSettings.MapAreaInfo;

		public SizeInt PosterSize { get; init; }
		public SizeDbl ViewportSize { get; set; }

		public VectorDbl DisplayPositionWithInverseY { get; private set; }

		#endregion

		#region Public Methods

		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			DisplayPositionWithInverseY = new VectorDbl(newDisplayPosition.X, GetInvertedYPos(newDisplayPosition.Y));

			var result = GetUpdatedMapAreaInfo(DisplayPositionWithInverseY);

			return result;
		}

		#endregion

		#region Private Methods

		private MapAreaInfo GetUpdatedMapAreaInfo(VectorDbl displayPositionWithInverseY)
		{
			var newScreenArea = new RectangleDbl(new PointDbl(displayPositionWithInverseY), ViewportSize);

			var newScreenAreaInt = newScreenArea.Round();

			var newCoords = _mapJobHelper.GetMapCoords(newScreenAreaInt, _virtualScreenAreaInfo.MapPosition, _virtualScreenAreaInfo.SamplePointDelta);

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, _virtualScreenAreaInfo.Subdivision, ViewportSize.Round());

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
