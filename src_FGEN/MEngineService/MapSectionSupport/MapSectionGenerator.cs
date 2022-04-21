using MEngineDataContracts;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace MEngineService
{
	public static class MapSectionGenerator
	{
		public static MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			// Counts
			int[] counts = GetAndFillCountsBuffer(mapSectionRequest, out var countsBuffer);

			// Done Flags
			byte[] doneFlagsAsBArray = ConvertDoneFlags(mapSectionRequest);
			var doneFlagsBuffer = GetDoneFlagsBuffer(doneFlagsAsBArray);
			
			// ZValues
			double[] zValues = GetAndFillZValuesBuffer(mapSectionRequest, out var zValuesBuffer);

			var requestStruct = new MapSectionReqHelper().GetRequestStruct(mapSectionRequest);
			NativeMethods.GenerateMapSection(requestStruct, countsBuffer, doneFlagsBuffer, zValuesBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			// Done Flags
			Marshal.Copy(doneFlagsBuffer, doneFlagsAsBArray, 0, doneFlagsAsBArray.Length);
			Marshal.FreeCoTaskMem(doneFlagsBuffer);
			var doneFlags = doneFlagsAsBArray.Select(x => x == 1 ? true : false).ToArray();

			// ZValues
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

		private static int[] GetAndFillCountsBuffer(MapSectionRequest mapSectionRequest, out IntPtr countsBuffer)
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

		private static byte[] ConvertDoneFlags(MapSectionRequest mapSectionRequest)
		{
			byte[] doneFlagsAsBArray;

			if (mapSectionRequest.DoneFlags != null)
			{
				doneFlagsAsBArray = mapSectionRequest.DoneFlags.Select(x => x ? (byte)1 : (byte)0).ToArray();
			}
			else
			{
				doneFlagsAsBArray = new byte[mapSectionRequest.BlockSize.NumberOfCells];
			}

			return doneFlagsAsBArray;
		}

		private static IntPtr GetDoneFlagsBuffer(byte[] doneFlagsAsBArray)
		{
			IntPtr doneFlagsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(byte)) * doneFlagsAsBArray.Length);
			Marshal.Copy(doneFlagsAsBArray, 0, doneFlagsBuffer, doneFlagsAsBArray.Length);

			return doneFlagsBuffer;
		}

		private static double[] GetAndFillZValuesBuffer(MapSectionRequest mapSectionRequest, out IntPtr zValuesBuffer)
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

			zValuesBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(double)) * zValues.Length);
			Marshal.Copy(zValues, 0, zValuesBuffer, zValues.Length);

			return zValues;
		}

	}
}
