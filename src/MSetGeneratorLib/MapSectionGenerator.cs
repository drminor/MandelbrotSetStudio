using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Runtime.InteropServices;

namespace MSetGeneratorLib
{
	public static class MapSectionGenerator
	{
		#region GetStringVals

		//public static string[] GetStringVals(MapSectionRequest mapSectionRequest)
		//{
		//	var requestStruct = MapSectionReqHelper.GetRequestStruct(mapSectionRequest);
		//	//var px = new string('1', 200);
		//	//var py = new string('1', 200);
		//	//var dw = new string('1', 200);
		//	//var dh = new string('1', 200);

		//	NativeMethods.GetStringValues(requestStruct, out var px, out var py, out var dw, out var dh);

		//	return new string[] { px, py, dw, dh };
		//}

		#endregion

		public static bool GenerateMapSection(IIterationState iterationState, ApFixedPointFormat apFixedPointFormat, uint threshold, CancellationToken ct)
		{
			var requestStruct = MapSectionReqHelper.GetRequestStruct(iterationState, apFixedPointFormat, threshold);

			var counts = new short[requestStruct.blockSizeWidth];

			var countsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(short)) * counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);


			//for (var idx = 0; idx < iterationState.VectorsPerRow; idx++)
			//{
			//	var allSamplesHaveEscaped = GenerateMapCol(idx, iterator, ref iterationState);

			//	if (!allSamplesHaveEscaped)
			//	{
			//		allRowSamplesHaveEscaped = false;
			//	}
			//}


			NativeMethods.GenerateMapSection(requestStruct, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			//var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, doneFlags, zValues);


			var allRowSamplesHaveEscaped = false;
			return allRowSamplesHaveEscaped;
		}

		private static short[] GetCounts(MapSectionRequest mapSectionRequest)
		{
			short[] counts;

			if (mapSectionRequest.MapSectionVectors?.Counts != null)
			{
				counts = mapSectionRequest.MapSectionVectors.Counts.Cast<short>().ToArray();
			}
			else
			{
				counts = new short[mapSectionRequest.BlockSize.NumberOfCells];
			}

			return counts;
		}




	}
}
