using System;
using System.Text.Json.Serialization;

namespace MFile
{
    public class MapInfoWithColorMapForExport
    {
        [JsonPropertyName("mapInfo")]
        public MapInfo MapInfo;

        [JsonPropertyName("colorMap")]
        public ColorMapForExport ColorMapForExport;

        [JsonPropertyName("version")]
        public double Version;

        private MapInfoWithColorMapForExport()
        {
            MapInfo = null;
            ColorMapForExport = null;
            Version = -1;
        }

        public MapInfoWithColorMapForExport(MapInfo mapInfo, ColorMapForExport colorMapForExport, double version)
        {
            MapInfo = mapInfo ?? throw new ArgumentNullException(nameof(mapInfo));
            ColorMapForExport = colorMapForExport ?? throw new ArgumentNullException(nameof(colorMapForExport));
            Version = version;
        }
    }
}
