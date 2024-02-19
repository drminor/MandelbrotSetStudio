namespace MSS.Types
{
	public class CutoffBand
	{
		public int Cutoff { get; set; }
		public double Percentage { get; set; }

		public double RunningPercentage { get; set; }
		public double TargetCount { get; set; }

		public double ActualPercentage { get; set; }
		public double ActualCount { get; set; }

		public double PreviousCount { get; set; }
		public double NextCount { get; set; }

		#region Constructor

		public CutoffBand(int cutoff)
		{
			Cutoff = cutoff;
		}

		public CutoffBand(int cutoff, double percentage)
		{
			Cutoff = cutoff;
			Percentage = percentage;
		}

		#endregion
	}

}
