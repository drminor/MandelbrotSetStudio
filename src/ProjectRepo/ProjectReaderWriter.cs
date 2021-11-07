using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.MSetDatabase;

namespace ProjectRepo
{
	public class ProjectReaderWriter : MongoDbCollectionBase<Project>
	{
		private const string COLLECTION_NAME = "Projects";

		public ProjectReaderWriter(DbProvider dbProvider) : base(dbProvider, COLLECTION_NAME)
		{ }

		public Project Get(ObjectId projectId)
		{
			var filter = Builders<Project>.Filter.Eq("_id", projectId);
			var project = Collection.Find(filter).FirstOrDefault();

			return project;
		}

		public Project Get(string name)
		{
			var filter = Builders<Project>.Filter.Eq("Name", name);
			var project = Collection.Find(filter).FirstOrDefault();

			return project;
		}

		public ObjectId GetProjectId(string name)
		{
			var project = Get(name);

			return project?.Id ?? ObjectId.Empty;
		}

		public ObjectId Insert(Project project)
		{
			Collection.InsertOne(project);
			return project.Id;
		}

		public long? Delete(ObjectId projectId)
		{
			var filter = Builders<Project>.Filter.Eq("_id", projectId);
			var project = Collection.Find(filter).FirstOrDefault();


			var deleteResult = Collection.DeleteOne(filter);

			return GetReturnCount(deleteResult);
		}
	}
}
