using System;

namespace MFile
{
    public record ColorMapEntry(int Cutoff, string StartCssColor, int BlendStyle, string EndCssColor)
    {
        public const int BLEND_STYLE_NONE = 0;
        public const int BLEND_STYLE_NEXT = 1;
        public const int BLEND_STYLE_END = 2;

        //public ColorMapEntry(int cutOff, string cssColor) : this(cutOff, cssColor ?? throw new ArgumentNullException(nameof(cssColor)), BLEND_STYLE_NONE, cssColor)
        //{ }

    }
}
