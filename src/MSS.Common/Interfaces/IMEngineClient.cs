﻿using MSS.Types.MSet;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMEngineClient
	{
		string EndPointAddress { get; }

		// True if running on the same machine as the Explorer program.
		bool IsLocal { get; }

		Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest, CancellationToken ct);

		MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct);

	}
}