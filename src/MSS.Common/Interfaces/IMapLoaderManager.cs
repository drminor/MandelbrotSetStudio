using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapLoaderManager : IDisposable
	{
		event EventHandler<JobProgressInfo>? RequestAdded;
		event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		int Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, Action<MapSection> callback);
		//int Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections, Action<MapSection> callback);

		int Push(IList<MapSectionRequest> mapSectionRequests, Action<MapSection> callback);

		Task? GetTaskForJob(int jobNumber);
		TimeSpan? GetExecutionTimeForJob(int jobNumber);

		void StopJob(int jobNumber);

		long NumberOfCountValSwitches { get; }
	}
}