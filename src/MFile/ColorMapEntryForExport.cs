using System;
using System.Text.Json.Serialization;

namespace MFile
{
    public class ColorMapEntryForExport
    {
        [JsonPropertyName("cutOff")]
        public int CutOff;

        [JsonPropertyName("cssColor")]
        public string CssColor;

        [JsonPropertyName("startCssColor")]
        public string StartCssColor;

        [JsonPropertyName("blendStyle")]
        public ColorMapBlendStyle BlendStyle;

        [JsonPropertyName("endCssColor")]
        public string EndCssColor;

        private ColorMapEntryForExport()
        {
            CutOff = 0;
            CssColor = null;
            StartCssColor = null;
            BlendStyle = ColorMapBlendStyle.None;
            EndCssColor = null;
        }

        public ColorMapEntryForExport(int cutOff, string cssColor)
        {
            CutOff = cutOff;
            StartCssColor = cssColor ?? throw new ArgumentNullException(nameof(cssColor));
            BlendStyle = ColorMapBlendStyle.None;
            EndCssColor = cssColor;
        }

        public ColorMapEntryForExport(int cutOff, string startCssColor, ColorMapBlendStyle blendStyle, string endCssColor)
        {
            CutOff = cutOff;
            StartCssColor = startCssColor ?? throw new ArgumentNullException(nameof(startCssColor));
            BlendStyle = blendStyle;
            EndCssColor = endCssColor ?? throw new ArgumentNullException(nameof(endCssColor));
        }

    }
}
