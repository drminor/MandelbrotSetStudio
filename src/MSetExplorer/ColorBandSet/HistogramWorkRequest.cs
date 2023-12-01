using MSS.Types;
using System;

namespace MSetExplorer
{
	public class HistogramWorkRequest
	{
		public HistogramWorkRequestType RequestType { get; init; }
		public int[] Cutoffs { get; init; }
		public IHistogram? Histogram { get; init; }
		public Action<PercentageBand[]> WorkAction { get; init; }

		public HistogramWorkRequest(HistogramWorkRequestType requestType, int[] cutoffs, IHistogram? histogram, Action<PercentageBand[]> workAction)
		{
			RequestType = requestType;
			Cutoffs = cutoffs;
			Histogram = histogram;
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));

			if (RequestType != HistogramWorkRequestType.Refresh && Histogram == null)
			{
				throw new ArgumentException("The Histogram cannot be null, unless the request type is 'Update-Buckets'.");
			}
		}

		public void RunWorkAction(PercentageBand[] newPercentages)
		{
			WorkAction(newPercentages);
		}
	}

	public enum HistogramWorkRequestType
	{
		Add,
		Remove,
		Refresh
	}

	public class HistogramBlockRequest
	{
		public HistogramBlockRequestType RequestType { get; init; }
		public IHistogram Histogram { get; init; }

		public HistogramBlockRequest(HistogramBlockRequestType requestType, IHistogram histogram)
		{
			RequestType = requestType;
			Histogram = histogram;

		}
	}

	public enum HistogramBlockRequestType
	{
		Add,
		Remove
	}

}
