using MEngineDataContracts;
using MEngineService.Services;
using MongoDB.Bson;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MEngineService
{
	public static class MapSectionGenerator
	{
		public static async Task<MapSectionResponse> GenerateMapSectionAsync(MapSectionRequest mapSectionRequest)
		{
			ushort[] counts = GetCounts(mapSectionRequest);
			ushort[] escapeVelocities = GetEscapeVelocities(mapSectionRequest);

			var countsAndEscVels = GetCountsAndEscVelsBuffer(counts, escapeVelocities, out var countsAndEscVelsBuffer);

			// Done Flags
			byte[] doneFlagsAsBArray = ConvertDoneFlags(mapSectionRequest);
			var doneFlagsBuffer = GetDoneFlagsBuffer(doneFlagsAsBArray);

			// ZValues

			var zValsAndBuffer = await GetAndFillZValuesBufferAsync(mapSectionRequest);

			var zValues = zValsAndBuffer.Item1;
			var zValuesBuffer = zValsAndBuffer.Item2;


			/* ***********************************
			 *		Make the call 
			 *		using the filled buffers.
			 **************************************/
			var requestStruct = MapSectionReqHelper.GetRequestStruct(mapSectionRequest);
			NativeMethods.GenerateMapSection(requestStruct, countsAndEscVelsBuffer, doneFlagsBuffer, zValuesBuffer);

			// Counts
			Marshal.Copy(countsAndEscVelsBuffer, countsAndEscVels, 0, countsAndEscVels.Length);
			Marshal.FreeCoTaskMem(countsAndEscVelsBuffer);

			MapSectionRequest.SplitCountsAndEscapeVelocities(countsAndEscVels, new Span<ushort>(counts), new Span<ushort>(escapeVelocities));

			// Done Flags
			Marshal.Copy(doneFlagsBuffer, doneFlagsAsBArray, 0, doneFlagsAsBArray.Length);
			Marshal.FreeCoTaskMem(doneFlagsBuffer);
			var doneFlags = CompressDoneFlags(doneFlagsAsBArray);

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
				EscapeVelocities = escapeVelocities,
				DoneFlags = doneFlags,
				ZValues = zValues
			};

			return result;
		}

		private static int[] GetCountsAndEscVelsBuffer(ushort[] counts, ushort[] escapeVelocities, out IntPtr countsAndEscVelsBuffer)
		{
			var countsPlusEscVels = MapSectionRequest.CombineCountsAndEscapeVelocities(counts, escapeVelocities);

			countsAndEscVelsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int)) * countsPlusEscVels.Length);
			Marshal.Copy(countsPlusEscVels, 0, countsAndEscVelsBuffer, counts.Length);

			return countsPlusEscVels;
		}

		private static ushort[] GetCounts(MapSectionRequest mapSectionRequest)
		{
			ushort[] counts;

			if (mapSectionRequest.Counts != null)
			{
				counts = mapSectionRequest.Counts;
			}
			else
			{
				counts = new ushort[mapSectionRequest.BlockSize.NumberOfCells];
			}

			return counts;
		}

		private static ushort[] GetEscapeVelocities(MapSectionRequest mapSectionRequest)
		{
			ushort[] escapeVelocities;

			if (mapSectionRequest.EscapeVelocities != null)
			{
				escapeVelocities = mapSectionRequest.EscapeVelocities;
			}
			else
			{
				escapeVelocities = new ushort[mapSectionRequest.BlockSize.NumberOfCells];
			}

			return escapeVelocities;
		}

		private static byte[] ConvertDoneFlags(MapSectionRequest mapSectionRequest)
		{
			byte[] doneFlagsAsBArray;

			if (mapSectionRequest.DoneFlags != null)
			{
				if (mapSectionRequest.DoneFlags.Length == 1)
				{
					if (mapSectionRequest.DoneFlags[0])
					{
						throw new InvalidOperationException("If all of the DoneFlags are true, then no request should be sent.");
					}

					var df = mapSectionRequest.DoneFlags[0] ? (byte)1 : (byte)0;
					//var df = (byte)0;
					doneFlagsAsBArray = Enumerable.Repeat(df, mapSectionRequest.BlockSize.NumberOfCells).ToArray();
				}
				else
				{
					doneFlagsAsBArray = mapSectionRequest.DoneFlags.Select(x => x ? (byte)1 : (byte)0).ToArray();
				}
			}
			else
			{
				doneFlagsAsBArray = new byte[mapSectionRequest.BlockSize.NumberOfCells];
			}

			return doneFlagsAsBArray;
		}

		private static bool[] CompressDoneFlags(byte[] doneFlags)
		{
			bool[] result;

			if (!doneFlags.Any(x => x != 1))
			{
				// All ones
				result = new bool[1] { true };
			}
			else if (!doneFlags.Any(x => x != 0))
			{
				// all Zeros
				result = new bool[1] { false };
			}
			else
			{
				// Mix
				result = doneFlags.Select(x => x == 1).ToArray();
			}


			//bool[] result;

			//var currentVal = doneFlags[0];
			//bool? allTheSame = null;

			//if (currentVal != 0 && currentVal != 1)
			//{
			//	throw new InvalidOperationException($"Expecting a 1 or a 0, but got {currentVal}.");
			//}

			//for (var i = 0; i < doneFlags.Length; i++)
			//{
			//	if (doneFlags[i] != currentVal)
			//	{
			//		allTheSame = false;
			//		break;
			//	}

			//	allTheSame = true;
			//}

			//if (!allTheSame.HasValue)
			//{
			//	throw new InvalidOperationException("The local var 'AllTheSame' has not been assigned.");
			//}
			//else
			//{
			//	if (allTheSame.Value)
			//	{
			//		result = new bool[1] { currentVal == 1 };
			//	}
			//	else
			//	{
			//		result = doneFlags.Select(x => x == 1).ToArray();
			//	}
			//}

			//var result = doneFlags.Select(x => x == 1).ToArray();
			return result;
		}

		private static IntPtr GetDoneFlagsBuffer(byte[] doneFlagsAsBArray)
		{
			Debug.Assert(Marshal.SizeOf(typeof(byte)) == 1, "Byte length is not 1.");

			IntPtr doneFlagsBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(byte)) * doneFlagsAsBArray.Length);
			Marshal.Copy(doneFlagsAsBArray, 0, doneFlagsBuffer, doneFlagsAsBArray.Length);

			return doneFlagsBuffer;
		}

		private static async Task<ValueTuple<double[], IntPtr>> GetAndFillZValuesBufferAsync(MapSectionRequest mapSectionRequest)
		{
			double[] zValues;

			if (mapSectionRequest.MapSectionId != null)
			{
				// Fetch the zvalues from the repo.
				var mapSectionObjId = new ObjectId(mapSectionRequest.MapSectionId);

				var zValuesObject = await MapSectionService.MapSectionAdapter.GetMapSectionZValuesAsync(mapSectionObjId);

				if (zValuesObject != null)
				{
					zValues = zValuesObject.GetZValuesAsDoubleArray();
				}
				else
				{
					throw new InvalidOperationException($"Could not retrieve the ZValues from the repo for MapSectionId: {mapSectionRequest.MapSectionId}.");
					//Debug.WriteLine($"WARNING: Could not retrieve the ZValues from the repo for MapSectionId: {mapSectionRequest.MapSectionId}.");
					//zValues = new double[mapSectionRequest.BlockSize.NumberOfCells * 4];
				}
			}
			else
			{
				zValues = new double[mapSectionRequest.BlockSize.NumberOfCells * 4];
			}


			var zValuesBuffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(double)) * zValues.Length);
			Marshal.Copy(zValues, 0, zValuesBuffer, zValues.Length);

			return new ValueTuple<double[], IntPtr>(zValues, zValuesBuffer);
		}

	}
}
