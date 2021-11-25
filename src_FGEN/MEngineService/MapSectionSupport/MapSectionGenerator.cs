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
			NativeMethods.GenerateMapSection(requestStruct, ref countsBuffer, length);

			int[] tmpCnts = new int[length];
			Marshal.Copy(countsBuffer, tmpCnts, 0, length);
			Marshal.FreeCoTaskMem(countsBuffer);

			// TODO: Update C++ code to use integers
			// Marshal.Copy does not have a method to copy unsigned integers, so we copy the buffer into an array of ints and then cast these to an array of uints.
			uint[] counts = tmpCnts.Cast<uint>().ToArray();

			var result = new MapSectionResponse
			{
				Status = 0,          // Ok
				QueuePosition = -1,   // Unknown
				Counts = counts.Select(e => Convert.ToInt32(e)).ToArray()	// Convert the uints to ints for our client.
			};

			return result;
		}

	}
}
