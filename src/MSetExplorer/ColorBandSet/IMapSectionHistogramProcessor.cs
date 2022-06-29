using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapSectionHistogramProcessor
	{
		IHistogram Histogram { get; }
		bool ProcessingEnabled { get; set; }

		event EventHandler? HistogramUpdated;
		event EventHandler<PercentageBand[]>? PercentageBandsUpdated;

		void AddWork(HistogramWorkRequest histogramWorkRequest);
		void Dispose();
		KeyValuePair<int, int>[] GetKeyValuePairsForBand(int previousCutoff, int cutoff);
		void LoadHistogram(IEnumerable<IHistogram> histograms);
		void Reset();
		void Reset(int newSize);
		void Stop(bool immediately);
	}
}