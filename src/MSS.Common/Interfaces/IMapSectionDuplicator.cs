using MongoDB.Bson;
using MSS.Types.MSet;

namespace MSS.Common
{
	public interface IMapSectionDuplicator
	{
		long? DuplicateJobMapSections(ObjectId ownerId, OwnerType jobOwnerType, ObjectId newOwnerId);
	}
}
