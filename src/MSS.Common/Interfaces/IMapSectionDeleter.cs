using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public interface IMapSectionDeleter
	{
		long? DeleteMapSectionsForJob(ObjectId jobId, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsForMany(IEnumerable<ObjectId> jobIds, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsWithJobType(IList<ObjectId> mapSectionIds, JobOwnerType jobOwnerType);

		long? DeleteMapSectionsCreatedSince(DateTime dateCreatedUtc, bool overrideRecentGuard = false);
	}
}
