using System;
using System.Linq;
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

		public bool TestConnection(string databaseName, TimeSpan? connectTimeout = null)
		{
			try
			{
				var dbClient = GetClient(connectTimeout);

				var task = dbClient.ListDatabaseNamesAsync();
				var e = task.Result.ToEnumerable();

				if (!e.Any())
				{
					return false;
				}

				task = dbClient.ListDatabaseNamesAsync();
				e = task.Result.ToEnumerable();

				var result = e.FirstOrDefault(x => x.Equals(databaseName)) != null;

				return result;
			}
			catch
			{
				return false;
			}
		}

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

		private IMongoDatabase GetDb()
		{
			var dbClient = GetClient();

			var mongoDatabase = dbClient.GetDatabase(_databaseName);
			return mongoDatabase;
		}

		private MongoClient GetClient(TimeSpan? connectTimeout = null)
		{
			var settings = new MongoClientSettings
			{
				Server = new MongoServerAddress(_server, _port),
				IPv6 = true,
			};

			if (connectTimeout != null)
			{
				settings.ConnectTimeout = connectTimeout.Value;
			}

			var dbClient = new MongoClient(settings);

			return dbClient;

		}
	}
}
