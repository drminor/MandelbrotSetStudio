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

		//bool SaveTheZValues { get; set; }
		//bool CalculateEscapeVelocities { get; set; }

		int GetNextJobNumber();

		//List<MapSection> Push(JobType jobType, string jobId, OwnerType jobOwnerType, MapAreaInfo mapAreaInfo, MapCalcSettings mapCalcSettings,
		//	IList<MapSection> emptyMapSections, Action<MapSection> callback, out int jobNumber, out IList<MapSection> mapSectionsPendingGeneration);

		List<MapSection> Push(List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, out int jobNumber, out List<MapSectionRequest> pendingGeneration);

		Task? GetTaskForJob(int jobNumber);
		TimeSpan? GetExecutionTimeForJob(int jobNumber);
		int GetPendingRequests(int jobNumber);

		void StopJob(int jobNumber);
		void StopJobs(List<int> jobNumbers);
		//void CancelRequests(IList<MapSection> sectionsToCancel);

		//void CancelRequests(IList<MapSectionRequest> requestsToCancel);



		//long NumberOfCountValSwitches { get; }
	}
}