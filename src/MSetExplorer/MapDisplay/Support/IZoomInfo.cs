namespace MSetExplorer
{
	public interface IZoomInfo
	{
		ZoomSlider? ZoomOwner { get; set; }

		bool CanZoom { get; set;}

		double Scale { get; }
		double MinScale { get; }
		double MaxScale { get; }

		void SetScale(double scale);
	}

}
