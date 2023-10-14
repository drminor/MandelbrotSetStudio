using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MSS.Common
{
	public interface IMapLoaderManager : IDisposable
	{
		//event EventHandler<JobProgressInfo>? RequestAdded;
		//event EventHandler<MapSectionProcessInfo>? SectionLoaded;

		event EventHandler<MsrJob>? RequestAdded2;

		MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings);
		MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId, VectorLong mapBlockOffset,
			int precision, bool crossesXZero, MapCalcSettings mapCalcSettings);

		List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration);

		//List<MapSection> PushV1(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out List<MapSectionRequest> pendingGeneration);

		// New version for use with the MapSectionRequestProcessor:: SubmitRequests method.
		//List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration);

		//int GetNextJobNumber();

		//int GetLimbCount(int precision);
		//Task? GetTaskForJob(int jobNumber);
		//TimeSpan? GetExecutionTimeForJob(int jobNumber);
		//int GetPendingRequests(int jobNumber);

		//void StopJob(int jobNumber);
		//void StopJobs(List<int> jobNumbers);

		//void CancelRequests(IList<MapSection> sectionsToCancel);
		//void CancelRequests(IList<MapSectionRequest> requestsToCancel);

		//long NumberOfCountValSwitches { get; }
	}
}