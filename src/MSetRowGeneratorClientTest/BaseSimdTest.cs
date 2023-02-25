using MSetGeneratorPrototype;
using MSetRowGeneratorClient;
using MSS.Common;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace MSetRowGeneratorClientTest
{
	public class BaseSimdTest
	{
		private SamplePointBuilder? _samplePointBuilder;

		[Fact]
		public void Test1()
		{
			var ROW_NUMBER = 45;

			var blockSize = RMapConstants.BLOCK_SIZE;
			var limbCount = 2;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 20, requestsPerJob: 4);
			var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

			var iterationState = BuildIterationState(blockSize, limbCount, iteratorCoords, mapCalcSettings);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest(iterationState, apfixedPointFormat, mapCalcSettings);

			var counts = new int[blockSize.NumberOfCells];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

			var firstTen = string.Join("; ", counts.Take(10));
			Debug.WriteLine($"The first 10 counts: {firstTen}.");
		}

		[Fact]
		public void Test2()
		{
			var ROW_NUMBER = 45;

			var blockSize = RMapConstants.BLOCK_SIZE;
			var limbCount = 2;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 20, requestsPerJob: 4);
			var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

			var iterationState = BuildIterationState(blockSize, limbCount, iteratorCoords, mapCalcSettings);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest2(iterationState, apfixedPointFormat, mapCalcSettings);

			var counts = new int[blockSize.NumberOfCells];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

			var rowSum = counts.Sum();

			var firstTen = string.Join("; ", counts.Take(10));
			Debug.WriteLine($"The first 10 counts: {firstTen}; Sum of counts for Row {ROW_NUMBER}: {rowSum}.");
		}

		[Fact]
		public void Test3()
		{
			var ROW_NUMBER = 45;

			var blockSize = RMapConstants.BLOCK_SIZE;
			var limbCount = 1;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: 20, requestsPerJob: 4);

			var blockPosition = new BigVector(-1, 1);
			var screenPosition = new PointInt(1, 1);
			var mapPosition = new RPoint(-128, 128, -11);
			var samplePointDelta = new RSize(1, 1, -11);
			var iteratorCoords = GetCoordinates(blockPosition, screenPosition, mapPosition, samplePointDelta, apfixedPointFormat);

			var iterationState = BuildIterationState(blockSize, limbCount, iteratorCoords, mapCalcSettings);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest3(iterationState, apfixedPointFormat, mapCalcSettings);

			var counts = new int[blockSize.Width];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

			var rowSum = counts.Sum();

			var firstTen = string.Join("; ", counts.Take(10));
			Debug.WriteLine($"The first 10 counts: {firstTen}; Sum of counts for Row {ROW_NUMBER}: {rowSum}.");
		}

		#region Support Methods

		private IIterationState BuildIterationState(SizeInt blockSize, int limbCount, IteratorCoords iteratorCoords, MapCalcSettings mapCalcSettings)
		{
			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));
			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(iteratorCoords);

			var mapSectionVectors = new MapSectionVectors(blockSize);
			var mapSectionZVectors = new MapSectionZVectors(blockSize, limbCount);

			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);
			var result = new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, increasingIterations: false, targetIterationsVector);

			//result.SetRowNumber(0);

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