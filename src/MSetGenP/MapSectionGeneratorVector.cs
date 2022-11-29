using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

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

			//var targetExponent = -88; // samplePointDeltaDto.Exponent - 20;

			var fixedPointFormat = new ApFixedPointFormat(8, 3 * 32 - 8);
			var smxMathHelper = new SmxMathHelper(fixedPointFormat);
			var smxVecMathHelper = new SmxVecMathHelper(mapSectionRequest.DoneFlags, fixedPointFormat);

			var dtoMapper = new DtoMapper();
			var mapPosition = dtoMapper.MapFrom(mapPositionDto);
			var samplePointDelta = dtoMapper.MapFrom(samplePointDeltaDto);

			var startingCx = smxMathHelper.CreateSmx(mapPosition.X); // .CreateSmxFromDto(mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = smxMathHelper.CreateSmx(mapPosition.Y); // .CreateSmxFromDto(mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = smxMathHelper.CreateSmx(samplePointDelta.Width); //.CreateSmxFromDto(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Precision: {startingCx.Precision}");
			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			
			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 0;

			var counts = GenerateMapSection(smxMathHelper, smxVecMathHelper, startingCx, startingCy, delta, blockSize, targetIterations, threshold);
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		private ushort[] GenerateMapSection(SmxMathHelper smxMathHelper, SmxVecMathHelper smxVecMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations, uint threshold)
		{
			var iterator = new IteratorVector(smxVecMathHelper);
			//iterator.Sample();

			var stride = blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);

			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var resultLength = blockSize.NumberOfCells;
			//var numberOfLimbs = samplePointsX[0].LimbCount;

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

			var cRs = new FPValues(crSmxes);
			var cIs = new FPValues(ciSmxes);

			var zRs = cRs.Clone();
			var zIs = cIs.Clone();

			var zRSqrs = smxVecMathHelper.Square(zRs);
			var zISqrs = smxVecMathHelper.Square(zIs);

			var cntrs = Enumerable.Repeat((ushort)1, resultLength).ToArray();

			var escapedFlagsMem = new Memory<ulong>(new ulong[resultLength]);
			var escapedFlagVectors = MemoryMarshal.Cast<ulong, Vector<ulong>>(escapedFlagsMem.Span);

			var inPlayList = smxVecMathHelper.InPlayList;

			while (inPlayList.Count > 0)
			{
				iterator.Iterate(cRs, cIs, zRs, zIs, zRSqrs, zISqrs);
				var sumOfSqrs = smxVecMathHelper.Add(zRSqrs, zISqrs);

				smxVecMathHelper.IsGreaterOrEqThan(sumOfSqrs, threshold, escapedFlagVectors);
				var vectorsNoLongerInPlay = UpdateCounts(inPlayList, escapedFlagVectors, cntrs);
				foreach (var vectorIndex in vectorsNoLongerInPlay)
				{
					inPlayList.Remove(vectorIndex);
				}
			}

			return cntrs;
		}

		private List<int> UpdateCounts(List<int> inPlayList, Span<Vector<ulong>> escapedFlagVectors, ushort[] cntrs)
		{
			var lanes = Vector<ulong>.Count;
			var toBeRemoved = new List<int>();

			foreach (var idx in inPlayList)
			{
				var escapedFlagVector = escapedFlagVectors[idx];

				if (Vector.EqualsAny(escapedFlagVector, Vector<ulong>.One))
				{
					toBeRemoved.Add(idx);
				}

				var cntrPtr = idx * lanes;
				for(var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (escapedFlagVector[lanePtr] == 0)
					{
						cntrs[cntrPtr + lanePtr]++;
					}
				}
			}

			return toBeRemoved;
		}

		private bool[] CalculateTheDoneFlags(ushort[] counts, int targetIterations)
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
