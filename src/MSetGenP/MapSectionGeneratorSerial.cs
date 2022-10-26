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
			var precision = mapSectionRequest.Precision;

			var startingCx = CreateSmxFromDto(mapPositionDto.X, mapPositionDto.Exponent, precision);
			var startingCy = CreateSmxFromDto(mapPositionDto.Y, mapPositionDto.Exponent, precision);
			var delta = CreateSmxFromDto(samplePointDeltaDto.Width, samplePointDeltaDto.Exponent, precision);

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

			for(int j = 0; j < samplePointsY.Length; j++)
			{
				for (int i = 0; i < samplePointsX.Length; i++)
				{
					var zR = Smx.Zero;
					var zI = Smx.Zero;

					ushort cntr;
					for (cntr = 0; cntr < 10; cntr++)
					{
						if (! Iterate(ref zR, ref zI, samplePointsX[i], samplePointsY[j]) )
						{
							break;
						}
					}

					result[j * 128 + i] = cntr;
				}
			}


			return result;
		}

		private static bool Iterate(ref Smx zR, ref Smx zI, Smx cR, Smx cI)
		{
			zR = SmxMathHelper.Add(zR, cR);
			zI = SmxMathHelper.Add(zI, cI);

			return true;
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


		private static Smx CreateSmxFromDto(long[] values, int exponent, int precision = 70)
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
