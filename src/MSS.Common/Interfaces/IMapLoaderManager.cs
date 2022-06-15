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
		//event EventHandler<Tuple<MapSection, int>>? MapSectionReady;

		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, Action<MapSection, int> callback);
		int Push(JobAreaAndCalcSettings jobAreaAndCalcSettings, IList<MapSection> emptyMapSections, Action<MapSection, int> callback);

		int Push(BigVector mapBlockOffset, IList<MapSectionRequest> mapSectionRequests, Action<MapSection, int> callback);

		Task? GetTaskForJob(int jobNumber);

		void StopJob(int jobNumber);

		long NumberOfCountValSwitches { get; }
	}
}