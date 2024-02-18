namespace MSS.Types
{
	public class PercentageBand
	{
		public int Cutoff { get; set; }
		public long Count { get; set; }
		public double Percentage { get; set; }

		public long ExactCount { get; set; }
		public long RunningSum { get; set; }
		//public long RunningSumReverse { get; set; }

		#region Constructor

		public PercentageBand(int cutoff)
		{
			Cutoff = cutoff;
		}

		public PercentageBand(int cutoff, double percentage)
		{
			Cutoff = cutoff;
			Percentage = percentage;
		}

		#endregion
	}

	public class CutoffBand
	{
		public int Cutoff { get; set; }
		public double Percentage { get; set; }

		public double RunningPercentage { get; set; }
		public double TargetCount { get; set; }

		public double ActualPercentage { get; set; }
		public double ActualCount { get; set; }

		public double CountAtPrev { get; set; }
		public double CountAtSucc { get; set; }

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
