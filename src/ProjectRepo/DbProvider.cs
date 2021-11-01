using System;
using MongoDB.Driver;

namespace ProjectRepo
{
	public class DbProvider
	{
		private const string DB_NAME = "MandelbrotProjects";
		private readonly string _connectionString;

		private Lazy<IMongoDatabase> _mongoDatabaseLazy;

		public DbProvider(string connectionString)
		{
			_connectionString = connectionString;
			_mongoDatabaseLazy = new Lazy<IMongoDatabase>(GetDb, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

		private IMongoDatabase GetDb()
		{
			MongoClient dbClient = new MongoClient(_connectionString);
			IMongoDatabase projectsDb = dbClient.GetDatabase(DB_NAME);
			return projectsDb;
		}
	}
}
