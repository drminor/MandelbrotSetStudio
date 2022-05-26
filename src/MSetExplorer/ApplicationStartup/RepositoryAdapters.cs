using MSetRepo;
using MSS.Common;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string dbConnectionString, bool dropAllCollections, bool dropMapCollections, bool useMapSectionRepo)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(dbConnectionString);

			if (dropAllCollections)
			{
				ProjectAdapter.DropCollections();
			}
			else if (dropMapCollections)
			{
				ProjectAdapter.DropSubdivisionsAndMapSectionsCollections();
			}

			ProjectAdapter.CreateCollections();

			MapSectionAdapter = useMapSectionRepo ? MSetRepoHelper.GetMapSectionAdapter(dbConnectionString) : null;

			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(dbConnectionString);
			SharedColorBandSetAdapter.CreateCollections();
		}

		public ProjectAdapter ProjectAdapter { get; init; }

		public IMapSectionAdapter? MapSectionAdapter { get; init; }

		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }


		//private void DoSchemaUpdates()
		//{
		//	//_projectAdapter.AddColorBandSetIdToAllJobs();
		//	//_projectAdapter.AddIsPreferredChildToAllJobs();

		//	//var report = _projectAdapter.FixAllJobRels();
		//	//Debug.WriteLine(report);

		//	//var report1 = _projectAdapter.OpenAllJobs();
		//	//Debug.WriteLine($"Could not open these projects:\n {string.Join("; ", report1)}");

		//	//Debug.WriteLine("About to call DeleteUnusedColorBandSets.");
		//	//var report = _projectAdapter.DeleteUnusedColorBandSets();
		//	//Debug.WriteLine(report);

		//	//var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_CONN_STRING);
		//	//mapSectionAdapter.AddCreatedDateToAllMapSections();

		//	//var res = MessageBox.Show("FixAll complete, stop application?", "done", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
		//	//if (res == MessageBoxResult.Yes)
		//	//{
		//	//	Current.Shutdown();
		//	//	return;
		//	//}
		//}

	}
}
