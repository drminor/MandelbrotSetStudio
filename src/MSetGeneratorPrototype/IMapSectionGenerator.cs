using MEngineDataContracts;

namespace MSetGeneratorPrototype
{
	public interface IMapSectionGenerator
	{
		MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest);
	}
}