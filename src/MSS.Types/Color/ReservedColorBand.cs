using System;

namespace MSS.Types
{
	public class ReservedColorBand : ICloneable
	{
		#region Constructor

		public ReservedColorBand() : this(ColorBandColor.White, ColorBandBlendStyle.None, ColorBandColor.Black)
		{ }

		public ReservedColorBand(string startCssColor, ColorBandBlendStyle blendStyle, string endCssColor)
			: this(new ColorBandColor(startCssColor), blendStyle, new ColorBandColor(endCssColor))
		{ }

		public ReservedColorBand(ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor)
		{
			StartColor = startColor;
			BlendStyle = blendStyle;
			EndColor = endColor;
		}

		#endregion

		#region Public Properties

		public ColorBandColor StartColor { get; set; }
		public ColorBandBlendStyle BlendStyle { get; set; }
		public ColorBandColor EndColor { get; set; }

		#endregion

		#region Public Methods

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ReservedColorBand Clone()
		{
			var result = new ReservedColorBand(StartColor, BlendStyle, EndColor);

			return result;
		}

		#endregion

		public override string? ToString()
		{
			return $"Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}";
		}

	}
}
