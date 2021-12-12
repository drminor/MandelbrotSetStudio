using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;

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
			var rRectangleZoomed = RMapHelper.Zoom(job.MSetInfo.Coords);

			// TODO: search for an existing SubDivision and use it.
			var subdivision = CreateSubdivision(job.MSetInfo.CanvasSize, job.Subdivision.BlockSize, job.MSetInfo.Coords);

			var result = new Job(
				id: ObjectId.GenerateNewId(),
				parentJob: job, 
				project: job.Project,
				subdivision: subdivision,
				label: null,
				new MSetInfo(job.MSetInfo.CanvasSize, rRectangleZoomed, job.MSetInfo.MapCalcSettings, job.MSetInfo.ColorMapEntries, job.MSetInfo.HighColorCss),
				canvasOffset: new PointDbl()
				);

			return result;
		}

		public static Subdivision CreateSubdivision(SizeInt canvasSize, SizeInt blockSize, RRectangle coords)
		{
			var position = coords.LeftBot;

			// Round canvasSize up to the nearest power of 2. (1280 --> 2048) 2048 = 2^11
			// SampleSize is then 1/canvasSize. (2^-11)
			var samplePointDelta = new RSize(1, 1, -11);

			// TODO: adjust coords to be expanded in proportion to the amount the canvasSize has been expanded.

			var result = new Subdivision(ObjectId.GenerateNewId(), position, blockSize, samplePointDelta);

			return result;
		}

		///// <summary>
		///// 
		///// </summary>
		///// <param name="canvasExtent"></param>
		///// <param name="coordExtent"></param>
		///// <param name="coordExp"></param>
		///// <returns>Extent in X and SampleWidth in Y</returns>
		//private static RPoint GetExtentAndSampleWidth(int canvasExtent, BigInteger coordExtent, int coordExp)
		//{
		//	RPoint result = new RPoint();

		//	return result;
		//}

		//private static BigInteger GetExtent(int canvasExtent, BigInteger coordExtent, int coordExp)
		//{
		//	BigInteger result = new BigInteger();

		//	return result;
		//}



	}
}
