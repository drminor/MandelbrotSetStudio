
namespace MSS.Types
{
	public interface IColorBand
	{
		int CutOff { get; set; }
		ColorBandColor StartColor { get; set; }
		ColorBandBlendStyle BlendStyle { get; set; }
		ColorBandColor EndColor { get; set; }

		int PreviousCutOff { get; set; }
		ColorBandColor ActualEndColor { get; set; }
		double Percentage { get; set; }

		int BucketWidth { get; }
		//string BlendStyleAsString { get; }

		IColorBand Clone();

		void UpdateWithNeighbors(IColorBand? predecessor, IColorBand? successor);
	}

}