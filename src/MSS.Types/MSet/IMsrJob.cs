using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Types
{
	public interface IMsrJob
	{
		int JobNumber { get; }

		List<MapSectionRequest>? MapSectionRequests { get; }

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

		int SectionsRequested { get; }
		int SectionsSubmitted { get; }
		int SectionsCompleted { get; }



		int GetNumberOfRequestsPendingSubmittal();
		int GetNumberOfRequestsPendingGeneration();


		bool Start(List<MapSectionRequest> mapSectionRequests, Action<MapSection> callback, int numberOfsectionsSubmitted);
		//bool UpdateReqPendingCount(int amount);
		void Cancel();
		void MarkJobAsComplete();
		

		//Task Task { get; init; }

		//Task Start(IList<MapSectionRequest> mapSectionRequests);
	}
}