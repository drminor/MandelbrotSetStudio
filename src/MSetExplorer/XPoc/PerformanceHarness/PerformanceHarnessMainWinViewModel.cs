using MSS.Common;
using MSS.Types.MSet;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Diagnostics;

namespace MSetExplorer.XPoc.PerformanceHarness
{
    class PerformanceHarnessMainWinViewModel : ViewModelBase
	{

		private readonly IMapDisplayViewModel _mapDisplayViewModel;
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapJobHelper _mapJobHelper;

		private readonly MapSectionHelper _mapSectionHelper;

		public PerformanceHarnessMainWinViewModel(IMapDisplayViewModel mapDisplayViewModel, IMapLoaderManager mapLoaderManager, MapJobHelper mapJobHelper, MapSectionHelper mapSectionHelper)
        {
			_mapDisplayViewModel = mapDisplayViewModel;
			_mapLoaderManager = mapLoaderManager;
			_mapJobHelper = mapJobHelper;
			_mapSectionHelper = mapSectionHelper;	
		}

		public void RunBaseLine()
		{
			var blockSize = RMapConstants.BLOCK_SIZE;
			var sizeInWholeBlocks = new SizeInt(8);
			var canvasSize = sizeInWholeBlocks.Scale(blockSize);


			var coords = RMapConstants.ENTIRE_SET_RECTANGLE_EVEN;
			var mapCalcSettings = new MapCalcSettings(targetIterations: 400, requestsPerJob: 100);
			var colorBandSet = RMapConstants.BuildInitialColorBandSet(mapCalcSettings.TargetIterations);
			var job = _mapJobHelper.BuildHomeJob(canvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, blockSize);

			var mapAreaInfo =  _mapJobHelper.GetMapAreaInfo(coords, canvasSize, blockSize);
			_mapLoaderManager.Push(job.ProjectId.ToString(), JobOwnerType.Project, mapAreaInfo, mapCalcSettings, MapSectionReady);
		}


		private void MapSectionReady(MapSection mapSection, int jobId, bool isLast)
		{
			Debug.WriteLine($"Got a mapSection.");
		}

	}
}
