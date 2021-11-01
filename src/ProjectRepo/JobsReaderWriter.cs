namespace ProjectRepo
{
	class JobsReaderWriter
	{
		private const string COLLECTION_NAME = "Jobs";

		private readonly DbProvider _dbProvider;

		public JobsReaderWriter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

	}
}
