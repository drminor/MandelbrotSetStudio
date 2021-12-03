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

			int length = mapSectionRequest.BlockSize.NumberOfCells;

			IntPtr countsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * length);
			IntPtr doneFlagsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(bool)) * length);
			IntPtr zValuesBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(double)) * length * 4);

			NativeMethods.GenerateMapSection(requestStruct, countsBuffer, doneFlagsBuffer, zValuesBuffer);

			int[] counts = new int[length];
			Marshal.Copy(countsBuffer, counts, 0, length);
			Marshal.FreeCoTaskMem(countsBuffer);

			// TODO: Return the doneFlags and zValues to the caller.
			Marshal.FreeCoTaskMem(doneFlagsBuffer);
			Marshal.FreeCoTaskMem(zValuesBuffer);

			var result = new MapSectionResponse
			{
				SubdivisionId = mapSectionRequest.SubdivisionId,
				BlockPosition = mapSectionRequest.BlockPosition,
				Status = 0,          // Ok
				Counts = counts
			};

			return result;
		}

	}
}
