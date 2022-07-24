using MongoDB.Bson;

namespace MSS.Types.MSet
{
	public interface IMapSectionDuplicator
	{
		long? DuplicateJobMapSections(ObjectId ownerId, JobOwnerType jobOwnerType, ObjectId newOwnerId);
	}
}
