using MongoDB.Driver;

namespace MSS.Common
{
	public interface IMongoDbCollection<T>
	{
		IMongoDatabase Database { get; }
		IMongoCollection<T> Collection { get; }

		bool CreateCollection();
	}
}
