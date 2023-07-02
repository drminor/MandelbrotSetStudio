using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IMapSectionDeleter
	{
		long? DeleteMapSectionsForJob(ObjectId jobId, OwnerType jobOwnerType);

		long? DeleteMapSectionsForManyJobs(IEnumerable<ObjectId> jobIds, OwnerType jobOwnerType);

		// TODO: UpdateDeleteMapSectionsWithJobType to use JobType instead of OwnerType
		long? DeleteMapSectionsWithJobType(IList<ObjectId> mapSectionIds, OwnerType jobOwnerType);

		long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false);
	}
}
