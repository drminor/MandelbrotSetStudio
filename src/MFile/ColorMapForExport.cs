using System;
using System.Text.Json.Serialization;

namespace MFile
{
    public class ColorMapForExport
    {
        [JsonPropertyName("ranges")]
        public ColorMapEntryForExport[] Ranges;

        [JsonPropertyName("highColorCss")]
        public string HighColorCss;

        [JsonPropertyName("version")]
        public double Version;

        private ColorMapForExport()
        {
            Ranges = Array.Empty<ColorMapEntryForExport>();
            HighColorCss = null;
            Version = -1;
        }

        public ColorMapForExport(ColorMapEntryForExport[] ranges, string highColorCss, double version)
        {
            Ranges = ranges ?? throw new ArgumentNullException(nameof(ranges));
            HighColorCss = highColorCss ?? throw new ArgumentNullException(nameof(highColorCss));
            Version = version;
        }
    }
}
