using MEngineDataContracts;
using MSS.Types;
using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public interface IMapSectionProvider
	{
		Task<MapSectionResponse> GenerateMapSectionAsync(Subdivision subdivision, PointInt blockPosition, MapCalcSettings mapCalcSettings);
	}
}