using MEngineDataContracts;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapLoaderManager
	{
		event EventHandler<Tuple<MapSection, int>>? MapSectionReady;

		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings);
		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection> emptyMapSections);

		int Push(BigVector mapBlockOffset, IList<MapSectionRequest> mapSectionRequests);

		Task? GetTaskForJob(int jobNumber);

		void StopJob(int jobNumber);
	}
}