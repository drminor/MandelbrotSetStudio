using MongoDB.Driver;

namespace MSS.Types
{
	public interface IMongoDbCollection<T>
	{
		IMongoDatabase Database { get; }
		IMongoCollection<T> Collection { get; }

		bool CreateCollection();
	}
}
