using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Types.MSetDatabase;

namespace ProjectRepo
{
	public class ProjectReaderWriter
	{
		private const string COLLECTION_NAME = "Projects";

		private readonly DbProvider _dbProvider;

		public ProjectReaderWriter(DbProvider dbProvider)
		{
			_dbProvider = dbProvider;
		}

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

		private IMongoCollection<Project> Collection
		{
			get
			{
				IMongoDatabase db = _dbProvider.Database;
				IMongoCollection<Project> projCollection = db.GetCollection<Project>(COLLECTION_NAME);

				return projCollection;
			}
		}

		#region unused

		//public ObjectId GetProjectId(string name)
		//{
		//	var filter = Builders<BsonDocument>.Filter.Eq("Name", name);
		//	var bsonRecord = Document.Find(filter).FirstOrDefault();

		//	return GetRecordId(bsonRecord);
		//}

		//private IMongoCollection<BsonDocument> Document
		//{
		//	get
		//	{
		//		IMongoDatabase db = _dbProvider.Database;
		//		var document = db.GetCollection<BsonDocument>(COLLECTION_NAME);

		//		return document;
		//	}
		//}

		//private ObjectId GetRecordId(BsonDocument bsonElements)
		//{
		//	ObjectId result = bsonElements == null ? ObjectId.Empty : GetObjectId(bsonElements.GetValue("_id"));
		//	return result;
		//}

		//private ObjectId GetObjectId(BsonValue bsonValue)
		//{
		//	ObjectId result = bsonValue == null ? ObjectId.Empty : bsonValue.AsObjectId;
		//	return result;
		//}

		#endregion
	}
}
