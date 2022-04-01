using MSS.Types;
using System;

namespace MSetExplorer
{
	internal class HistogramWorkRequest
	{
		public HistogramWorkRequestType RequestType { get; init; }
		public int[] CutOffs { get; init; }
		public IHistogram? Histogram { get; init; }
		public Action<ValueTuple<int, double>[]> WorkAction { get; init; }

		public HistogramWorkRequest(HistogramWorkRequestType requestType, int[] cutOffs, IHistogram? histogram, Action<ValueTuple<int, double>[]> workAction)
		{
			RequestType = requestType;
			CutOffs = cutOffs;
			Histogram = histogram;
			WorkAction = workAction ?? throw new ArgumentNullException(nameof(workAction));

			if (RequestType != HistogramWorkRequestType.BucketsUpdated && Histogram == null)
			{
				throw new ArgumentException("The Histogram cannot be null, unless the request type is 'Update-Buckets'.");
			}
		}

		public void RunWorkAction(ValueTuple<int, double>[] newPercentages)
		{
			WorkAction(newPercentages);
		}
	}

	internal enum HistogramWorkRequestType
	{
		Add,
		Remove,
		BucketsUpdated
	}

}
