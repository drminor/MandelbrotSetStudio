using MSS.Types;
using MongoDB.Bson;
using System.Collections.Generic;

namespace ProjectRepo
{
	public record Job(
		ObjectId ProjectId,
		ObjectId ParentJobId,
		TransformType? Operation,
		int OperationAmount,
		bool Saved,
		string Label,
		int Zoom,
		int CoordValuePrecision,
		Coords Coords,
		int MaxInterations,
		int Threshold,
		int IterationsPerStep,
		IList<ColorMapEntry> ColorMapEntries,
		string HighColorCss,
		IList<MapSectionRef> MapSectionRefs
		) : RecordBase()

	{
		public Job(ObjectId projectId, bool saved, Coords coords, int maxIterations, int threshold, int iterationsPerStep, IList<ColorMapEntry> colorMapEntries, string highColorCss)
			: this(
				  ProjectId: projectId,
				  ParentJobId: ObjectId.Empty,
				  Operation: null,
				  OperationAmount: 0,
				  Saved: saved,
				  Label: null,
				  Zoom: 0,
				  CoordValuePrecision: 0,
				  Coords: coords,
				  MaxInterations: maxIterations,
				  Threshold: threshold,
				  IterationsPerStep: iterationsPerStep,
				  ColorMapEntries: colorMapEntries,
				  HighColorCss: highColorCss,
				  MapSectionRefs: null)
		{ }
				  
	}

}
