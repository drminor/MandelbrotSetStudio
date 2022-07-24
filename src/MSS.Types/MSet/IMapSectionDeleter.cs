using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public interface IMapSectionDeleter
	{
		long? DeleteMapSectionsForJob(ObjectId ownerId, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsForMany(IList<ObjectId> ownerIds, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false);
	}
}
