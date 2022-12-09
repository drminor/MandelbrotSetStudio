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
			var doneFlags = mapSectionRequest.DoneFlags;

			var fixedPointFormat = new ApFixedPointFormat(8, precision);
			var smxMathHelper = new SmxMathHelper(fixedPointFormat);
			var smxVecMathHelper = new SmxVecMathHelper(fixedPointFormat, threshold, doneFlags);



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
			Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");

			var cRs = BuildMapPoints(smxMathHelper, startingCx, startingCy, delta, blockSize, out var cIs);

			var counts = GenerateMapSection(smxVecMathHelper, cRs, cIs, targetIterations, threshold);
			var doneFlagsCompressed = CompressTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlagsCompressed, zValues: null);

			return result;
		}

		private ushort[] GenerateMapSection(SmxVecMathHelper smxVecMathHelper, FPValues cRs, FPValues cIs, int targetIterations, uint threshold)
		{
			var resultLength = cRs.Length;

			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			var zRSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			var zISqrs = new FPValues(cIs.LimbCount, cIs.Length);

			smxVecMathHelper.Square(zRs, zRSqrs);
			smxVecMathHelper.Square(zIs, zISqrs);

			var cntrs = Enumerable.Repeat((ushort)1, resultLength).ToArray();

			var escapedFlagsMem = new Memory<long>(new long[resultLength]);
			var escapedFlagVectors = MemoryMarshal.Cast<long, Vector256<long>>(escapedFlagsMem.Span);

			var inPlayList = smxVecMathHelper.InPlayList;

			var iterator = new IteratorVector(smxVecMathHelper, cRs, cIs, zRs, zIs, zRSqrs, zISqrs);

			var sumOfSqrs = new FPValues(cRs.LimbCount, cRs.Length);
			
			smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
			smxVecMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlagVectors);
			var vectorsNoLongerInPlay = UpdateCounts(inPlayList, targetIterations, escapedFlagVectors, cntrs);

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				inPlayList.Remove(vectorIndex);
			}

			while (inPlayList.Count > 0)
			{
				iterator.Iterate();

				smxVecMathHelper.Add(zRSqrs, zISqrs, sumOfSqrs);
				smxVecMathHelper.IsGreaterOrEqThanThreshold(sumOfSqrs, escapedFlagVectors);
				vectorsNoLongerInPlay = UpdateCounts(inPlayList, targetIterations, escapedFlagVectors, cntrs);

				foreach (var vectorIndex in vectorsNoLongerInPlay)
				{
					inPlayList.Remove(vectorIndex);
				}
			}

			return cntrs;
		}

		private List<int> UpdateCounts(List<int> inPlayList, int targetIterations, Span<Vector256<long>> escapedFlagVectors, ushort[] cntrs)
		{
			var lanes = Vector256<ulong>.Count;
			var toBeRemoved = new List<int>();

			foreach (var idx in inPlayList)
			{
				var oneOrMoreReachedTargetIterations = false;
				var anyEscaped = false;

				var escapedFlagVector = escapedFlagVectors[idx];

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

						cntrs[cntrPtr + lanePtr] = (ushort) cnt;

						if (cnt >= targetIterations)
						{
							oneOrMoreReachedTargetIterations = true;
						}
					}
					else
					{
						anyEscaped = true;
					}
				}

				if (oneOrMoreReachedTargetIterations || anyEscaped)
				{
					toBeRemoved.Add(idx);
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

		private FPValues BuildMapPoints(SmxMathHelper smxMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, out FPValues cIValues)
		{
			var stride = (byte)blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);
			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var resultLength = blockSize.NumberOfCells;

			var crSmxes = new Smx[resultLength];
			var ciSmxes = new Smx[resultLength];

			var resultPtr = 0;
			for (int j = 0; j < samplePointsY.Length; j++)
			{
				for (int i = 0; i < samplePointsX.Length; i++)
				{
					ciSmxes[resultPtr] = samplePointsY[j];
					crSmxes[resultPtr++] = samplePointsX[i];
				}
			}

			var result = new FPValues(crSmxes);
			cIValues = new FPValues(ciSmxes);

			return result;
		}

	}
}
