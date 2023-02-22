using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

			// Counts
			var counts = GetCounts(iterationState, requestStruct.RowNumber);
			var countsBuffer = Marshal.AllocCoTaskMem(counts.Length);
			Marshal.Copy(counts, 0, countsBuffer, counts.Length);

			// Make the call -- TODO: return allRowSamplesHaveEscaped
			NativeMethods.GenerateMapSection(requestStruct, countsBuffer);

			// Counts
			Marshal.Copy(countsBuffer, counts, 0, counts.Length);
			Marshal.FreeCoTaskMem(countsBuffer);

			var allRowSamplesHaveEscaped = false;
			return allRowSamplesHaveEscaped;
		}

		private static byte[] GetCounts(IIterationState iterationState, int rowNumber)
		{
			//for(var i = 0; i < 64; i++)
			//{
			//	iterationState.MapSectionVectors.Counts[i] = (ushort)i;
			//}

			var buffer = new byte[iterationState.MapSectionVectors.BytesPerRow * 2]; // Using Vector of uints, not ushorts
			iterationState.MapSectionVectors.FillCountsRow(rowNumber, buffer);

			return buffer;

		}


	}
}
