using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapLoaderManager
	{
		event EventHandler<MapSection> MapSectionReady;

		void Push(JobAreaInfo jobAreaInfo, MapCalcSettings mapCalcSettings);
		void Push(JobAreaInfo jobAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections);
		void StopCurrentJob();
	}
}