using MongoDB.Driver;

namespace MSS.Common.MSetRepo
{
	public interface IMongoDbCollection<T>
	{
		IMongoDatabase Database { get; }
		IMongoCollection<T> Collection { get; }

		void CreateCollection();
	}
}
