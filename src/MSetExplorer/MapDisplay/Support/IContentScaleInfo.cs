namespace MSetExplorer
{
	public interface IContentScaleInfo
	{
		ZoomSlider? ZoomSliderOwner { get; set; }

		bool CanZoom { get; set;}

		double ContentScale { get; }
		double MinContentScale { get; }
		double MaxContentScale { get; }

		void SetContentScale(double contentScale);
	}

}
