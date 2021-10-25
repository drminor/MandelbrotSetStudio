using System.Text.Json;
using System.Text.Json.Serialization;
namespace MFile
{
    public class MapInfoWithColorMap_FixMe
    {
        public string Name { get; init; }
        public MapInfo MapInfo { get; init; }
        public ColorMapForExport ColorMap { get; init; }

        public MapInfoWithColorMap_FixMe(string name, MapInfo mapInfo, ColorMapForExport colorMap)
        {
            Name = name;
            MapInfo = mapInfo;
            ColorMap = colorMap;
        }
    }
}
