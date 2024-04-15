using System;

namespace MSS.Types
{
	public class ReservedColorBand : ICloneable
	{
		#region Constructor

		public ReservedColorBand() : this(ColorBandColor.White, ColorBandBlendStyle.Next, ColorBandBlendMethod.Rgb, ColorBandColor.Black)
		{ }

		public ReservedColorBand(string startCssColor, ColorBandBlendStyle blendStyle, ColorBandBlendMethod blendMethod, string endCssColor)
			: this(new ColorBandColor(startCssColor), blendStyle, blendMethod, new ColorBandColor(endCssColor))
		{ }

		public ReservedColorBand(ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandBlendMethod blendMethod, ColorBandColor endColor)
		{
			StartColor = startColor;
			BlendStyle = blendStyle;
			BlendMethod = blendMethod;
			EndColor = endColor;
		}

		#endregion

		#region Public Properties

		public ColorBandColor StartColor { get; set; }
		public ColorBandBlendStyle BlendStyle { get; set; }
		public ColorBandBlendMethod BlendMethod { get; set; }
		public ColorBandColor EndColor { get; set; }

		#endregion

		#region Public Methods

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ReservedColorBand Clone()
		{
			var result = new ReservedColorBand(StartColor, BlendStyle, BlendMethod, EndColor);

			return result;
		}

		#endregion

		public override string? ToString()
		{
			return $"Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}/{BlendMethod}, End: {EndColor.GetCssColor()}";
		}

	}
}
