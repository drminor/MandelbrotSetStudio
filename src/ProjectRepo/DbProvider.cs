using System;
using MongoDB.Driver;

namespace ProjectRepo
{
	public class DbProvider
	{

		private readonly string _server;
		private readonly int _port;
		private readonly string _databaseName;

		private Lazy<IMongoDatabase> _mongoDatabaseLazy;

		public DbProvider(string server, int port, string databaseName)
		{
			_server = server;
			_port = port;
			_databaseName = databaseName;

			_mongoDatabaseLazy = new Lazy<IMongoDatabase>(GetDb, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

		private IMongoDatabase GetDb()
		{
			var settings = new MongoClientSettings
			{
				Server = new MongoServerAddress(_server, _port),
				IPv6 = true,
			};

			var dbClient = new MongoClient(settings);

			var projectsDb = dbClient.GetDatabase(_databaseName);
			return projectsDb;
		}
	}
}
