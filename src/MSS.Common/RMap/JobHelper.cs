using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Numerics;

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

		public static Subdivision CreateSubdivision(SizeInt blockSize, RRectangle coords)
		{
			var position = coords.LeftBot;

			// Round canvasSize up to the nearest power of 2. (1280 --> 2048) 2048 = 2^11
			// SampleSize is then 1/canvasSize. (2^-11)
			var samplePointDelta = new RSize(1, 1, -11);

			// TODO: adjust coords to be expanded in proportion to the amount the canvasSize has been expanded.

			var result = new Subdivision(ObjectId.GenerateNewId(), position, samplePointDelta, blockSize);

			return result;
		}

		#region OLD STUFF -- for main program 

		public static Job ZoomIn(Job job)
		{
			var rRectangleZoomed = GetMiddle4(job.MSetInfo.Coords);

			// TODO: search for an existing SubDivision and use it.
			var subdivision = CreateSubdivision(job.Subdivision.BlockSize, job.MSetInfo.Coords);

			var result = new Job(
				id: ObjectId.GenerateNewId(),
				parentJob: job,
				project: job.Project,
				subdivision: subdivision,
				label: null,
				new MSetInfo(rRectangleZoomed, job.MSetInfo.MapCalcSettings, job.MSetInfo.ColorMapEntries, job.MSetInfo.HighColorCss),
				canvasSizeInBlocks: new SizeInt(6,6),
				canvasBlockOffset: new PointInt(-4, -3),
				canvasControlOffset: new PointDbl()
				);

			return result;
		}

		/// <summary>
		/// Divide the given rectangle into 16 squares and then return the coordinates of the "middle" 4 squares.
		/// </summary>
		/// <param name="rRectangle"></param>
		/// <returns>A new RRectangle</returns>
		public static RRectangle GetMiddle4(RRectangle rRectangle)
		{
			//DIAG -- double x0n = GetVal(rRectangle.X1, rRectangle.Exponent);

			// Here are the current values for the rectangle's Width and Height numerators
			var curWidth = rRectangle.WidthNumerator;
			var curHeight = rRectangle.HeightNumerator;

			// Create a new rectangle with its exponent adjusted to support the new precision required in the numerators.
			RRectangle rectangleWithNewExp;

			// And calculate the amount to adjust the x and y coordinates
			BigInteger adjustmentX;
			BigInteger adjustmentY;

			// First see if both the width and height are even
			// If even, but not integer multiple of 4 then these halves will become quarters
			var halfOfXLen = BigInteger.DivRem(curWidth, 2, out var remainderX);
			var halfOfYLen = BigInteger.DivRem(curHeight, 2, out var remainderY);

			if (remainderX == 0 && remainderY == 0)
			{
				// Both are even, now let's try 4.
				var quarterOfXLen = BigInteger.DivRem(curWidth, 4, out remainderX);
				var quarterOfYLen = BigInteger.DivRem(curHeight, 4, out remainderY);

				if (remainderX == 0 && remainderY == 0)
				{
					// The exponent does not need to be changed, the quarter values are naturaly whole numbers
					rectangleWithNewExp = rRectangle;
					adjustmentX = quarterOfXLen;
					adjustmentY = quarterOfYLen;
				}
				else
				{
					// The exponent needs to be reduced by 1 (all values are halved)
					rectangleWithNewExp = rRectangle.ScaleB(-1);
					adjustmentX = halfOfXLen;
					adjustmentY = halfOfYLen;
				}
			}
			else
			{
				// The exponent needs to be reduced by 2 (all values are quartered)
				rectangleWithNewExp = rRectangle.ScaleB(-2);
				adjustmentX = curWidth;
				adjustmentY = curHeight;
			}

			//DIAG double x1n = GetVal(rebased.X1, rebased.Exponent);

			var result = new RRectangle(
				rectangleWithNewExp.X1 + adjustmentX,
				rectangleWithNewExp.X2 - adjustmentX,
				rectangleWithNewExp.Y1 + adjustmentY,
				rectangleWithNewExp.Y2 - adjustmentY,
				rectangleWithNewExp.Exponent
				);

			return result;
		}

		#endregion
	}
}
