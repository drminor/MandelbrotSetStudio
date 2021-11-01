using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRepo
{
	class JobsReaderWriter
	{
		private const string COLLECTION_NAME = "Jobs";
		//private const string COLLECTION_NAME = "MapSections";

		private readonly DbProvider _dbProvider;

		public JobsReaderWriter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

	}
}
