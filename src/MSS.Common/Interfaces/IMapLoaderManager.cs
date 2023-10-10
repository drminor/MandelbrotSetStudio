using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapLoaderManager : IDisposable
	{
		event EventHandler<JobProgressInfo>? RequestAdded;
		event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		//int GetNextJobNumber();

		int GetLimbCount(int precision);

		MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings);
		MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId, VectorLong mapBlockOffset,
			int precision, bool crossesXZero, MapCalcSettings mapCalcSettings);

		//List<MapSection> PushV1(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration);

		// New version for use with the MapSectionRequestProcessor:: SubmitRequests method.
		List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration);

		//Task? GetTaskForJob(int jobNumber);
		TimeSpan? GetExecutionTimeForJob(int jobNumber);
		//int GetPendingRequests(int jobNumber);

		void StopJob(int jobNumber);
		void StopJobs(List<int> jobNumbers);

		//void CancelRequests(IList<MapSection> sectionsToCancel);
		//void CancelRequests(IList<MapSectionRequest> requestsToCancel);

		//long NumberOfCountValSwitches { get; }
	}
}