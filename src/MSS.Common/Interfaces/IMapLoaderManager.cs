using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MSS.Common
{
	public interface IMapLoaderManager
	{
		event EventHandler<MsrJob>? RequestAdded;

		MsrJob CreateMapSectionRequestJob(JobType jobType, ObjectId jobId, OwnerType jobOwnerType, MapPositionSizeAndDelta mapAreaInfo, MapCalcSettings mapCalcSettings);

		MsrJob CreateNewCopy(MsrJob s);

		List<MapSection> Push(MsrJob msrJob, List<MapSectionRequest> mapSectionRequests, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback, CancellationToken ct, out List<MapSectionRequest> requestsPendingGeneration);

	}
}