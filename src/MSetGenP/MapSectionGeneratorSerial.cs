using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MSetGenP
{
	public class MapSectionGeneratorSerial
	{
		public static MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapPositionDto = mapSectionRequest.Position;
			var samplePointDeltaDto = mapSectionRequest.SamplePointDelta;
			var blockSize = mapSectionRequest.BlockSize;

			var startingCx = CreateSmx(mapPositionDto.X, mapPositionDto.Exponent);
			var startingCy = CreateSmx(mapPositionDto.Y, mapPositionDto.Exponent);
			var delta = CreateSmx(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent);

			var counts = GenerateMapSection(startingCx, startingCy, delta, blockSize);
			var escapeVelocities = new ushort[128 * 128];
			var doneFlags = new bool[128 * 128];
			var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues: null);

			return result;
		}

		public static ushort[] GenerateMapSection(Smx startingCx, Smx startingCy, Smx delta, SizeInt blockSize)	
		{
			var s1 = RValueHelper.ConvertToString(startingCx.GetRValue());
			var s2 = RValueHelper.ConvertToString(startingCy.GetRValue());
			var s3 = RValueHelper.ConvertToString(delta.GetRValue());

			var mapPosPrecision = SmxMathHelper.GetPrecision(startingCx);

			var samplePrecision = SmxMathHelper.GetPrecision(delta);


			Debug.WriteLine($"Value of C at origin: real: {s1}, imaginary: {s2}. Delta: {s3}. Position Precision: {mapPosPrecision}. Sample Precision: {samplePrecision}.");

			var result = new ushort[blockSize.NumberOfCells];

			var samplePointOffsets = BuildSamplePointOffsets(delta, blockSize.Width);
			var samplePointsX = BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = BuildSamplePoints(startingCy, samplePointOffsets);



			return result;
		}

		private static Smx[] BuildSamplePoints(Smx startValue, Smx[] samplePoints)
		{
			var result = new Smx[samplePoints.Length];

			for(var i = 0; i < samplePoints.Length; i++)
			{
				result[i] = SmxMathHelper.Add(startValue, samplePoints[i]);
			}

			return result;
		}

		private static Smx[] BuildSamplePointOffsets(Smx delta, int sampleCount)
		{
			var result = new Smx[sampleCount];

			for(var i = 0; i < sampleCount; i++)
			{
				result[i] = SmxMathHelper.Multiply(delta, i);
			}

			return result;
		}


		private static Smx CreateSmx(long[] values, int exponent, int precision = 70)
		{
			var sign = !values.Any(x => x < 0);
			var mantissa = ConvertDtoLongsToSmxULongs(values, out var shiftAmount);
			var adjExponent = exponent - shiftAmount;
			var result = new Smx(sign, mantissa, adjExponent, precision);

			return result;
		}

		private static ulong[] ConvertDtoLongsToSmxULongs(long[] values, out int shiftAmount)
		{
			// DtoLongs are in Big-Endian order, convert to Little-Endian order.
			var leValues = values.Reverse().ToArray();

			var result = new ulong[leValues.Length * 2];

			for (int i = 0; i < leValues.Length; i++)
			{
				var value = (ulong) Math.Abs(leValues[i]);
				var lo = SmxMathHelper.Split(value, out var hi);
				result[2 * i] = lo;
				result[2 * i + 1] = hi;
			}

			var trResult = SmxMathHelper.TrimLeadingZeros(result);
			var adjResult = SmxMathHelper.FillMsb(trResult, out shiftAmount);

			return adjResult;
		}

	}
}
