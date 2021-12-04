using MEngineDataContracts;
using MSS.Types;
using MSS.Types.Screen;

namespace MSS.Common
{
	public interface IMapSectionRepo
	{
		MapSection GetMapSection(string subdivisionId, SizeInt blockPosition);
		MapSection GetMapSection(string mapSectionId);

		void SaveMapSection(MapSectionResponse mapSectionResponse);
	}
}
