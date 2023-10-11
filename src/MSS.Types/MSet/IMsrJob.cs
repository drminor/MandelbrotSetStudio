using MSS.Types.MSet;
using System;

namespace MSS.Types
{
	public interface IMsrJob
	{
		int JobNumber { get; }

		//List<MapSectionRequest>? MapSectionRequests { get; }

		event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;
		event EventHandler? JobHasCompleted;

		JobType JobType { get; }
		string JobId { get; }
		OwnerType OwnerType { get; }
		Subdivision Subdivision { get; }
		string OriginalSourceSubdivisionId { get; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		VectorLong JobBlockOffset { get; }

		int Precision { get; set; }
		int LimbCount { get; set; }

		SizeInt BlockSize { get; }
		RSize SamplePointDelta { get; }
		MapCalcSettings MapCalcSettings { get; }
		bool CrossesYZero { get; }

		bool IsCancelled { get; set; }

		DateTime? ProcessingStartTime { get; set; }
		DateTime? ProcessingEndTime { get; set;  }
		TimeSpan ElaspedTime { get; }
		TimeSpan TotalExecutionTime { get; set; }

		MathOpCounts? MathOpCounts { get; }

		int TotalNumberOfSectionsRequested { get; set; }
		int SectionsFoundInRepo { get; set;  }
		int SectionsGenerated { get; set; }
		int SectionsCancelled { get; set; }

		bool Start(int sectionsRequested, Action<MapSection> callback);

		bool Start(int sectionsRequested, int sectionsFoundInRepo, int sectionsGenerated, int sectionsCancelled, Action<MapSection> callback);
		
		void Cancel();
		void MarkJobAsComplete();
		

		//Task Task { get; init; }

		//Task Start(IList<MapSectionRequest> mapSectionRequests);
	}
}