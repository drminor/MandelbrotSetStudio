﻿using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;

namespace MSetExplorer
{
	public class RepositoryAdapters
	{
		public RepositoryAdapters(string server, int port)
		{
			// Project Repository Adapter
			ProjectAdapter = MSetRepoHelper.GetProjectAdapter(server, port);
			ProjectAdapter.CreateCollections();

			// MapSection Repository Adapter
			MapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(server, port);
			MapSectionAdapter.CreateCollections();

			// SharedColorBandSet Repository Adapter
			SharedColorBandSetAdapter = MSetRepoHelper.GetSharedColorBandSetAdapter(server, port);
			SharedColorBandSetAdapter.CreateCollections();
		}

		public IProjectAdapter ProjectAdapter { get; init; }

		public IMapSectionAdapter MapSectionAdapter { get; init; }

		public SharedColorBandSetAdapter SharedColorBandSetAdapter { get; init; }

	}
}
