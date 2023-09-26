using MSS.Types.MSet;
using System.Threading;

namespace MSS.Common
{
	public interface IMEngineClient
	{
		int ClientNumber { get; }

		string EndPointAddress { get; }

		// True if running on the same machine as the Explorer program.
		bool IsLocal { get; }

		MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct);

		//bool CancelGeneration(MapSectionRequest mapSectionRequest, CancellationToken ct);
	}
}