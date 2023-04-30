namespace MSetExplorer
{
	public interface IContentScaleInfo
	{
		ZoomSlider? ZoomSliderOwner { get; set; }

		bool CanZoom { get; set;}

		double Scale { get; }
		double MinScale { get; }
		double MaxScale { get; }

		void SetScale(double scale);
	}

}
