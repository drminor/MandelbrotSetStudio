using MSS.Types;
using MSS.Types.MSetDatabase;
using System;

using MongoDB.Bson;
using System.Collections.Generic;

namespace MSS.Common
{
	public static class JobHelper
	{
		public static Job DoOperation(Job job, TransformType transformtype)
		{
			return transformtype switch
			{
				TransformType.In => ZoomIn(job),
				//case TransformType.Out:
				//	break;
				//case TransformType.Left:
				//	break;
				//case TransformType.Right:
				//	break;
				//case TransformType.Up:
				//	break;
				//case TransformType.Down:
				//	break;
				_ => throw new InvalidOperationException($"TransformType: {transformtype} is not recognized or is not supported."),
			};
		}

		public static Job ZoomIn(Job job)
		{
			Coords coords = job.Coords;
			IList<MapSectionRef> mapSectionRefs = null;

			Job result = new Job(job.ProjectId, job.Id, TransformType.In, 2, Saved:false, Label: null, 
				coords, 
				job.MaxInterations, job.Threshold, job.IterationsPerStep, job.ColorMapEntries, job.HighColorCss, 
				mapSectionRefs);

			return result;
		}

	}
}
