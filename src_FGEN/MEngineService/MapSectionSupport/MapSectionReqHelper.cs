using MEngineDataContracts;
using System;
using System.Runtime.InteropServices;

namespace MEngineService
{
	internal class MapSectionReqHelper
	{
		public MapSectionRequestStruct GetRequestStruct(MapSectionRequest mapSectionRequest)
		{
			MapSectionRequestStruct result = new MapSectionRequestStruct();

			result.subdivisionId = mapSectionRequest.SubdivisionId;

			// BlockPosition
			result.blockPositionX = mapSectionRequest.BlockPosition.X;
			result.blockPositionY = mapSectionRequest.BlockPosition.Y;

			// RPointDto Position
			result.positionX = mapSectionRequest.Position.X;
			result.positionY = mapSectionRequest.Position.Y;
			result.positionExponent = mapSectionRequest.Position.Exponent;

			// BlockSize
			result.blockSizeWidth = mapSectionRequest.BlockSize.Width;
			result.blockSizeHeight = mapSectionRequest.BlockSize.Height;

			// RSizeDto SamplePointsDelta;
			result.samplePointDeltaWidth = mapSectionRequest.SamplePointsDelta.Width;
			result.samplePointDeltaHeight = mapSectionRequest.SamplePointsDelta.Height;
			result.samplePointDeltaExponent = mapSectionRequest.SamplePointsDelta.Exponent;

			// MapCalcSettings
			result.maxIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			result.threshold = mapSectionRequest.MapCalcSettings.Threshold;
			result.iterationsPerStep = mapSectionRequest.MapCalcSettings.RequestsPerJob;

			return result;
		}


		public void PrepareResultArray(int[] array2, int size)
		{
			IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(size) * array2.Length);
			Marshal.Copy(array2, 0, buffer, array2.Length);

			//int sum2 = NativeMethods.TestRefArrayOfInts(ref buffer, ref size);
			//Console.WriteLine("\nSum of elements:" + sum2);
			if (size > 0)
			{
				int[] arrayRes = new int[size];
				Marshal.Copy(buffer, arrayRes, 0, size);
				Marshal.FreeCoTaskMem(buffer);
			}
		}

	}
}
