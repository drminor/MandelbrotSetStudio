using System;

namespace MClient
{
	public class SubJob
	{
		public SubJob(IJob parentJob, MapSectionWorkRequest mapSectionWorkRequest)
		{
			ParentJob = parentJob ?? throw new ArgumentNullException(nameof(parentJob));
			MapSectionWorkRequest = mapSectionWorkRequest ?? throw new ArgumentNullException(nameof(mapSectionWorkRequest));
			MapSectionResult = null;
		}

		public readonly IJob ParentJob;
		public readonly MapSectionWorkRequest MapSectionWorkRequest;
		public string ConnectionId => ParentJob.ConnectionId;

		public MapSectionResult MapSectionResult;
	}
}
