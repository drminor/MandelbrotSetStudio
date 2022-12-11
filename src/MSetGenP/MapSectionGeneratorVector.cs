using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGenP
{
	public class MapSectionGeneratorVector
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapPositionDto = mapSectionRequest.Position;
			var samplePointDeltaDto = mapSectionRequest.SamplePointDelta;
			var blockSize = mapSectionRequest.BlockSize;
			var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			
			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var counts = mapSectionRequest.Counts;
			var counts = new ushort[blockSize.NumberOfCells];

			//var doneFlags = mapSectionRequest.DoneFlags;
			var doneFlags = new bool[blockSize.NumberOfCells];

			var fixedPointFormat = new ApFixedPointFormat(8, precision);
			//var fixedPointFormat = new ApFixedPointFormat(3);

			var smxMathHelper = new SmxMathHelper(fixedPointFormat, threshold);

			var dtoMapper = new DtoMapper();
			var mapPosition = dtoMapper.MapFrom(mapPositionDto);
			var samplePointDelta = dtoMapper.MapFrom(samplePointDeltaDto);

			var startingCx = smxMathHelper.CreateSmx(mapPosition.X);
			var startingCy = smxMathHelper.CreateSmx(mapPosition.Y);
			var delta = smxMathHelper.CreateSmx(samplePointDelta.Width);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			var blockPos = mapSectionRequest.BlockPosition;
			//Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			//Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}.");

			//var cRs = smxMathHelper.BuildMapPoints(startingCx, startingCy, delta, blockSize, out var cIs);

			var stride = (byte)blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);
			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var cRs = new FPValues(samplePointsX);

			var aCarries = 0;
			var mCarries = 0;

			for (int j = 0; j < samplePointsY.Length; j++)
			{
				//var cRs = new FPValues(samplePointsX);
				var cIs = new FPValues(samplePointsY[j], stride);

				var rowDoneFlags = new bool[stride];
				Array.Copy(doneFlags, j * stride, rowDoneFlags, 0, stride);
				var smxVecMathHelper = new SmxVecMathHelper(fixedPointFormat, threshold, rowDoneFlags);

				var rowCounts = GenerateMapSection(smxVecMathHelper, cRs, cIs, targetIterations);
				Array.Copy(rowCounts, 0, counts, j * stride, stride);

				aCarries += smxVecMathHelper.NumberOfACarries;
				mCarries += smxVecMathHelper.NumberOfMCarries;
			}

			//Debug.WriteLine($"Completed: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). ACarries: {aCarries}, MCarries:{mCarries}.");
			Debug.WriteLine($"Completed: BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. ACarries: {aCarries}, MCarries:{mCarries}.");

			var escapeVelocities = new ushort[128 * 128];
			var compressedDoneFlags = CompressTheDoneFlags(counts, targetIterations);
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
			return result;
		}

		private ushort[] GenerateMapSection(SmxVecMathHelper smxVecMathHelper, FPValues cRs, FPValues cIs, int targetIterations)
		{
			var resultLength = cRs.Length;
			var cntrs = Enumerable.Repeat((ushort)1, resultLength).ToArray();
			//var cntrs = new ushort[resultLength]; // Using 0, instead of 1

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);
			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);

			var escapedFlagsMem = new Memory<long>(new long[resultLength]);
			var escapedFlagVectors = MemoryMarshal.Cast<long, Vector256<long>>(escapedFlagsMem.Span);

			var inPlayList = smxVecMathHelper.InPlayList;

			// Perform the first iteration. 
			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			smxVecMathHelper.Square(zRs, zRSqrs);
			smxVecMathHelper.Square(zIs, zISqrs);
			smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);


			UpdateTheInPlayList(smxVecMathHelper, sumOfSqrs, escapedFlagVectors, targetIterations, cntrs, inPlayList);

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			while (inPlayList.Count > 0)
			{
				iterator.Iterate();
				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
				UpdateTheInPlayList(smxVecMathHelper, sumOfSqrs, escapedFlagVectors, targetIterations, cntrs, inPlayList);
			}

			return cntrs;
		}

		private void UpdateTheInPlayList(SmxVecMathHelper smxVecMathHelper, FPValues sumOfSqrs, Span<Vector256<long>> escapedFlagVectors, int targetIterations, ushort[] cntrs, List<int> inPlayList)
		{
			smxVecMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlagVectors);
			var vectorsNoLongerInPlay = UpdateCounts(smxVecMathHelper, inPlayList, targetIterations, escapedFlagVectors, cntrs, sumOfSqrs);

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				inPlayList.Remove(vectorIndex);
			}
		}

		private List<int> UpdateCounts(SmxVecMathHelper smxVecMathHelper, List<int> inPlayList, int targetIterations, Span<Vector256<long>> escapedFlagVectors, ushort[] cntrs, FPValues sumOfSqrs)
		{
			var lanes = Vector256<ulong>.Count;
			var toBeRemoved = new List<int>();

			foreach (var idx in inPlayList)
			{
				var anyReachedTargetIterations = false;
				var anyEscaped = false;

				var allCompleted = true;

				var escapedFlagVector = escapedFlagVectors[idx];

				var cntrsBuf = Enumerable.Repeat(-1, lanes).ToArray();

				var cntrPtr = idx * lanes;
				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (escapedFlagVector.GetElement(lanePtr) == 0)
					{
						var cnt = (cntrs[cntrPtr + lanePtr] + 1);

						if (cnt >= ushort.MaxValue)
						{
							Debug.WriteLine($"WARNING: The Count is > ushort.Max.");
							cnt = ushort.MaxValue;
						}

						//cntrs[cntrPtr + lanePtr] = (ushort) cnt;
						cntrsBuf[lanePtr] = (ushort)cnt;

						if (cnt >= targetIterations)
						{
							// Target reached
							anyReachedTargetIterations = true;

							//var sacResult = escapedFlagVector.GetElement(lanePtr);
							//var rValDiag = smxVecMathHelper.GetSmxAtIndex(sumOfSqrs, idx + lanePtr).GetStringValue();
							//Debug.WriteLine($"Target reached: The value is {rValDiag}. Compare returned: {sacResult}.");
						}
						else
						{
							// Didn't escape and didn't reach target
							allCompleted = false;
						}
					}
					else
					{
						//cntrsBuf[lanePtr] = cntrs[cntrPtr + lanePtr]; // record current counter.
						// Escaped
						anyEscaped = true;
						//var sacResult = escapedFlagVector.GetElement(lanePtr);
						//var rValDiag = smxVecMathHelper.GetSmxAtIndex(sumOfSqrs, idx + lanePtr).GetStringValue();
						//Debug.WriteLine($"Bailed out: The value is {rValDiag}. Compare returned: {sacResult}.");
					}
				}

				//if (allCompleted)
				//{
				//	toBeRemoved.Add(idx);
				//}

				if (anyReachedTargetIterations || anyEscaped)
				{
					if (!allCompleted)
					{
						for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
						{
							cntrs[cntrPtr + lanePtr] = cntrsBuf[lanePtr] == -1 ? (ushort)51 : (ushort)(cntrsBuf[lanePtr]);
						}
					}
					else
					{
						for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
						{
							if (cntrsBuf[lanePtr] != -1)
							{
								cntrs[cntrPtr + lanePtr] = (ushort)cntrsBuf[lanePtr];
							}
						}
					}

					toBeRemoved.Add(idx);
				}
				else
				{
					for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
					{
						if (cntrsBuf[lanePtr] != -1)
						{
							cntrs[cntrPtr + lanePtr] = (ushort)cntrsBuf[lanePtr];
						}
					}

				}
			}

			return toBeRemoved;
		}

		private bool[] CompressTheDoneFlags(ushort[] counts, int targetIterations)
		{
			bool[] result;

			if (!counts.Any(x => x < targetIterations))
			{
				// All reached the target
				result = new bool[] { true };
			}
			else if (!counts.Any(x => x >= targetIterations))
			{
				// none reached the target
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = counts.Select(x => x >= targetIterations).ToArray();
			}

			return result;
		}



	}
}
