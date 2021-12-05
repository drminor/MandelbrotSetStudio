using MEngineDataContracts;
using MSS.Types;
using System.Threading.Tasks;

namespace MSS.Common
{
	public interface IMapSectionRepo
	{
		Task<MapSectionResponse?> GetMapSectionAsync(string subdivisionId, PointInt blockPosition);
		Task<MapSectionResponse?> GetMapSectionAsync(string mapSectionId);

		void SaveMapSection(MapSectionResponse mapSectionResponse);
	}
}
