using MongoDB.Bson;
using MSS.Types;
namespace MSS.Common
{
	public interface IMapSectionDuplicator
	{
		long? DuplicateJobMapSections(ObjectId ownerId, OwnerType jobOwnerType, ObjectId newOwnerId);
	}
}
