using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IMapSectionHistogramProcessor
	{
		IHistogram Histogram { get; }
		bool ProcessingEnabled { get; set; }
		long NumberOfSectionsProcessed { get; }

		event EventHandler<HistogramUpdateType>? HistogramUpdated;

		void AddWork(HistogramWorkRequest histogramWorkRequest);
		void Reset();
		void Reset(int newSize);
		void Stop(bool immediately);
		void Dispose();

		KeyValuePair<int, int>[] GetKeyValuePairsForBand(int previousCutoff, int cutoff, bool includeCatchAll);
		IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff);

		public double GetAverageMapSectionTargetIteration();
	}

	public enum HistogramUpdateType
	{
		BlockAdded,
		BlockRemoved,
		Clear,
		Refresh
	}
}