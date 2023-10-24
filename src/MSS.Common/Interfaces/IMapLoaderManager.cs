using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MSS.Common
{
	public interface IMapLoaderManager : IDisposable
	{
		event EventHandler<MsrJob>? RequestAdded;

		MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, MapPositionSizeAndDelta mapAreaInfo, MapCalcSettings mapCalcSettings);

		//MsrJob CreateMapSectionRequestJob(JobType jobType, string jobId, OwnerType jobOwnerType, Subdivision subdivision, string originalSourceSubdivisionId, VectorLong mapBlockOffset,
		//	int precision, bool crossesYZero, MapCalcSettings mapCalcSettings);

		MsrJob CreateNewCopy(MsrJob s);

		List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration);

		//long NumberOfCountValSwitches { get; }
	}
}