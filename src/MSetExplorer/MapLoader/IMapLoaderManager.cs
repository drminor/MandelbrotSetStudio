using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	internal interface IMapLoaderManager
	{
		event EventHandler<MapSection> MapSectionReady;

		void Push(Job job);
		void Push(Job job, IList<MapSection> emptyMapSections);
		void StopCurrentJob();
	}
}