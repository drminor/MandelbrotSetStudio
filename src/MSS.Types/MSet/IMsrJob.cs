using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Types
{
	internal interface IMsrJob
	{
		int JobNumber { get; }

		List<MapSectionRequest>? MapSectionRequests { get; }

		DateTime? ProcessingStartTime { get; set; }
		DateTime? ProcessingEndTime { get; set;  }
		TimeSpan ElaspedTime { get; set; }
		TimeSpan TotalExecutionTime { get; set; }

		MathOpCounts? MathOpCounts { get; }

		int NumSectionsRequested { get; }
		int NumSectionsSubmitted { get; }
		int NumSectionsCompleted { get; }

		event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;
		event EventHandler? JobHasCompleted;

		int GetNumberOfRequestsPendingSubmittal();
		int GetNumberOfRequestsPendingGeneration();

		void Cancel();
		void MarkJobAsComplete();
		

		//Task Task { get; init; }

		//Task Start(IList<MapSectionRequest> mapSectionRequests);
	}
}