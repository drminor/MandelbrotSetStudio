using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IMapLoaderManager
	{
		//event EventHandler<MapSection> MapSectionReady;

		event EventHandler<Tuple<MapSection, int>>? MapSectionReady;
		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings);
		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection> emptyMapSections);
		void StopCurrentJob(int jobNumber);
	}
}