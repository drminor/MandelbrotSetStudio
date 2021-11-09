using MSS.Types;
using MongoDB.Bson;
using System.Collections.Generic;

namespace MSS.Types.MSetDatabase
{
	// Record Declaration
	public record Job(
		ObjectId ProjectId,
		ObjectId? ParentJobId,
		TransformType? Operation,
		int OperationAmount,
		bool Saved,
		string? Label,
		BCoords Coords,
		int MaxInterations,
		int Threshold,
		int IterationsPerStep,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss,
		IList<MapSectionRef>? MapSectionRefs
		) : RecordBase()

	{
		private const string ROOT_JOB_LABEL = "Root";

		// Custom constructor to create the initial or "root" Job. 
		public Job(ObjectId projectId, bool saved, BCoords coords, int maxIterations, int threshold, int iterationsPerStep, IList<ColorMapEntry> colorMapEntries, string highColorCss)
			: this(
				  ProjectId: projectId,
				  ParentJobId: null,
				  Operation: null,
				  OperationAmount: 0,
				  Saved: saved,
				  Label: ROOT_JOB_LABEL,
				  Coords: coords,
				  MaxInterations: maxIterations,
				  Threshold: threshold,
				  IterationsPerStep: iterationsPerStep,
				  ColorMapEntries: colorMapEntries,
				  HighColorCss: highColorCss,
				  MapSectionRefs: null
				  )
		{ }
				  
	}

}
