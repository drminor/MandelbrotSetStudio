using MEngineDataContracts;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MEngineService
{
	public class MapSectionGenerator
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			MapSectionRequestStruct requestStruct = new MapSectionReqHelper().GetRequestStruct(mapSectionRequest);

			int[] counts = GetAndFillCountsBuffer(mapSectionRequest, out var countsBuffer);
			bool[] doneFlags = GetAndFillDoneFlagsBuffer(mapSectionRequest, out var doneFlagsBuffer);
			double[] zValues = GetAndFillZValuesBuffer(mapSectionRequest, out var zValuesBuffer);

			NativeMethods.GenerateMapSection(requestStruct, countsBuffer, doneFlagsBuffer, zValuesBuffer);

			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			//Marshal.Copy(doneFlagsBuffer,  doneFlags, 0, length);
			Marshal.FreeCoTaskMem(doneFlagsBuffer);

			Marshal.Copy(zValuesBuffer, zValues, 0, zValues.Length);
			Marshal.FreeCoTaskMem(zValuesBuffer);

			var result = new MapSectionResponse
			{
				MapSectionId = mapSectionRequest.MapSectionId,
				SubdivisionId = mapSectionRequest.SubdivisionId,
				BlockPosition = mapSectionRequest.BlockPosition,
				MapCalcSettings = mapSectionRequest.MapCalcSettings,
				Counts = counts,
				DoneFlags = doneFlags,
				ZValues = zValues
			};

			return result;
		}

		private int[] GetAndFillCountsBuffer(MapSectionRequest mapSectionRequest, out IntPtr countsBuffer)
		{
			int[] counts;
			
			if (mapSectionRequest.Counts != null)
			{
				counts = mapSectionRequest.Counts;
			}
			else
			{
				counts = new int[mapSectionRequest.BlockSize.NumberOfCells];
			}

			countsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			return counts;
		}

		private bool[] GetAndFillDoneFlagsBuffer(MapSectionRequest mapSectionRequest, out IntPtr countsBuffer)
		{
			bool[] doneFlags;

			if (mapSectionRequest.DoneFlags != null)
			{
				doneFlags = mapSectionRequest.DoneFlags;
			}
			else
			{
				doneFlags = new bool[mapSectionRequest.BlockSize.NumberOfCells];
			}

			countsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(byte)) * doneFlags.Length);

			var bArray = doneFlags.Select(x => x ? (byte)1 : (byte)0).ToArray();

			Marshal.Copy(bArray, 0, countsBuffer, doneFlags.Length);

			return doneFlags;
		}

		private double[] GetAndFillZValuesBuffer(MapSectionRequest mapSectionRequest, out IntPtr countsBuffer)
		{
			double[] zValues;

			if (mapSectionRequest.ZValues != null)
			{
				zValues = mapSectionRequest.ZValues;
			}
			else
			{
				zValues = new double[mapSectionRequest.BlockSize.NumberOfCells * 4];
			}

			countsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(double)) * zValues.Length);
			Marshal.Copy(zValues, 0, countsBuffer, zValues.Length);

			return zValues;
		}

	}
}
