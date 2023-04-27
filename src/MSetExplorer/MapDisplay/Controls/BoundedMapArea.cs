using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer.MapDisplay.Controls
{
	internal class BoundedMapArea
	{
		private readonly MapJobHelper _mapJobHelper;
		//private VectorDbl _displayPosition;

		private MapAreaInfo _virtualScreenAreaInfo;
		//private BigVector _pixelOrigin;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl viewPortSize, SizeInt posterSize, VectorDbl? displayPosition = null)
		{
			_mapJobHelper = mapJobHelper;

			AreaColorAndCalcSettings = areaColorAndCalcSettings;
			ViewPortSize = viewPortSize;
			PosterSize = posterSize;

			DisplayPosition = displayPosition ?? new VectorDbl(0, 0);

			_virtualScreenAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(MapAreaInfo, PosterSize);

			//_pixelOrigin = new BigVector(
			//	_virtualScreenAreaInfo.MapBlockOffset.X * 128 + _virtualScreenAreaInfo.CanvasControlOffset.X,
			//	_virtualScreenAreaInfo.MapBlockOffset.Y * 128 + _virtualScreenAreaInfo.CanvasControlOffset.Y);


		}

		#endregion

		#region Public Properties

		public AreaColorAndCalcSettings AreaColorAndCalcSettings { get; init; }

		public MapAreaInfo2 MapAreaInfo => AreaColorAndCalcSettings.MapAreaInfo;

		public SizeDbl ViewPortSize { get; init; }
		public SizeInt PosterSize { get; init; }

		public VectorDbl DisplayPosition { get; private set; }
		//{
		//	get => _displayPosition;
			
		//	private set
		//	{
		//		if (ScreenTypeHelper.IsVectorDblChanged(value, _displayPosition))
		//		{
		//			_displayPosition = value;
		//		}
		//	}
		//}

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
			var newScreenArea = new RectangleDbl(new PointDbl(displayPosition), ViewPortSize);

			var newScreenAreaInt = newScreenArea.Round();

			var newCoords = _mapJobHelper.GetMapCoords(newScreenAreaInt, _virtualScreenAreaInfo.MapPosition, _virtualScreenAreaInfo.SamplePointDelta);

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaInfoScaleConstant(newCoords, _virtualScreenAreaInfo.Subdivision, ViewPortSize.Round());

			return mapAreaInfoV1;
		}


		private MapAreaInfo2 GetUpdatedMapAreaInfoOLD(VectorDbl displayPosition)
		{
			// TODO: Update the BoundedMapArea, GetView logic.
			var newCenter = new PointDbl(ViewPortSize.Width / 2, ViewPortSize.Height / 2);


			var oldCenter = new PointDbl(PosterSize.Width / 2, PosterSize.Height / 2);
			var panAmount = newCenter.Diff(oldCenter).Round();


			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(MapAreaInfo, panAmount);

			return newMapAreaInfo;
		}

		#endregion
	}
}
