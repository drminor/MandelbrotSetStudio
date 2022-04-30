using MongoDB.Bson;
using MSS.Types.MSet;

namespace MSS.Types.MSet
{
	public interface IProjectAdapter
	{
		public ColorBandSet CreateColorBandSet(ColorBandSet colorBandSet);

		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);

		//void UpdateColorBandSetParentId(ObjectId colorBandSetId, ObjectId? parentId);
		//void UpdateColorBandSetProjectId(ObjectId colorBandSetId, ObjectId projectId);

		public Job InsertJob(Job job);

		void UpdateJobDetails(Job job);
		void UpdateJobsParent(Job job);
		void UpdateJobsProject(ObjectId jobId, ObjectId projectId);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectName(ObjectId projectId, string name);
	}
}