namespace MSetExplorer.ColorBandSetHistogram.Support
{
	public class HPlotSeriesData
	{
		private static HPlotSeriesData _zeroSingleton = new HPlotSeriesData(0);

		public static HPlotSeriesData Zero => _zeroSingleton;

		public HPlotSeriesData(int length)
		{
			DataX = new double[length];
			DataY = new double[length];
		}

		public HPlotSeriesData(double[] dataX, double[] dataY)
		{
			DataX = dataX;
			DataY = dataY;
		}

		public double[] DataX { get; init; }
		public double[] DataY { get; init; }

		public bool IsZero()
		{
			return DataX.Length == 0;
		}

	}
}
