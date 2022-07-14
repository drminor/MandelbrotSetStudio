using System;
using MongoDB.Driver;

namespace ProjectRepo
{
	public class DbProvider
	{
		private const string DB_NAME = "MandelbrotProjects";
		//private readonly string _connectionString;

		//mongodb://desktop-bau7fe6:27017";

		private readonly string _server;
		private readonly int _port;

		private Lazy<IMongoDatabase> _mongoDatabaseLazy;

		public DbProvider(string server, int port)
		{
			//_server = "desktop-bau7fe6";
			//_server = "localhost";
			//_server = "davidmain";
			//_port = 27017;

			_server = server;
			_port = port;

			//_connectionString = connectionString;
			_mongoDatabaseLazy = new Lazy<IMongoDatabase>(GetDb, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
		}

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

		private IMongoDatabase GetDb()
		{
			var settings = new MongoClientSettings
			{
				Server = new MongoServerAddress(_server, _port),
				IPv6 = true
			};

			var dbClient = new MongoClient(settings);

			var projectsDb = dbClient.GetDatabase(DB_NAME);
			return projectsDb;
		}
	}
}
