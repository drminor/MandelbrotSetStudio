using MEngineDataContracts;
using MSS.Common;
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
			var precision = mapSectionRequest.Precision; // + 20;

			var smxMathHelper = new SmxMathHelper(precision);
			var smxVecMathHelper = new SmxVecMathHelper(mapSectionRequest.DoneFlags, precision);

			var startingCx = CreateSmxFromDto(smxMathHelper, mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = CreateSmxFromDto(smxMathHelper, mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = CreateSmxFromDto(smxMathHelper, samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			uint threshold = 0; // 4;
			var counts = GenerateMapSection(smxVecMathHelper, startingCx, startingCy, delta, blockSize, targetIterations, threshold);
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		private ushort[] GenerateMapSection(SmxVecMathHelper smxVecMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations, uint threshold)
		{
			var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
			var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
			var s3 = RValueHelper.ConvertToString(delta.GetRValue());

			Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Precision: {startingCx.Precision}");

			var iterator = new IteratorVector(smxVecMathHelper);
			//iterator.Sample();

			var stride = blockSize.Width;
			var samplePointOffsets = iterator.BuildSamplePointOffsets(delta, stride);

			var samplePointsX = iterator.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = iterator.BuildSamplePoints(startingCy, samplePointOffsets);

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

		private Smx CreateSmxFromDto(SmxMathHelper smxMathHelper, long[] values, int exponent, int precision)
		{
			var sign = !values.Any(x => x < 0);

			var mantissa = ConvertDtoLongsToSmxULongs(values);
			var nrmMantissa = smxMathHelper.NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);

			if (SmxMathHelper.CheckPWValues(nrmMantissa))
			{
				Debug.WriteLine("XXX");
			}
			var result = new Smx(sign, nrmMantissa, nrmExponent, precision);

			return result;
		}

		private ulong[] ConvertDtoLongsToSmxULongs(long[] values/*, out int shiftAmount*/)
		{
			// DtoLongs are in Big-Endian order, convert to Little-Endian order.
			//var leValues = values.Reverse().ToArray();

			// Currently the Dto classes produce an array of longs with length of either 1 or 2.

			var leValues = TrimLeadingZeros(values);

			if (leValues.Length > 1)
			{
				throw new NotSupportedException("ConvertDtoLongsToSmxULongs only supports values with a single 'digit.'");
			}

			var result = new ulong[leValues.Length * 2];

			for (int i = 0; i < leValues.Length; i++)
			{
				var value = (ulong)Math.Abs(leValues[i]);
				var lo = Split(value, out var hi);
				result[2 * i] = lo;
				result[2 * i + 1] = hi;
			}

			var trResult = SmxMathHelper.TrimLeadingZeros(result);
			return trResult;
		}

		// Trim Leading Zeros for a Big-Endian formatted array of longs.
		private long[] TrimLeadingZeros(long[] mantissa)
		{
			var i = 0;
			for (; i < mantissa.Length; i++)
			{
				if (mantissa[i] != 0)
				{
					break;
				}
			}

			if (i == 0)
			{
				return mantissa;
			}

			if (i == mantissa.Length)
			{
				// All digits are zero
				return new long[] { 0 };
			}

			var result = new long[mantissa.Length - i];
			Array.Copy(mantissa, i, result, 0, result.Length);
			return result;
		}

		private ulong Split(ulong x, out ulong hi)
		{
			hi = x >> 32; // Create new ulong from bits 32 - 63.
			return x & 0x00000000FFFFFFFF; // Create new ulong from bits 0 - 31.
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
