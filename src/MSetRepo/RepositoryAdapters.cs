using MSS.Common;

namespace MSetRepo
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string server, int port)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(server, port);

			// MapSection Repository Adapter
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(server, port);

			// SharedColorBandSet Repository Adapter
			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(server, port);
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

		public void WarmUp()
		{
			ProjectAdapter.WarmUp();
		}

		#endregion

	}
}
