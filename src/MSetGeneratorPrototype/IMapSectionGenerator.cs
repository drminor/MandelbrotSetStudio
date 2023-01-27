using MEngineDataContracts;

namespace MSetGeneratorPrototype
{
	public interface IMapSectionGenerator
	{
		MapSectionServiceResponse GenerateMapSection(MapSectionServiceRequest mapSectionRequest);
	}
}