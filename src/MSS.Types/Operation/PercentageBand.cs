﻿namespace MSS.Types
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

		//public PercentageBand() : this(0, 0, 0)
		//{ }

		public PercentageBand(int cutoff)
		{
			Cutoff = cutoff;
		}

		//public PercentageBand(int cutoff, long exactCount, long runningSum)
		//{
		//	Cutoff = cutoff;
		//	ExactCount = exactCount;
		//	RunningSum = runningSum;

		//	Count = exactCount;
		//}

		//public PercentageBand(int cutoff, long exactCount, long runningSum, long count, double percentage)
		//{
		//	Cutoff = cutoff;
		//	ExactCount = exactCount;
		//	RunningSum = runningSum;

		//	Count = count;
		//	Percentage = percentage;
		//}

		#endregion
	}
}
