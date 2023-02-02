using MSetRepo;
using MSS.Common;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string server, int port/*, bool createCollections, bool dropMapSectionCollections*/)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(server, port);

			// MapSection Repository Adapter
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(server, port);

			// SharedColorBandSet Repository Adapter
			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(server, port);

			//if (dropMapSectionCollections)
			//{
			//	MapSectionAdapter.DropJobMapSecAndMapSecCollections();
			//}


			//if (createCollections)
			//{
			//	ProjectAdapter.CreateCollections();
			//	MapSectionAdapter.CreateCollections();
			//	SharedColorBandSetAdapter.CreateCollections();
			//}
			//else
			//{
			//	ProjectAdapter.WarmUp();
			//}
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
