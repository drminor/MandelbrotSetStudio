using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IMapLoaderManager
	{
		event EventHandler<MapSection> MapSectionReady;

		void Push(JobAreaAndCalcSettings jobAreaAndCalcSettings);
		void Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection> emptyMapSections);
		void StopCurrentJob();
	}
}