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

		//int Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, Action<MapSection> callback);

		List<Tuple<MapSectionRequest, MapSectionResponse>> Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, Action<MapSection> callback, out int jobNumber);

		//int Push(string ownerId, JobOwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings, IList<MapSection> emptyMapSections, Action<MapSection> callback);

		//int Push(IList<MapSectionRequest> mapSectionRequests, Action<MapSection> callback);
		List<Tuple<MapSectionRequest, MapSectionResponse>> Push(List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out int jobNumber);

		Task? GetTaskForJob(int jobNumber);
		TimeSpan? GetExecutionTimeForJob(int jobNumber);

		void StopJob(int jobNumber);

		long NumberOfCountValSwitches { get; }
	}
}