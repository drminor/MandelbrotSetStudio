using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System.Diagnostics;

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

			var smxVecMathHelper = new SmxVecMathHelper(precision);

			var startingCx = CreateSmxFromDto(mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = CreateSmxFromDto(mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = CreateSmxFromDto(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;

			var counts = GenerateMapSection(smxVecMathHelper, startingCx, startingCy, delta, blockSize, targetIterations);
			var doneFlags = CalculateTheDoneFlags(counts, targetIterations);

			var escapeVelocities = new ushort[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		private ushort[] GenerateMapSection(SmxVecMathHelper smxVecMathHelper, Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize, int targetIterations)
		{
			var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
			var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
			var s3 = RValueHelper.ConvertToString(delta.GetRValue());

			Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Precision: {startingCx.Precision}");

			var result = new ushort[blockSize.NumberOfCells];

			var iterator = new IteratorVector(smxVecMathHelper, targetIterations);

			//iterator.Sample();

			var stride = blockSize.Width;
			var samplePointOffsets = iterator.BuildSamplePointOffsets(delta, stride);
			var samplePointsX = iterator.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = iterator.BuildSamplePoints(startingCy, samplePointOffsets);

			for (int j = 0; j < samplePointsY.Length; j++)
			{
				for (int i = 0; i < samplePointsX.Length; i++)
				{
					var cntr = iterator.Iterate(samplePointsX[i], samplePointsY[j]);
					result[j * stride + i] = cntr;
				}
			}

			return result;
		}



		private Smx CreateSmxFromDto(long[] values, int exponent, int precision)
		{
			var sign = !values.Any(x => x < 0);

			//var mantissa = ConvertDtoLongsToSmxULongs(values, out var shiftAmount);
			//var adjExponent = exponent - shiftAmount;
			//var result = new Smx(sign, mantissa, adjExponent, precision);

			var mantissa = ConvertDtoLongsToSmxULongs(values);
			var nrmMantissa = SmxMathHelper.NormalizeFPV(mantissa, exponent, precision, out var nrmExponent);

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

			//return result;
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
