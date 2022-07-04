using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string dbConnectionString)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(dbConnectionString);

			ProjectAdapter.CreateCollections();

			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(dbConnectionString);

			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(dbConnectionString);
			SharedColorBandSetAdapter.CreateCollections();
		}

		public IProjectAdapter ProjectAdapter { get; init; }

		public IMapSectionAdapter MapSectionAdapter { get; init; }

		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }

	}
}
