using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	public static class BlockingCollectionHelper
	{
		public static void ReportQueueCount<T>(BlockingCollection<T> bc)
		{
			//Debug.WriteLine($"MapSectionHistogramProcessor dummy log output. ProcessingIsEnabled: {ProcessingEnabled}."); 

			if (bc.TryGetNonEnumeratedCount(out var hCnt))
			{
				Debug.WriteLine($"MapSectionHistogramProcessor. The WorkQueue has {hCnt} items.");
			}
			else
			{
				Debug.WriteLine("MapSectionHistogramProcessor. TryGetNonEnumeratedCount returned false.");
			}
		}
	}
}
