using MongoDB.Bson;

namespace MSS.Types.MSet
{
	public interface IProjectAdapter
	{
		public ColorBandSet InsertColorBandSet(ColorBandSet colorBandSet);

		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);

		public Job InsertJob(Job job);

		void UpdateProjectName(ObjectId projectId, string name);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateJobDetails(Job job);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);
	}
}