using MSS.Types.MSet;
using System.Threading;

namespace MSS.Common
{
	public interface IMapSectionGenerator
	{
		MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct);
	}
}