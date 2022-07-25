using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IMapSectionDeleter
	{
		long? DeleteMapSectionsForJob(ObjectId ownerId, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsForMany(IEnumerable<ObjectId> ownerIds, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false);
	}
}
