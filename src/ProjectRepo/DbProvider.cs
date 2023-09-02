using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

		public IMongoDatabase Database => _mongoDatabaseLazy.Value;

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

		private IMongoDatabase GetDb()
		{
			try
			{
				var dbClient = GetClient();

				var mongoDatabase = dbClient.GetDatabase(_databaseName);
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
			try
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
			catch (Exception ex)
			{
				Debug.WriteLine($"Got exception: {ex} during call to GetClient.");
				throw;
			}
		}
	}
}
