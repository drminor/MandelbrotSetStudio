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
		private static SizeInt BLOCK_SIZE = new SizeInt(128);

		private SamplePointBuilder? _samplePointBuilder;

		[Fact]
		public void Test1()
		{
			var ROW_NUMBER = 45;

			var limbCount = 2;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);
			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest(iterationState, apfixedPointFormat, mapCalcSettings);
		}

		[Fact]
		public void Test2()
		{
			var ROW_NUMBER = 45;

			var blockSize = RMapConstants.BLOCK_SIZE;
			var limbCount = 2;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinates(new BigVector(2, 2), new PointInt(2, 2), new RPoint(1, 1, -2), new RSize(1, 1, -8), apfixedPointFormat);

			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest2(iterationState, apfixedPointFormat, mapCalcSettings);

			// Just for diagnostics
			var counts = new int[blockSize.NumberOfCells];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

			iterationState.SetRowNumber(iterationState.RowCount); //Closeout the Interation State.

			var rowSum = counts.Sum();
			var firstTen = string.Join("; ", counts.Take(10));
			Debug.WriteLine($"The first 10 counts: {firstTen}; Sum of counts for Row {ROW_NUMBER}: {rowSum}.");
		}

		[Fact]
		public void Test3() // Call Test 3 for RowNumber = 45
		{
			var ROW_NUMBER = 45;

			var limbCount = 1;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinatesSample1(apfixedPointFormat);

			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest3(iterationState, apfixedPointFormat, mapCalcSettings);

			// Just for diagnostics
			var counts = new int[BLOCK_SIZE.NumberOfCells];
			iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

			iterationState.SetRowNumber(iterationState.RowCount); //Closeout the Interation State.

			var rowSum = counts.Sum();
			var firstTen = string.Join("; ", counts.Take(10));
			Debug.WriteLine($"The first 10 counts: {firstTen}; Sum of counts for Row {ROW_NUMBER}: {rowSum}.");
		}

		[Fact]
		public void Test3_TenRows() // Call Test 3 For RowNumbers 35 to 44.
		{
			var limbCount = 1;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinatesSample1(apfixedPointFormat);

			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);

			for (int rn = 35; rn < 45; rn++)
			{
				iterationState.SetRowNumber(rn);

				var mSetRowClient = new HpMSetRowClient();
				mSetRowClient.BaseSimdTest3(iterationState, apfixedPointFormat, mapCalcSettings);

				// Just for diagnostics
				var counts = new int[iterationState.MapSectionVectors.ValuesPerRow];
				iterationState.MapSectionVectors.FillCountsRow(iterationState.RowNumber!.Value, counts);

				var rowSum = counts.Sum();
				var firstTen = string.Join("; ", counts.Take(10));
				Debug.WriteLine($"Row #: {rn}: The first 10 counts: {firstTen}; Sum {rn}: {rowSum}.");

			}

			iterationState.SetRowNumber(iterationState.RowCount); //Closeout the Interation State.

		}

		[Fact]
		public void Test4() // Call Test4 once for RowNumber = 0
		{
			var ROW_NUMBER = 0;

			var limbCount = 1;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinatesSample1(apfixedPointFormat);

			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);
			iterationState.SetRowNumber(ROW_NUMBER);

			var mSetRowClient = new HpMSetRowClient();
			mSetRowClient.BaseSimdTest4(iterationState, apfixedPointFormat, mapCalcSettings);

			// Have the IterationState update its source.
			iterationState.SetRowNumber(iterationState.RowCount); //Closeout the Interation State.


			// Load into an array of integers the integer counts for RowNumber = 0
			// From the IterationState's source.
			var diagCounts = new int[iterationState.MapSectionVectors.ValuesPerRow];
			iterationState.MapSectionVectors.FillCountsRow(0, diagCounts);

			// Measure
			var rowSum = diagCounts.Sum();

			var firstTen = string.Join("; ", diagCounts.Take(20));
			Debug.WriteLine($"The first 10 counts: {firstTen}; Sum of counts for Row {ROW_NUMBER}: {rowSum}.");
		}

		[Fact]
		public void RoundTrip_Counts()
		{
			var limbCount = 1;
			var targetIterations = 20;
			var threshold = 4;
			var apfixedPointFormat = new ApFixedPointFormat(limbCount);

			var mapCalcSettings = new MapCalcSettings(targetIterations: targetIterations, threshold: threshold, requestsPerJob: 4);
			var iteratorCoords = GetCoordinatesSample1(apfixedPointFormat);

			var iterationState = BuildIterationState(limbCount, mapCalcSettings, iteratorCoords);

			iterationState.SetRowNumber(0);

			var mSetRowClient = new HpMSetRowClient();
			var success = mSetRowClient.RoundTripCounts(iterationState, apfixedPointFormat, mapCalcSettings);
			
			Assert.True(success);

			iterationState.SetRowNumber(iterationState.RowCount); //Closeout the Interation State.

		}

		#region Support Methods

		private IIterationState BuildIterationState(int limbCount, MapCalcSettings mapCalcSettings, IteratorCoords iteratorCoords)
		{
			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(BLOCK_SIZE));
			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(iteratorCoords);

			var mapSectionVectors = new MapSectionVectors(BLOCK_SIZE);
			var mapSectionZVectors = new MapSectionZVectors(BLOCK_SIZE, limbCount);

			var targetIterationsVector = Vector256.Create(mapCalcSettings.TargetIterations);
			var result = new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, increasingIterations: false, targetIterationsVector);

			//result.SetRowNumber(0);

			return result;
		}

		private IteratorCoords GetCoordinatesSample1(ApFixedPointFormat apFixedPointFormat)
		{
			var blockPosition = new BigVector(-1, 1);
			var screenPosition = new PointInt(1, 1);
			var mapPosition = new RPoint(-128, 128, -11);
			var samplePointDelta = new RSize(1, 1, -11);
			var iteratorCoords = GetCoordinates(blockPosition, screenPosition, mapPosition, samplePointDelta, apFixedPointFormat);

			return iteratorCoords;

		}

		/*

			var blockPosition = new BigVector(-1, 1);
			var screenPosition = new PointInt(1, 1);
			var mapPosition = new RPoint(-128, 128, -11);
			var samplePointDelta = new RSize(1, 1, -11);
			var iteratorCoords = GetCoordinates(blockPosition, screenPosition, mapPosition, samplePointDelta, apfixedPointFormat);

		*/


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