using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Windows;

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

			DisplayPosition = displayPosition ?? new VectorDbl(0, 0);

			_virtualScreenAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(MapAreaInfo, PosterSize);
		}

		#endregion

		#region Public Properties

		public AreaColorAndCalcSettings AreaColorAndCalcSettings { get; init; }

		public MapAreaInfo2 MapAreaInfo => AreaColorAndCalcSettings.MapAreaInfo;

		public SizeInt PosterSize { get; init; }
		public SizeDbl ViewportSize { get; set; }

		public VectorDbl DisplayPosition { get; private set; }

		#endregion

		#region Public Methods

		public MapAreaInfo GetView(VectorDbl newDisplayPosition)
		{
			DisplayPosition = newDisplayPosition;

			var result = GetUpdatedMapAreaInfo(DisplayPosition);

			return result;
		}

		#endregion

		#region Private Methods

		private MapAreaInfo GetUpdatedMapAreaInfo(VectorDbl displayPosition)
		{
			var newScreenArea = new RectangleDbl(new PointDbl(displayPosition), ViewportSize);

			var newScreenAreaInt = newScreenArea.Round();

			var newCoords = _mapJobHelper.GetMapCoords(newScreenAreaInt, _virtualScreenAreaInfo.MapPosition, _virtualScreenAreaInfo.SamplePointDelta);

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, _virtualScreenAreaInfo.Subdivision, ViewportSize.Round());

			return mapAreaInfoV1;
		}

		#endregion
	}
}
