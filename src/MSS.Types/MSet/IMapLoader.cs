using System;

namespace MSS.Types.MSet
{
	public interface IMapLoader
	{
		bool IsCancelled { get; set; }
		bool IsComplete { get; }
		int JobNumber { get; }
		int MapLoaderJobNumber { get; set; }
		MathOpCounts? MathOpCounts { get; }
		DateTime? ProcessingEndTime { get; set; }
		DateTime? ProcessingStartTime { get; set; }
		int SectionsCancelled { get; set; }
		int SectionsFoundInRepo { get; set; }
		int SectionsGenerated { get; set; }
		int SectionsPending { get; }
		TimeSpan TotalExecutionTime { get; set; }
		int TotalNumberOfSectionsRequested { get; set; }

		event EventHandler? JobHasCompleted;
		event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;

		void Cancel();
		void MarkJobAsComplete();
		bool Start(int sectionsRequested, int sectionsCancelled, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback);
		bool Start(int sectionsRequested, int sectionsFoundInRepo, int sectionsGenerated, int sectionsCancelled, Action<MapSection> mapSectionReadyCallback, Action<int, bool> mapViewUpdateCompleteCallback);
	}
}