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

}
