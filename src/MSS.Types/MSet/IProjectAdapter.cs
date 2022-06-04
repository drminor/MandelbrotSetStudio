using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public interface IProjectAdapter
	{
		public void InsertColorBandSet(ColorBandSet colorBandSet);

		void UpdateColorBandSetName(ObjectId colorBandSetId, string? name);
		void UpdateColorBandSetDescription(ObjectId colorBandSetId, string? description);
		void UpdateColorBandSetDetails(ColorBandSet colorBandSet);

		public void InsertJob(Job job);
		void UpdateJobDetails(Job job);

		void UpdateProjectName(ObjectId projectId, string name);
		void UpdateProjectDescription(ObjectId projectId, string? description);
		void UpdateProjectCurrentJobId(ObjectId projectId, ObjectId? currentJobId);

		bool TryGetSubdivision(RSize samplePointDelta, SizeInt blockSize, [MaybeNullWhen(false)] out Subdivision subdivision);
		void InsertSubdivision(Subdivision subdivision);
	}
}