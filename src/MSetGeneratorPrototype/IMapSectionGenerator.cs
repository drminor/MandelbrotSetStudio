using MSS.Types.MSet;

namespace MSetGeneratorPrototype
{
	public interface IMapSectionGenerator
	{
		MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest);
	}
}