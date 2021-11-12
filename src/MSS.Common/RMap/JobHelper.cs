using MongoDB.Bson;
using MSS.Common.DataTransferObjects;
using MSS.Common.MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.MSetRepo;
using System;
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
			var rRectangleZoomed = RMapHelper.Zoom(job.Coords);

			// TODO: search for an existing SubDivision and use it.
			var subdivisionId = ObjectId.GenerateNewId();

			Job result = new Job(ObjectId.GenerateNewId(), label: null, job.ProjectId, job.Id, job.CanvasSize, 
				rRectangleZoomed, subdivisionId,
				job.MaxInterations, job.Threshold, job.IterationsPerStep, job.ColorMapEntries, job.HighColorCss);

			return result;
		}

	}
}
