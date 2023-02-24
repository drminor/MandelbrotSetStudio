using MSetGeneratorPrototype;
using MSetRowGeneratorClient;
using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Runtime.Intrinsics;

namespace MSetRowGeneratorClientTest
{
	public class BaseSimdTest
	{
		[Fact]
		public void Test1()
		{
			var blockSize = RMapConstants.BLOCK_SIZE;
			var limbCount = 2;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 20, requestsPerJob: 4);
			var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

			var samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));
			var (samplePointsX, samplePointsY) = samplePointBuilder.BuildSamplePoints(iteratorCoords);
			var iterationState = BuildIterationState(samplePointsX, samplePointsY, blockSize, limbCount, mapCalcSettings);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest(iterationState, apfixedPointFormat, mapCalcSettings);
		}

		#region Support Methods

		private IIterationState BuildIterationState(FP31Val[] samplePointsX, FP31Val[] samplePointsY, SizeInt blockSize, int limbCount, MapCalcSettings mapCalcSettings)
		{
			var mapSectionVectors = new MapSectionVectors(blockSize);
			var mapSectionZVectors = new MapSectionZVectors(blockSize, limbCount);

			var xs = new FP31Val[0];
			var ys = new FP31Val[0];

			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);
			var result = new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, increasingIterations: false, targetIterationsVector);

			return result;
		}

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var msr = mapSectionRequest;
			var result = GetCoordinates(msr.BlockPosition, msr.ScreenPosition, msr.MapPosition, msr.SamplePointDelta, apFixedPointFormat);
			return result;
		}

		private IteratorCoords GetCoordinates(BigVector blockPosition, PointInt screenPosition, RPoint mapPosition, RSize samplePointDelta, ApFixedPointFormat apFixedPointFormat)
		{
			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			return new IteratorCoords(blockPosition, screenPosition, startingCx, startingCy, delta);
		}

		#endregion
	}
}