﻿using Microsoft.VisualBasic;
using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Common.MSetGenerator;
using MSS.Types;
using MSS.Types.APValues;
using MSS.Types.MSet;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MSetGeneratorPrototype
{
	public class MapSectionGeneratorDepthFirst : IMapSectionGenerator
	{
		#region Private Properties

		private readonly double _logOf2;

		private SamplePointBuilder _samplePointBuilder;

		private IFP31VecMath _fp31VecMath;
		private IIterator _iterator;

		private readonly bool _useCImplementation;
		//private HpMSetRowClient? _hpMSetRowClient;

		private Vector256<uint>[] _crs;
		private Vector256<uint>[] _cis;
		private Vector256<uint>[] _zrs;
		private Vector256<uint>[] _zis;

		private Vector256<uint>[] _resultZrs;
		private Vector256<uint>[] _resultZis;

		private Vector256<uint>[] _resultZrsForEscV;
		private Vector256<uint>[] _resultZisForEscV;


		private readonly Vector256<int> _justOne;

		private const bool USE_DET_DEBUG = false;

		#endregion

		#region Constructor

		public MapSectionGeneratorDepthFirst(int limbCount, SizeInt blockSize, bool useCImplementation)
		{
			_logOf2 = Math.Log(2);
			_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSize));

			_fp31VecMath = _samplePointBuilder.GetVecMath(limbCount);
			_iterator = new IteratorDepthFirst(_fp31VecMath);
			_useCImplementation = useCImplementation;
			//_hpMSetRowClient = null; // new HpMSetRowClient();

			_crs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_cis = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_zrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_zis = FP31VecMathHelper.CreateNewLimbSet(limbCount);

			_resultZrs = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_resultZis = FP31VecMathHelper.CreateNewLimbSet(limbCount);

			_resultZrsForEscV = FP31VecMathHelper.CreateNewLimbSet(limbCount);
			_resultZisForEscV = FP31VecMathHelper.CreateNewLimbSet(limbCount);

			_justOne = Vector256.Create(1);
		}

		#endregion

		#region Generate MapSection

		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest, CancellationToken ct)
		{
			//var (currentLimbCount, limbCountForThisRequest) = GetMathAndAllocateTempVars(mapSectionRequest);
			GetMathAndAllocateTempVars(mapSectionRequest);

			//TestRoundTripRValue(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);

			var coords = GetCoordinates(mapSectionRequest, _fp31VecMath.ApFixedPointFormat);
			//ReportLimbCountUpdate(coords, currentLimbCount, limbCountForThisRequest, mapSectionRequest.Precision);

			var (mapSectionVectors, mapSectionZVectors) = GetMapSectionVectors(mapSectionRequest);

			//if (ShouldSkipThisSection(skipPositiveBlocks: false, skipLowDetailBlocks: false, coords))
			//	return new MapSectionResponse(mapSectionRequest, requestCompleted: false, allRowsHaveEscaped: false, mapSectionVectors, mapSectionZVectors);

			//ReportCoords(coords, _fp31VecMath.LimbCount, mapSectionRequest.Precision);
			var stopwatch = Stopwatch.StartNew();

			var (samplePointsX, samplePointsY) = _samplePointBuilder.BuildSamplePoints(coords);
			//var (samplePointsX, samplePointsY, samplePointOffsets) = _samplePointBuilder.BuildSamplePointsDiag(coords);
			//ReportSamplePoints(coords, samplePointOffsets, samplePointsX, samplePointsY);

			var mapCalcSettings = mapSectionRequest.MapCalcSettings;
			_iterator.Threshold = (uint)mapCalcSettings.Threshold;
			_iterator.ThresholdForEscVel = RMapConstants.DEFAULT_NORMALIZED_THRESHOLD; // TODO: Add the ThresholdForEscVel as a property of the MapCalcSettings class.
			_iterator.IncreasingIterations = mapSectionRequest.IncreasingIterations;
			_iterator.MathOpCounts.Reset();

			IIterationState iterationState = mapSectionZVectors == null
				? new IterationStateDepthFirstNoZ(samplePointsX, samplePointsY, mapSectionVectors, mapCalcSettings.TargetIterations)
				: new IterationStateDepthFirst(samplePointsX, samplePointsY, mapSectionVectors, mapSectionZVectors, mapSectionRequest.IncreasingIterations, mapCalcSettings.TargetIterations);

			var completed = GeneratorOrUpdateRows(_iterator, iterationState/*, _hpMSetRowClient*/, ct, out var allRowsHaveEscaped);
			stopwatch.Stop();

			var result = new MapSectionResponse(mapSectionRequest, completed, allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors);
			mapSectionRequest.GenerationDuration = stopwatch.Elapsed;
			
			UpdateResponseWithMops(result, _iterator, iterationState);
			//ReportResults(coords, mapSectionRequest, result, ct);

			return result;
		}

		private bool GeneratorOrUpdateRows(IIterator iterator, IIterationState iterationState/*, HpMSetRowClient? hpMSetRowClient*/, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			bool completed;

			if (_useCImplementation)
			{
				//completed = HighPerfGenerateMapSectionRows(iterator, iterationState, hpMSetRowClient, ct, out allRowsHaveEscaped);
				throw new NotImplementedException("HighPerfGenerateMapSectionRows is not currently supported.");
			}
			else
			{
				if (!iterationState.HaveZValues)
				{
					completed = GenerateMapSectionRowsNoZ(iterator, iterationState, ct, out allRowsHaveEscaped);
				}
				else
				{
					if (_iterator.IncreasingIterations)
					{
						completed = UpdateMapSectionRows(iterator, iterationState, ct, out allRowsHaveEscaped);
					}
					else
					{
						completed = GenerateMapSectionRows(iterator, iterationState, ct, out allRowsHaveEscaped);
					}
				}
			}


			return completed;
		}

		//private bool HighPerfGenerateMapSectionRows(IIterator iterator, IIterationState iterationState, HpMSetRowClient hpMSetRowClient, CancellationToken ct, out bool allRowsHaveEscaped)
		//{
		//	allRowsHaveEscaped = false;

		//	if (ct.IsCancellationRequested)
		//	{
		//		return false;
		//	}

		//	allRowsHaveEscaped = true;

		//	for (var rowNumber = 0; rowNumber < iterationState.RowCount; rowNumber++)
		//	{
		//		iterationState.SetRowNumber(rowNumber);

		//		// TODO: Include the MapCalcSettings in the iterationState.
		//		var mapCalcSettings = new MapCalcSettings(iterationState.TargetIterationsVector.GetElement(0));

		//		var allRowSamplesHaveEscaped = hpMSetRowClient.GenerateMapSectionRow(iterationState, _fp31VecMath.ApFixedPointFormat, mapCalcSettings, ct);
		//		//iterationState.RowHasEscaped[rowNumber] = allRowSamplesHaveEscaped;

		//		if (!allRowSamplesHaveEscaped)
		//		{
		//			allRowsHaveEscaped = false;
		//		}

		//		if (ct.IsCancellationRequested)
		//		{
		//			allRowsHaveEscaped = false;
		//			return false;
		//		}
		//	}

		//	// 'Close out' the iterationState
		//	iterationState.SetRowNumber(iterationState.RowCount);

		//	Debug.Assert(iterationState.RowNumber == null, $"The iterationState should have a null RowNumber, but instead has {iterationState.RowNumber}.");

		//	return true;
		//}

		private bool GenerateMapSectionRows(IIterator iterator, IIterationState iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			bool completed = true;

			if (ct.IsCancellationRequested)
			{
				allRowsHaveEscaped = false;
				return false;
			}

			allRowsHaveEscaped = true;

			for(var rowNumber = 0; rowNumber < iterationState.RowCount; rowNumber++)
			{
				iterationState.SetRowNumber(rowNumber);

				var allRowSamplesHaveEscaped = true;
				for (var idx = 0; idx < iterationState.VectorsPerRow; idx++)
				{
					var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, iterationState);

					if (!allSamplesHaveEscaped)
					{
						allRowSamplesHaveEscaped = false;
					}
				}

				iterationState.RowHasEscaped[rowNumber] = allRowSamplesHaveEscaped;

				if (ct.IsCancellationRequested)
				{
					allRowsHaveEscaped = false;
					completed = false;
					break;
				}

				if (!allRowSamplesHaveEscaped)
				{
					allRowsHaveEscaped = false;
				}
			}

			// 'Close out' the iterationState
			iterationState.SetRowNumber(iterationState.RowCount);
			Debug.Assert(iterationState.RowNumber == null, $"The iterationState should have a null RowNumber, but instead has {iterationState.RowNumber}.");

			return completed;
		}

		private bool UpdateMapSectionRows(IIterator iterator, IIterationState iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			bool completed = true;

			if (ct.IsCancellationRequested)
			{
				allRowsHaveEscaped = false;
				return false;
			}

			allRowsHaveEscaped = true;

			var rowNumber = iterationState.GetNextRowNumber();
			while (rowNumber != null)
			{
				Debug.Assert(iterationState.InPlayList.Length > 0, "GetNextRowNumber returned a non-null value, however the InPlayList is empty.");

				var allRowSamplesHaveEscaped = true;

				for (var idxPtr = 0; idxPtr < iterationState.InPlayList.Length; idxPtr++)
				{
					var idx = iterationState.InPlayList[idxPtr];
					var allSamplesHaveEscaped = UpdateMapCol(idx, iterator, ref iterationState);

					if (!allSamplesHaveEscaped)
					{
						allRowSamplesHaveEscaped = false;
					}
				}

				iterationState.RowHasEscaped[rowNumber.Value] = allRowSamplesHaveEscaped;

				if (ct.IsCancellationRequested)
				{
					allRowsHaveEscaped = false;
					completed = false;

					// 'Close out' the iterationState
					iterationState.SetRowNumber(iterationState.RowCount);
					break;
				}

				if (!allRowSamplesHaveEscaped)
				{
					allRowsHaveEscaped = false;
				}

				rowNumber = iterationState.GetNextRowNumber();
			}

			return completed;
		}

		private bool GenerateMapSectionRowsNoZ(IIterator iterator, IIterationState iterationState, CancellationToken ct, out bool allRowsHaveEscaped)
		{
			allRowsHaveEscaped = false;
			bool completed = true;

			if (ct.IsCancellationRequested)
			{
				Debug.WriteLine("GenerateMapSectionRowsNoZ is cancelled before processing the first row.");
				return false;
			}

			for (var rowNumber = 0; rowNumber < iterationState.RowCount; rowNumber++)
			{
				iterationState.SetRowNumber(rowNumber);

				for (var idx = 0; idx < iterationState.VectorsPerRow; idx++)
				{
					GenerateMapColNoZ(idx, iterator, iterationState);
				}

				if (ct.IsCancellationRequested)
				{
					Debug.WriteLine($"GenerateMapSectionRowsNoZ is cancelled after row:{rowNumber}.");
					completed = false;
					break;
				}
			}

			// 'Close out' the iterationState
			iterationState.SetRowNumber(iterationState.RowCount);
			Debug.Assert(iterationState.RowNumber == null, $"The iterationState should have a null RowNumber, but instead has {iterationState.RowNumber}.");

			return completed;
		}

		#endregion

		#region Generate One Vector

		private bool GenerateMapCol(int idx, IIterator iterator, IIterationState iterationState)
		{
			var hasEscapedFlagsV = Vector256<int>.Zero;
			var hasEscapedFlagsV2 = Vector256<int>.Zero;

			var doneFlagsV = Vector256<int>.Zero;
			var doneFlagsV2 = Vector256<int>.Zero;

			var countsV = Vector256<int>.Zero;
			var resultCountsV = countsV;
			var resultCountsV2 = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			FP31VecMathHelper.ClearLimbSet(_zrs);
			FP31VecMathHelper.ClearLimbSet(_zis);
			FP31VecMathHelper.ClearLimbSet(_resultZrs);
			FP31VecMathHelper.ClearLimbSet(_resultZis);
			FP31VecMathHelper.ClearLimbSet(_resultZrsForEscV);
			FP31VecMathHelper.ClearLimbSet(_resultZisForEscV);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;
			Vector256<int> escapedFlags2Vec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);
			countsV = Avx2.Add(countsV, _justOne);

			// Compare the new Counts with the TargetIterations
			var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

			//var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			// Update the resultCountsV2
			var compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

			if (compositeIsDone != -1)
			{
				// if not all of the items have escaped the large bailout value
				// then check to see if any items have just now reached the small bailout value.
				_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, _zrs, _zis, _resultZrs, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV);
			}

			var baseEscapedFlagsVec = escapedFlagsVec;
			var baseEscapedFlags2Vec = escapedFlags2Vec;

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);

				// Once escaped, always escaped
				escapedFlagsVec = Avx2.Or(baseEscapedFlagsVec, escapedFlagsVec);
				escapedFlags2Vec = Avx2.Or(baseEscapedFlags2Vec, escapedFlags2Vec);

				//compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

				countsV = Avx2.Add(countsV, _justOne);

				// Compare the new Counts with the TargetIterations
				targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

				// Update the resultCountsV2
				compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

				if (compositeIsDone != -1)
				{
					// if not all of the items have escaped the large bailout value
					// then check to see if any items have just now reached the small bailout value.
					_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, _zrs, _zis, _resultZrs, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV);
				}
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV2, iterationState.UsedCalcs, iterationState.UnusedCalcs);

			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = resultCountsV;

			iterationState.UpdateZrLimbSet(iterationState.RowNumber!.Value, idx, _resultZrs);
			iterationState.UpdateZiLimbSet(iterationState.RowNumber!.Value, idx, _resultZis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			if (compositeAllEscaped == -1)
			{
				for(var i = 0; i < 8; i++)
				{
					if (resultCountsV.GetElement(i) >= iterationState.TargetIterations)
					{
						Debug.WriteLine("Check All Escaped. It looks like some have reached the target iteration count.");
					}
				}
			}

			return compositeAllEscaped == -1;
		}

		private bool UpdateMapCol(int idx, IIterator iterator, ref IIterationState iterationState)
		{
			var hasEscapedFlagsV = Vector256<int>.Zero;
			var hasEscapedFlagsV2 = Vector256<int>.Zero;

			var doneFlagsV = Vector256<int>.Zero;
			var doneFlagsV2 = Vector256<int>.Zero;

			var countsV = Vector256<int>.Zero;
			var resultCountsV = countsV;
			var resultCountsV2 = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			iterationState.FillZrLimbSet(idx, _zrs);
			iterationState.FillZiLimbSet(idx, _zis);

			Array.Copy(_zrs, _resultZrs, _resultZrs.Length);
			Array.Copy(_zis, _resultZis, _resultZis.Length);

			Array.Copy(_zrs, _resultZrsForEscV, _resultZrsForEscV.Length);
			Array.Copy(_zis, _resultZisForEscV, _resultZisForEscV.Length);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;
			Vector256<int> escapedFlags2Vec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);
			countsV = Avx2.Add(countsV, _justOne);

			// Compare the new Counts with the TargetIterations
			var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

			//var compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

			// Update the resultCountsV2
			var compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

			if (compositeIsDone != -1)
			{
				// if not all of the items have escaped the large bailout value
				// then check to see if any items have just now reached the small bailout value.
				_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, _zrs, _zis, _resultZrs, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV);
			}

			var baseEscapedFlagsVec = escapedFlagsVec;
			var baseEscapedFlags2Vec = escapedFlags2Vec;

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);

				// Once escaped, always escaped
				escapedFlagsVec = Avx2.Or(baseEscapedFlagsVec, escapedFlagsVec);
				escapedFlags2Vec = Avx2.Or(baseEscapedFlags2Vec, escapedFlags2Vec);

				//compositeIsDone = UpdateCounts(escapedFlagsVec, ref countsV, ref resultCountsV, _zrs, _resultZrs, _zis, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV, iterationState.TargetIterationsVector);

				countsV = Avx2.Add(countsV, _justOne);

				// Compare the new Counts with the TargetIterations
				targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

				// Update the resultCountsV2
				compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

				if (compositeIsDone != -1)
				{
					// if not all of the items have escaped the large bailout value
					// then check to see if any items have just now reached the small bailout value.
					_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, _zrs, _zis, _resultZrs, _resultZis, ref hasEscapedFlagsV, ref doneFlagsV);
				}
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV2, iterationState.UsedCalcs, iterationState.UnusedCalcs);


			iterationState.HasEscapedFlagsRowV[idx] = hasEscapedFlagsV;
			iterationState.CountsRowV[idx] = resultCountsV;

			iterationState.UpdateZrLimbSet(iterationState.RowNumber!.Value, idx, _resultZrs);
			iterationState.UpdateZiLimbSet(iterationState.RowNumber!.Value, idx, _resultZis);

			var compositeAllEscaped = Avx2.MoveMask(hasEscapedFlagsV.AsByte());

			return compositeAllEscaped == -1;
		}

		private void GenerateMapColNoZ(int idx, IIterator iterator, IIterationState iterationState)
		{
			var hasEscapedFlagsV = Vector256<int>.Zero;
			var hasEscapedFlagsV2 = Vector256<int>.Zero;

			var doneFlagsV = Vector256<int>.Zero;
			var doneFlagsV2 = Vector256<int>.Zero;

			var countsV = Vector256<int>.Zero;
			var resultCountsV = countsV;
			var resultCountsV2 = countsV;

			iterationState.FillCrLimbSet(idx, _crs);
			_cis = iterationState.CiLimbSet;

			FP31VecMathHelper.ClearLimbSet(_zrs);
			FP31VecMathHelper.ClearLimbSet(_zis);

			Vector256<int> escapedFlagsVec = Vector256<int>.Zero;
			Vector256<int> escapedFlags2Vec = Vector256<int>.Zero;

			iterator.IterateFirstRound(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);
			countsV = Avx2.Add(countsV, _justOne);

			// Compare the new Counts with the TargetIterations
			var targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

			// Update the resultCountsV2
			var compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

			_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV);

			//if (compositeIsDone != -1)
			//{
			//	// if not all of the items have escaped the large bailout value
			//	// then check to see if any items have just now reached the small bailout value.
			//	_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV);
			//}

			var baseEscapedFlagsVec = escapedFlagsVec;
			var baseEscapedFlags2Vec = escapedFlags2Vec;

			while (compositeIsDone != -1)
			{
				iterator.Iterate(_crs, _cis, _zrs, _zis, ref escapedFlagsVec, ref escapedFlags2Vec);

				// Once escaped, always escaped
				escapedFlagsVec = Avx2.Or(baseEscapedFlagsVec, escapedFlagsVec);
				escapedFlags2Vec = Avx2.Or(baseEscapedFlags2Vec, escapedFlags2Vec);

				countsV = Avx2.Add(countsV, _justOne);

				// Compare the new Counts with the TargetIterations
				targetReachedCompVec = Avx2.CompareGreaterThan(countsV, iterationState.TargetIterationsVector);

				_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV);

				// Update the resultCountsV2
				compositeIsDone = SaveCountsForDoneItems(escapedFlags2Vec, targetReachedCompVec, countsV, ref resultCountsV2, _zrs, _zis, _resultZrsForEscV, _resultZisForEscV, ref hasEscapedFlagsV2, ref doneFlagsV2);

				//if (compositeIsDone != -1)
				//{
				//	// if not all of the items have escaped the large bailout value
				//	// then check to see if any items have just now reached the small bailout value.
				//	_ = SaveCountsForDoneItems(escapedFlagsVec, targetReachedCompVec, countsV, ref resultCountsV, ref hasEscapedFlagsV, ref doneFlagsV);
				//}
			}

			TallyUsedAndUnusedCalcs(idx, iterationState.CountsRowV[idx], countsV, resultCountsV2, iterationState.UsedCalcs, iterationState.UnusedCalcs);

			iterationState.CountsRowV[idx] = resultCountsV;

			var escVels = CalculateEscapeVelocities(_resultZrsForEscV, _resultZisForEscV, targetReachedCompVec);
			Array.Copy(escVels, 0, iterationState.EscapeVelocities, idx * Vector256<uint>.Count, escVels.Length);

			//return false;
		}

		private ushort[] CalculateEscapeVelocities(Vector256<uint>[] zRs, Vector256<uint>[] zIs, Vector256<int> targetReachedCompVec)
		{
			var ourCount = 0;
			var limbCount = _fp31VecMath.LimbCount;
			var lanes = Vector256<uint>.Count; 
			
			ushort[] escapeVelocities = new ushort[lanes];

			var sumOfSqrs = _iterator.GetSumOfSquares(zRs, zIs);

			for (var i = 0; i < lanes; i++)
			{
				var doneFlag = targetReachedCompVec.GetElement(i);
				if (doneFlag != -1)
				{
					var val = new uint[limbCount];

					for (var j = 0; j < limbCount; j++)
					{
						val[j] = sumOfSqrs[j].GetElement(i);
					}

					var rValue = FP31ValHelper.CreateRValue(sign: true, val, _fp31VecMath.ApFixedPointFormat.TargetExponent, RMapConstants.DEFAULT_PRECISION);
					var doubles = RValueHelper.ConvertToDoubles(rValue);

					// TODO: For higher LimbCount simply taking the high limb may not be sufficient.
					var logZn = Math.Log(doubles[0]) / 2; // Divide the Log by 2 instead of taking the Log of the Square Root.

					var logOfLogZn = Math.Log(logZn);
					var nu = logOfLogZn / _logOf2;
					nu /= 2;

					if (nu < 0 || nu > 1)
					{
						//Debug.WriteLine("WARNING: The EscapeVelocity is not in the range: 0..1");
						//throw new InvalidOperationException("The EscapeVelocity is not in the range: 0..2");
						ourCount++;
						nu = 0;
					}

					var d = 1 - nu;

					var e = (ushort)Math.Round(d * 10000);

					escapeVelocities[i] = e;
				}
				else
				{
					escapeVelocities[i] = 0;
				}
			}

			/*
				log_zn:= log(x * x + y * y) / 2

				nu:= log(log_zn / log(2)) / log(2)
				Rearranging the potential function.
				Dividing log_zn by log(2) instead of log(N = 1<<8)
				because we want the entire palette to range from the
				center to radius 2, NOT our bailout radius.
				iteration:= iteration + 1 - nu

				Z = Z * Z + C; iter_count++;    // a couple of extra iterations helps
				Z = Z * Z + C; iter_count++;    // decrease the size of the error term.
				float modulus = sqrt(ReZ * ReZ + ImZ * ImZ);
				float mu = iter_count - (log(log(modulus))) / log(2.0);
			 */

			if (ourCount > 0)
			{
				Debug.WriteLine($"There were {ourCount} out of range events.");
			}
			return escapeVelocities;
		}

		private int SaveCountsForDoneItems(Vector256<int> escapedFlagsVec, Vector256<int> targetReachedCompVec, 
			Vector256<int> countsV, ref Vector256<int> resultCountsV,
			ref Vector256<int> hasEscapedFlagsV, ref Vector256<int> doneFlagsV)
		{
			// Apply the new escapedFlags, only if the doneFlags is false for each vector position
			hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);

			var prevDoneFlagsV = doneFlagsV;

			// If escaped or reached the target iterations, we're done 
			doneFlagsV = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);

			var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());
			var prevCompositeIsDone = Avx2.MoveMask(prevDoneFlagsV.AsByte());
			if (compositeIsDone != prevCompositeIsDone)
			{
				var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);

				// Save the current count 
				resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1
			}

			return compositeIsDone;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int SaveCountsForDoneItems(Vector256<int> escapedFlagsVec, Vector256<int> targetReachedCompVec,
			Vector256<int> countsV, ref Vector256<int> resultCountsV,
			Vector256<uint>[] zRs, Vector256<uint>[] zIs,
			Vector256<uint>[] resultZRs, Vector256<uint>[] resultZIs,
			ref Vector256<int> hasEscapedFlagsV, ref Vector256<int> doneFlagsV)
		{
			// Apply the new escapedFlags, only if the doneFlags is false for each vector position
			hasEscapedFlagsV = Avx2.BlendVariable(escapedFlagsVec, hasEscapedFlagsV, doneFlagsV);

			var prevDoneFlagsV = doneFlagsV;

			// If escaped or reached the target iterations, we're done 
			doneFlagsV = Avx2.Or(hasEscapedFlagsV, targetReachedCompVec);

			var compositeIsDone = Avx2.MoveMask(doneFlagsV.AsByte());
			var prevCompositeIsDone = Avx2.MoveMask(prevDoneFlagsV.AsByte());
			if (compositeIsDone != prevCompositeIsDone)
			{
				var justNowDone = Avx2.CompareEqual(prevDoneFlagsV, doneFlagsV);

				// Save the current count 
				resultCountsV = Avx2.BlendVariable(countsV, resultCountsV, justNowDone); // use First if Zero, second if 1

				// Save the current ZValues.
				for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
				{
					resultZRs[limbPtr] = Avx2.BlendVariable(zRs[limbPtr].AsInt32(), resultZRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
					resultZIs[limbPtr] = Avx2.BlendVariable(zIs[limbPtr].AsInt32(), resultZIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
				}

				//ResetWorkingValues(cRs, cIs, zRs, zIs, justNowDone);
			}

			return compositeIsDone;
		}

		#endregion

		#region Support Methods

		//private (int prevLimbCount, int newLimbCount) GetMathAndAllocateTempVars(MapSectionRequest mapSectionRequest)

		private void GetMathAndAllocateTempVars(MapSectionRequest mapSectionRequest)
		{
			if (mapSectionRequest.MapSectionZVectors != null && mapSectionRequest.MapSectionZVectors.LimbCount != mapSectionRequest.LimbCount)
			{
				throw new ArgumentException($"The MapSectionRequest's MapSectionZVectors has a LimbCount of {mapSectionRequest.MapSectionZVectors.LimbCount} " +
					$"that does not match the MapSectionRequest LimbCount setting of {mapSectionRequest.LimbCount}!");
			}

			if (_useCImplementation && mapSectionRequest.LimbCount > 4)
			{
				throw new NotSupportedException("The current C++ Imp does not support LimbCount > 4.");
			}

			var currentBlockSize = _samplePointBuilder.BlockSize;
			var blockSizeForThisRequest = mapSectionRequest.BlockSize;

			if (currentBlockSize != blockSizeForThisRequest)
			{
				_samplePointBuilder.Dispose();
				_samplePointBuilder = new SamplePointBuilder(new SamplePointCache(blockSizeForThisRequest));
			}

			var currentLimbCount = _fp31VecMath.LimbCount;
			var limbCountForThisRequest = mapSectionRequest.LimbCount;

			if (currentLimbCount != limbCountForThisRequest)
			{
				//Debug.WriteLineIf(USE_DET_DEBUG, $"LimbCount Switch. From: {currentLimbCount}, To: {limbCountForThisRequest}. {_fp31VecMath.Implementation}");

				_fp31VecMath = _samplePointBuilder.GetVecMath(limbCountForThisRequest);
				_iterator = new IteratorDepthFirst(_fp31VecMath);

				_crs = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
				_cis = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
				_zrs = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
				_zis = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);

				_resultZrs = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
				_resultZis = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);

				_resultZrsForEscV = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
				_resultZisForEscV = FP31VecMathHelper.CreateNewLimbSet(limbCountForThisRequest);
			}
			else
			{
				Debug.WriteLineIf(USE_DET_DEBUG, $"LimbCount continues to be {currentLimbCount}. {_fp31VecMath.Implementation}");
			}

			//return (currentLimbCount, limbCountForThisRequest);
		}

		private IteratorCoords GetCoordinates(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var mapPosition = mapSectionRequest.MapPosition;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;

			var startingCx = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);
			var startingCy = FP31ValHelper.CreateFP31Val(mapPosition.Y, apFixedPointFormat);
			var delta = FP31ValHelper.CreateFP31Val(samplePointDelta.Width, apFixedPointFormat);

			var blockPos = mapSectionRequest.BlockPosition;
			var screenPos = mapSectionRequest.ScreenPosition;

			return new IteratorCoords(blockPos, screenPos, startingCx, startingCy, delta);
		}

		private void TestRoundTripRValue(MapSectionRequest mapSectionRequest, ApFixedPointFormat apFixedPointFormat)
		{
			var mapPosition = mapSectionRequest.MapPosition;
			var samplePointDelta = mapSectionRequest.SamplePointDelta;

			var rValueX = mapPosition.X;
			var fp31ValX = FP31ValHelper.CreateFP31Val(mapPosition.X, apFixedPointFormat);

			var rValueComp = FP31ValHelper.CreateRValue(sign: true, fp31ValX.Mantissa, apFixedPointFormat.TargetExponent, RMapConstants.DEFAULT_PRECISION);

			var doubles = RValueHelper.ConvertToDoubles(rValueX);
			var doublesComp = RValueHelper.ConvertToDoubles(rValueComp);


			var nX = RNormalizer.Normalize(rValueX, rValueComp, out var nComp);

			if (nX != nComp)
			{
				Debug.WriteLine("RValues do not agree.");
			}

			for (var i = 0; i < fp31ValX.Mantissa.Length; i++)
			{
				if (doubles[i] != doublesComp[i])
				{
					Debug.WriteLine($"Doubles do not agree at index: {i}");
				}
			}

		}

		private (MapSectionVectors, MapSectionZVectors?) GetMapSectionVectors(MapSectionRequest mapSectionRequest)
		{
			var (msv, mszv) = mapSectionRequest.TransferMapVectorsOut();
			if (msv == null) throw new ArgumentException("The MapSectionVectors is null.");
			//if (mszv == null) throw new ArgumentException("The MapSetionZVectors is null.");

			return (msv, mszv);
		}

		private void ReportLimbCountUpdate(IteratorCoords coords, int currentLimbCount, int limbCountForThisRequest, int precision)
		{
			if (currentLimbCount != limbCountForThisRequest && coords.ScreenPos.IsZero())
			{
				Debug.WriteLine($"Changing Limbcount from {currentLimbCount} to {limbCountForThisRequest}. Precision: {precision}. " +
					$"\nCx: {coords.StartingCx.ToStringDec()}; Cy: {coords.StartingCy.ToStringDec()}." +
					$"\nCx: {coords.StartingCx}; Cy: {coords.StartingCy}" +
					$"\nCx: {coords.GetStartingCxStringVal()}; Cy: {coords.GetStartingCyStringVal()}.");
			}
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
			var spX = coords.ScreenPos.X;
			var spY = coords.ScreenPos.Y;
			if (spX == 0 && spY == 0 || spX == 3 && spY == 4)
			{
				ReportSamplePoints("Offsets:", samplePointOffsets);
				Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay("y[0]", samplePointsY[0].Mantissa)} {samplePointsY[0].Exponent}.");
				ReportSamplePoints("X Points:", samplePointsX);
			}
		}

		private void ReportSamplePoints(string name, FP31Val[] fP31Vals)
		{
			Debug.WriteLine("");
			Debug.WriteLine($"{name}");

			for (var i = 0; i < 32; i++)
			{
				var value = fP31Vals[i];	
				Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay(i.ToString(), value.Mantissa)} {value.Exponent}.");
			}

			//foreach (var value in fP31Vals)
			//{
			//	Debug.WriteLine($"{FP31ValHelper.GetDiagDisplay("x", value.Mantissa)} {value.Exponent}.");
			//}
		}

		private void ReportResults(IteratorCoords coords, MapSectionRequest request, MapSectionResponse result, CancellationToken ct)
		{
			var s1 = coords.GetStartingCxStringVal();
			var s2 = coords.GetStartingCyStringVal();

			Debug.WriteLine($"{s1}, {s2}: {result.MathOpCounts}");

			if (ct.IsCancellationRequested)
			{
				Debug.WriteLine($"The block: {coords.ScreenPos} is cancelled.");
			}
			else
			{
				if (result.AllRowsHaveEscaped)
				{
					Debug.WriteLine($"The entire block: {coords.ScreenPos} is done.");
				}
			}
		}

		private bool ShouldSkipThisSection(bool skipPositiveBlocks, bool skipLowDetailBlocks, IteratorCoords coords)
		{
			//// Skip positive 'blocks'

			//if (skipPositiveBlocks)
			//{
			//	var xSign = coords.StartingCx.GetSign();
			//	var ySign = coords.StartingCy.GetSign();

			//	return xSign && ySign;
			//}

			//// Move directly to a block where at least one sample point reaches the iteration target.
			//else if (skipLowDetailBlocks && (BigInteger.Abs(coords.ScreenPos.Y) > 1 || BigInteger.Abs(coords.ScreenPos.X) > 3))
			//{
			//	return true;
			//}

			// Only process block at screen position x=1, y = 0
			if (coords.ScreenPos.X == 1 && coords.ScreenPos.Y == 0)
			{
				return false;
			}

			return true;
		}

		[Conditional("PERF")]
		private void TallyUsedAndUnusedCalcs(int idx, Vector256<int> originalCountsV, Vector256<int> newCountsV, Vector256<int> resultCountsV, Vector256<int>[] usedCalcs, Vector256<int>[] unusedCalcs)
		{
			usedCalcs[idx] = Avx2.Subtract(resultCountsV, originalCountsV);
			unusedCalcs[idx] = Avx2.Subtract(newCountsV, resultCountsV);
		}

		[Conditional("PERF")]
		private void UpdateResponseWithMops(MapSectionResponse mapSectionResponse, IIterator iterator, IIterationState iterationState)
		{
			mapSectionResponse.MathOpCounts = iterator.MathOpCounts.Clone();
			mapSectionResponse.MathOpCounts.RollUpNumberOfCalcs(iterationState.RowUsedCalcs, iterationState.RowUnusedCalcs);
		}

		//[Conditional("DIAG")]
		//private void ResetWorkingValues(Vector256<uint>[] cRs, Vector256<uint>[] cIs, Vector256<uint>[] zRs, Vector256<uint>[] zIs, Vector256<int> justNowDone)
		//{
		//	for (var limbPtr = 0; limbPtr < _resultZrs.Length; limbPtr++)
		//	{

		//		// Set the working values to zero -- these values are now 'out of play.'
		//		cRs[limbPtr] = Avx2.BlendVariable(Vector256<int>.Zero, cRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
		//		cIs[limbPtr] = Avx2.BlendVariable(Vector256<int>.Zero, cIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1

		//		zRs[limbPtr] = Avx2.BlendVariable(Vector256<int>.Zero, zRs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
		//		zIs[limbPtr] = Avx2.BlendVariable(Vector256<int>.Zero, zIs[limbPtr].AsInt32(), justNowDone).AsUInt32(); // use First if Zero, second if 1
		//	}
		//}

		#endregion
	}
}
