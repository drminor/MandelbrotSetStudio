using MSS.Types;
using MongoDB.Bson;
using System.Collections.Generic;

namespace ProjectRepo.Entities
{
	// Record Declaration
	public record JobRecord(
		ObjectId ProjectId,
		ObjectId? ParentJobId,
		TransformType? Operation,
		int OperationAmount,
		string? Label,
		SizeInt CanvasSize,
		RRectangleRecord CoordsRecord,
		int MaxInterations,
		int Threshold,
		int IterationsPerStep,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss,

		IList<MapSectionPtr>? MapSectionPtrs
		) : RecordBase()

	{
		private const string ROOT_JOB_LABEL = "Root";

		// Custom constructor to create the initial or "root" Job. 
		public JobRecord(ObjectId projectId, SizeInt canvasSize, RRectangleRecord coords, int maxIterations, int threshold, int iterationsPerStep, IList<ColorMapEntry> colorMapEntries, string highColorCss)
			: this(
				  ProjectId: projectId,
				  ParentJobId: null,
				  Operation: null,
				  OperationAmount: 0,
				  Label: ROOT_JOB_LABEL,
				  CanvasSize: canvasSize,
				  CoordsRecord: coords,
				  MaxInterations: maxIterations,
				  Threshold: threshold,
				  IterationsPerStep: iterationsPerStep,
				  ColorMapEntries: colorMapEntries,
				  HighColorCss: highColorCss,
				  MapSectionPtrs: null
				  )
		{ }
				  
	}

}
