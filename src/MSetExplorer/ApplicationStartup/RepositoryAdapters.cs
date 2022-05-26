using MSetRepo;
using MSS.Common;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string dbConnectionString,  bool useMapSectionRepo)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(dbConnectionString);

			ProjectAdapter.CreateCollections();

			MapSectionAdapter = useMapSectionRepo ? MSetRepoHelper.GetMapSectionAdapter(dbConnectionString) : null;

			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(dbConnectionString);
			SharedColorBandSetAdapter.CreateCollections();
		}

		public ProjectAdapter ProjectAdapter { get; init; }

		public IMapSectionAdapter? MapSectionAdapter { get; init; }

		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }

	}
}
