using MongoDB.Bson;
using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public interface IMapSectionDuplicator
	{
		long? DuplicateJobMapSections(ObjectId ownerId, JobOwnerType jobOwnerType, ObjectId newOwnerId);

		long? DeleteMapSectionsForJob(ObjectId ownerId, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsForMany(IList<ObjectId> ownerIds, JobOwnerType jobOwnerType);
	}

}
