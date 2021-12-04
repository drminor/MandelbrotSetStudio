using MEngineClient;
using MSS.Common;
using System;

namespace MapSectionProviderLib
{
	public class MapSectionProvider
	{
		private readonly IMEngineClient _mEngineClient;
		private readonly IMapSectionRepo _mapSectionRepo;

		public MapSectionProvider(IMEngineClient mEngineClient, IMapSectionRepo mapSectionRepo)
		{
			_mEngineClient = mEngineClient;
			_mapSectionRepo = mapSectionRepo;
		}


	}
}
