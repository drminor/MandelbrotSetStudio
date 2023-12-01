using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapSectionHistogramProcessor
	{
		IHistogram Histogram { get; }
		bool ProcessingEnabled { get; set; }

		event EventHandler<HistogramUpdateType>? HistogramUpdated;
		event EventHandler<PercentageBand[]>? PercentageBandsUpdated;

		void AddWork(HistogramBlockRequest histogramWorkRequest);
		void Dispose();
		KeyValuePair<int, int>[] GetKeyValuePairsForBand(int previousCutoff, int cutoff, bool includeCatchAll);
		void LoadHistogram(IEnumerable<IHistogram> histograms);
		void Reset();
		void Reset(int newSize);
		void Stop(bool immediately);

		IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff);

	}


	public enum HistogramUpdateType
	{
		BlockAdded,
		BlockRemoved,
		Clear,
		Refresh
	}
}