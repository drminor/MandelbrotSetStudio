using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Linq;

namespace ProjectRepo
{
	public class DbProvider
	{
		private readonly string _server;
		private readonly int _port;
		private readonly string _databaseName;

		private Lazy<IMongoDatabase> _mongoDatabaseLazy;

		//private MongoClient? _client;
		//private IMongoDatabase? _database;

		private readonly bool _useDetailedDebug = false;

		public DbProvider(string server, int port, string databaseName)
		{
			_server = server;
			_port = port;
			_databaseName = databaseName;

			_mongoDatabaseLazy = new Lazy<IMongoDatabase>(GetDb, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

			//_client = null;
			//_database = null;
		}

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

		//public IMongoDatabase Database
		//{
		//	get
		//	{
		//		if (_database == null)
		//		{
		//			_client = GetClient();
		//			_database = GetDb(_client);
		//		}

		//		return _database;
		//	}
		//}


		private IMongoDatabase GetDb(MongoClient mongoClient)
		{
			try
			{
				//var dbClient = GetClient();

				Debug.WriteLineIf(_useDetailedDebug, $"DbProvider: About to call GetDb for {_databaseName}.");

				var mongoDatabase = mongoClient.GetDatabase(_databaseName);
				
				Debug.WriteLineIf(_useDetailedDebug, $"DbProvider: Completed call GetDb for {_databaseName}.");

				return mongoDatabase;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Got exception: {ex} during call to GetDb.");
				throw;
			}
		}

		private IMongoDatabase GetDb()
		{
			try
			{
				var dbClient = GetClient();

				Debug.WriteLineIf(_useDetailedDebug, $"DbProvider: About to call GetDb for {_databaseName}.");

				var mongoDatabase = dbClient.GetDatabase(_databaseName);

				Debug.WriteLineIf(_useDetailedDebug, $"DbProvider: Completed call GetDb for {_databaseName}.");

				return mongoDatabase;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Got exception: {ex} during call to GetDb.");
				throw;
			}
		}

		private MongoClient GetClient(TimeSpan? connectTimeout = null)
		{
			Debug.WriteLineIf(_useDetailedDebug, "DbProvider: About to call GetClient.");
			try
			{
				var settings = new MongoClientSettings
				{
					Server = new MongoServerAddress(_server, _port),
					//IPv6 = true,
				};

				if (connectTimeout != null)
				{
					settings.ConnectTimeout = connectTimeout.Value;
				}

				var dbClient = new MongoClient(settings);

				Debug.WriteLineIf(_useDetailedDebug, "DbProvider: Completed call GetClient.");

				return dbClient;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Got exception: {ex} during call to GetClient.");
				throw;
			}
		}

		#region Tests

		public bool TestConnection(string databaseName, TimeSpan? connectTimeout = null)
		{
			try
			{
				var dbClient = GetClient(connectTimeout);

				//var task = dbClient.ListDatabaseNamesAsync();
				//task.Wait();
				//var e = task.Result.ToEnumerable();

				//if (!e.Any())
				//{
				//	return false;
				//}

				//task = dbClient.ListDatabaseNamesAsync();
				//e = task.Result.ToEnumerable();

				//var result = e.FirstOrDefault(x => x.Equals(databaseName)) != null;

				var list = dbClient.ListDatabaseNames().ToEnumerable();

				var result = list.FirstOrDefault(x => x.Equals(databaseName)) != null;

				return result;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Got exception: {ex} during call to TestConnection.");
				return false;
			}
		}


		//private void TestIt()
		//{
		//	try
		//	{
		//		var dbClient = GetClient();

		//		var mongoDatabase = dbClient.GetDatabase(_databaseName);

		//		//var x = new mongodb.GridFSBucket(db);

		//		//var x = new MongoDB.Driver.gr

		//		//return mongoDatabase;
		//	}
		//	catch (Exception ex)
		//	{
		//		Debug.WriteLine($"Got exception: {ex} during call to GetDb.");
		//		throw;
		//	}
		//}

		#endregion
	}
}
