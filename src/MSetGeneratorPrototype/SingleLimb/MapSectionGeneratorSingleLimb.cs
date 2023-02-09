using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Numerics;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorSingleLimb : IMapSectionGenerator
	{
		#region Private Properties

		private readonly IteratorSingleLimb _iterator;

		#endregion

		#region Constructor

		public MapSectionGeneratorSingleLimb()
		{
			_iterator = new IteratorSingleLimb();
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			var skipPositiveBlocks = false;
			var skipLowDetailBlocks = false;

			//var blockPos = mapSectionRequest.BlockPosition;
			var mapPosition = mapSectionRequest.Position;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;
			var screenPosition = mapSectionRequest.ScreenPosition;

			MapSectionResponse result;

			if (ShouldSkipThisSection(skipPositiveBlocks, skipLowDetailBlocks, screenPosition, mapPosition))
			{
				result = new MapSectionResponse(mapSectionRequest);
			}
			else
			{
				//ReportCoords(coords, _fp31VectorsMath.LimbCount, mapSectionRequest.Precision);

				var stride = (byte)mapSectionRequest.BlockSize.Width;
				var samplePointOffsets = SamplePointBuilder.BuildSamplePointOffsets(samplePointDelta, stride);
				var samplePointsX = SamplePointBuilder.BuildSamplePoints(mapPosition.X, samplePointOffsets);
				var samplePointsY = SamplePointBuilder.BuildSamplePoints(mapPosition.Y, samplePointOffsets);
				//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

				var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest);

				var mapCalcSettings = mapSectionRequest.MapCalcSettings;
				_iterator.Threshold = (uint)mapCalcSettings.Threshold;
				_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;
				_iterator.MathOpCounts.Reset();
				var targetIterations = mapCalcSettings.TargetIterations;
				var iterationState = new IterationStateSingleLimb(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, targetIterations);

				var allRowsHaveEscaped = GenerateMapSection(_iterator, iterationState, ct);
				//Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

				if (ct.IsCancellationRequested)
				{
					Debug.WriteLine($"The block: {screenPosition} is cancelled.");
				}
				else
				{
					if (allRowsHaveEscaped)
					{
						Debug.WriteLine($"The entire block: {screenPosition} is done.");
					}
				}

				result = new MapSectionResponse(mapSectionRequest, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors, ct.IsCancellationRequested);


				//result.MathOpCounts = _iterator.MathOpCounts;
			}

			return result;
		}

		// Generate MapSection
		private bool GenerateMapSection(IteratorSingleLimb iterator, IterationStateSingleLimb iterationState, CancellationToken ct)
		{
			var allRowsHaveEscaped = true;

			var rowNumber = iterationState.GetNextRowNumber();
			while(rowNumber != null && !ct.IsCancellationRequested)
			{ 
				var allRowSamplesHaveEscaped = true;

				for (var idxPtr = 0; idxPtr < iterationState.InPlayList.Length; idxPtr++)
				{
					var idx = iterationState.InPlayList[idxPtr];
					var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, ref iterationState);

					if (!allSamplesHaveEscaped)
					{
						allRowSamplesHaveEscaped = false;
					}
				}

				iterationState.RowHasEscaped[rowNumber.Value] = allRowSamplesHaveEscaped;

				if (!allRowSamplesHaveEscaped)
				{
					allRowsHaveEscaped = false;
				}

				//_iterator.MathOpCounts.RollUpNumberOfUnusedCalcs(itState.GetUnusedCalcs());

				rowNumber = iterationState.GetNextRowNumber();
			}

			return allRowsHaveEscaped;
		}

		#endregion

		#region Generate One Vector

		private bool GenerateMapCol(int idx, IteratorSingleLimb iterator, ref IterationStateSingleLimb iterationState)
		{
			//var hasEscaped = iterationState.HasEscapedFlagsRowV[idx];
			var hasEscaped = false; 
			var count = iterationState.CountsRowV[idx];

			//var doneFlag = iterationState.DoneFlags[idx];
			//var unusedCalcs = iterationState.UnusedCalcs[idx];

			var cr = iterationState.CrsRow[idx];
			var ci = iterationState.CisRow[idx];

			var zr = iterationState.ZrsRowV[idx];
			var zi = iterationState.ZisRowV[idx];

			iterator.Reset();

			var done = false;
			while (!done)
			{
				hasEscaped = iterator.Iterate(cr, ci, zr, zi);
				count++;

				done = hasEscaped || count >= iterationState.TargetIterations;
			}

			iterationState.HasEscapedFlagsRowV[idx] = hasEscaped;
			iterationState.CountsRowV[idx] = count;

			//iterationState.UpdateZrLimbSet(idx, _zrs);
			//iterationState.UpdateZrLimbSet(idx, _zis);

			var result = false;
			return result;
		}

		#endregion

		#region Support Methods

		private (MapSectionVectors, MapSectionZVectors) GetMapSectionVectors(MapSectionRequest mapSectionRequest)
		{
			var mapSectionVectors = mapSectionRequest.MapSectionVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
			mapSectionRequest.MapSectionVectors = null;

			var mapSectionZVectors = mapSectionRequest.MapSectionZVectors ?? throw new ArgumentNullException("The MapSectionVectors is null.");
			mapSectionRequest.MapSectionZVectors = null;

			return (mapSectionVectors, mapSectionZVectors);
		}

		private bool[] CompressHasEscapedFlags(int[] hasEscapedFlags)
		{
			bool[] result;

			if (!hasEscapedFlags.Any(x => !(x == 0)))
			{
				// All have escaped
				result = new bool[] { true };
			}
			else if (!hasEscapedFlags.Any(x => x > 0))
			{
				// none have escaped
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = hasEscapedFlags.Select(x => x == 0 ? false : true).ToArray();
			}

			return result;
		}

		private void ReportCoords(IteratorCoords coords, int limbCount, int precision)
		{
			//var s1 = coords.GetStartingCxStringVal();
			//var s2 = coords.GetStartingCyStringVal();
			var s3 = coords.GetDeltaStringVal();

			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. Limbs: {apFixedPointFormat.LimbCount}.");

			Debug.WriteLine($"Starting : {coords.ScreenPos}: {coords.BlockPos}, delta: {s3}, #oflimbs: {limbCount}. MapSecReq Precision: {precision}.");
		}

		private void ReportSamplePoints(IteratorCoords coords, FP31Val[] samplePointOffsets, FP31Val[] samplePointsX, FP31Val[] samplePointsY)
		{
			var bx = coords.ScreenPos.X;
			var by = coords.ScreenPos.Y;
			if (bx == 0 && by == 0 || bx == 3 && by == 4)
			{
				ReportSamplePoints(samplePointOffsets);
				ReportSamplePoints(samplePointsX);
			}
		}

		private void ReportSamplePoints(FP31Val[] fP31Vals)
		{
			foreach (var value in fP31Vals)
			{
				Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay("x", value.Mantissa)} {value.Exponent}.");
			}
		}

		private bool ShouldSkipThisSection(bool skipPositiveBlocks, bool skipLowDetailBlocks, PointInt screenPosition, RPoint mapPosition)
		{
			// Skip positive 'blocks'

			if (skipPositiveBlocks)
			{
				var xSign = mapPosition.X.Value > 0;
				var ySign = mapPosition.Y.Value > 0;

				return xSign && ySign;
			}

			// Move directly to a block where at least one sample point reaches the iteration target.
			else if (skipLowDetailBlocks && (BigInteger.Abs(screenPosition.Y) > 1 || BigInteger.Abs(screenPosition.X) > 3))
			{
				return true;
			}

			return false;
		}

		#endregion
	}
}
