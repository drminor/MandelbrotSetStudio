using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using static MongoDB.Driver.WriteConcern;

namespace MSetExplorer.MapDisplay.Controls
{
	internal class BoundedMapArea
	{

		private readonly MapJobHelper _mapJobHelper;

		#region Constructor

		public BoundedMapArea(MapJobHelper mapJobHelper, MapAreaInfo2 mapAreaInfo, SizeDbl viewPortSize, SizeInt posterSize, VectorInt? displayPosition = null)
		{
			_mapJobHelper = mapJobHelper;

			MapAreaInfo = mapAreaInfo;
			ViewPortSize = viewPortSize;
			PosterSize = posterSize;
			DisplayPosition = displayPosition ?? new VectorInt(0, 0);
		}

		#endregion

		#region Public Properties

		public MapAreaInfo2 MapAreaInfo { get; init; }
		public SizeDbl ViewPortSize { get; init; }
		public SizeInt PosterSize { get; init; }
		public VectorInt DisplayPosition { get; private set; }

		#endregion

		#region Public Methods

		public MapAreaInfo2 GetView(VectorInt newDisplayPosition)
		{
			DisplayPosition = newDisplayPosition;

			var result = GetUpdatedMapAreaInfo(DisplayPosition);

			return result;
		}

		#endregion


		private MapAreaInfo2 GetUpdatedMapAreaInfo(VectorInt displayPosition)
		{
			// TODO: Update the BoundedMapArea, GetView logic.
			var newCenter = new PointDbl(ViewPortSize.Width / 2, ViewPortSize.Height / 2);


			var oldCenter = new PointDbl(PosterSize.Width / 2, PosterSize.Height / 2);
			var panAmount = newCenter.Diff(oldCenter).Round();


			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(MapAreaInfo, panAmount);

			return newMapAreaInfo;
		}


	}
}
