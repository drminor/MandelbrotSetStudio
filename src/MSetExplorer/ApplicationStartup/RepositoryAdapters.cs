using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string server, int port, bool createCollections)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(server, port);

			// MapSection Repository Adapter
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(server, port);

			// SharedColorBandSet Repository Adapter
			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(server, port);

			if (createCollections)
			{
				ProjectAdapter.CreateCollections();
				MapSectionAdapter.CreateCollections();
				SharedColorBandSetAdapter.CreateCollections();
			}
		}

		public IProjectAdapter ProjectAdapter { get; init; }

		public IMapSectionAdapter MapSectionAdapter { get; init; }

		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }

	}
}
