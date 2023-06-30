using MSS.Common;
using MSS.Common.DataTransferObjects;
using ProjectRepo;
using System.Diagnostics;

namespace MSetRepo
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string server, int port, string databaseName)
		{
			var dbProvider = new DbProvider(server, port, databaseName);

			var dtoMapper = new DtoMapper();
			var mSetRecordMapper = new MSetRecordMapper(dtoMapper);

			ProjectAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper);

			MapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper);

			SharedColorBandSetAdapter = new SharedColorBandSetAdapter(dbProvider, mSetRecordMapper);
		}

		#region Public Properties

		public IProjectAdapter ProjectAdapter { get; init; }
		public IMapSectionAdapter MapSectionAdapter { get; init; }
		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }

		#endregion

		#region Public Methods

		public void CreateCollections()
		{
			ProjectAdapter.CreateCollections();
			MapSectionAdapter.CreateCollections();
			SharedColorBandSetAdapter.CreateCollections();
		}


		public void CreateCollectionIndexes()
		{
			MapSectionAdapter.CreateIndexes();
		}

		public void WarmUp()
		{
			var result = ProjectAdapter.ProjectCollectionIsEmpty();

			if (result)
			{
				Debug.WriteLine("WARNING: The Project Collection is empty.");
			}
		}

		#endregion

		//#region Private Methods

		//private IProjectAdapter CreateProjectAdapter(string server, int port)
		//{
		//	var dbProvider = new DbProvider(server, port);
		//	var mSetRecordMapper = CreateMSetRecordMapper();
		//	var projectAdapter = new ProjectAdapter(dbProvider, mSetRecordMapper);

		//	return projectAdapter;
		//}

		//private IMapSectionAdapter CreateMapSectionAdapter(string server, int port)
		//{
		//	var dbProvider = new DbProvider(server, port);
		//	var mSetRecordMapper = CreateMSetRecordMapper();
		//	var mapSectionAdapter = new MapSectionAdapter(dbProvider, mSetRecordMapper);

		//	return mapSectionAdapter;
		//}

		//private SharedColorBandSetAdapter CreateGetSharedColorBandSetAdapter(string server, int port)
		//{
		//	var dbProvider = new DbProvider(server, port);
		//	var mSetRecordMapper = CreateMSetRecordMapper();
		//	var sharedColorBandSetAdapter = new SharedColorBandSetAdapter(dbProvider, mSetRecordMapper);

		//	return sharedColorBandSetAdapter;
		//}

		//private MSetRecordMapper CreateMSetRecordMapper()
		//{
		//	var dtoMapper = new DtoMapper();
		//	var result = new MSetRecordMapper(dtoMapper);

		//	return result;
		//}


		//#endregion
	}
}
