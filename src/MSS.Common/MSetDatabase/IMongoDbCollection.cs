using MongoDB.Driver;

namespace MSS.Common.MSetDatabase
{
	public interface IMongoDbCollection<T>
	{
		IMongoDatabase Database { get; }
		IMongoCollection<T> Collection { get; }
	}
}
