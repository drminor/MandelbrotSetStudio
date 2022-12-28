using MEngineDataContracts;
using MSS.Common.DataTransferObjects;
using System.Diagnostics;

namespace MSetGenP
{
	public class MapSectionGeneratorVector
	{
		public MapSectionResponse GenerateMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapPositionDto = mapSectionRequest.Position;
			var samplePointDeltaDto = mapSectionRequest.SamplePointDelta;
			var blockSize = mapSectionRequest.BlockSize;
			var precision = mapSectionRequest.Precision;

			var targetIterations = mapSectionRequest.MapCalcSettings.TargetIterations;
			
			//var threshold = (uint) mapSectionRequest.MapCalcSettings.Threshold;
			uint threshold = 4;

			//var counts = mapSectionRequest.Counts;
			var counts = new ushort[blockSize.NumberOfCells];

			//var doneFlags = mapSectionRequest.DoneFlags;
			var doneFlags = new bool[blockSize.NumberOfCells];

			//var fixedPointFormat = new ApFixedPointFormat(bitsBeforeBinaryPoint: 8, minimumFractionalBits: precision);
			var fixedPointFormat = new ApFixedPointFormat(2);

			var smxMathHelper = new ScalarMath(fixedPointFormat, threshold);

			var dtoMapper = new DtoMapper();
			var mapPosition = dtoMapper.MapFrom(mapPositionDto);
			var samplePointDelta = dtoMapper.MapFrom(samplePointDeltaDto);

			var startingCx = smxMathHelper.CreateSmx(mapPosition.X);
			var startingCy = smxMathHelper.CreateSmx(mapPosition.Y);
			var delta = smxMathHelper.CreateSmx(samplePointDelta.Width);

			var s1 = startingCx.GetStringValue();
			var s2 = startingCy.GetStringValue();
			var s3 = delta.GetStringValue();

			var blockPos = mapSectionRequest.BlockPosition;

			Debug.WriteLine($"Value of C at origin: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). Delta: {s3}. Precision: {startingCx.Precision}, BP: {blockPos}");
			Debug.WriteLine($"Starting : BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}.");


			if (startingCx.Sign && startingCy.Sign)
			{
				var escapeVelocities = new ushort[blockSize.NumberOfCells];
				var compressedDoneFlags = CompressTheDoneFlags(doneFlags);

				var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
				return result;

			}

			//var cRs = smxMathHelper.BuildMapPoints(startingCx, startingCy, delta, blockSize, out var cIs);

			var stride = (byte)blockSize.Width;
			var samplePointOffsets = smxMathHelper.BuildSamplePointOffsets(delta, stride);
			var samplePointsX = smxMathHelper.BuildSamplePoints(startingCx, samplePointOffsets);
			var samplePointsY = smxMathHelper.BuildSamplePoints(startingCy, samplePointOffsets);

			var use2CVersion = true;

			if (use2CVersion)
			{
				var subSectionGeneratorVector = new SubSectionGeneratorVector2C(fixedPointFormat, targetIterations, threshold);

				var samplePointsX2C = subSectionGeneratorVector.Convert(samplePointsX);
				var samplePointsY2C = subSectionGeneratorVector.Convert(samplePointsY);

				var cRs = new FPValues(samplePointsX2C);

				for (int j = 0; j < samplePointsY.Length; j++)
				{
					var cIs = new FPValues(samplePointsY2C[j], stride);
					//Array.Copy(doneFlags, j * stride, rowDoneFlags, 0, stride);
					var rowCounts = subSectionGeneratorVector.GenerateMapSection(cRs, cIs, out var rowDoneFlags);
					Array.Copy(rowCounts, 0, counts, j * stride, stride);
					Array.Copy(rowDoneFlags, 0, doneFlags, j * stride, stride);
				}

				//Debug.WriteLine($"Completed: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). ACarries: {aCarries}, MCarries:{mCarries}.");
				//Debug.WriteLine($"Completed: BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. ACarries: {subSectionGeneratorVector.NumberOfACarries}, MCarries:{subSectionGeneratorVector.NumberOfMCarries}.");
				Debug.WriteLine($"{s1}, {s2}: Adds: {subSectionGeneratorVector.NumberOfACarries}\tSubtracts: {subSectionGeneratorVector.NumberOfMCarries}.");

				var escapeVelocities = new ushort[blockSize.NumberOfCells];
				var compressedDoneFlags = CompressTheDoneFlags(doneFlags);

				if (compressedDoneFlags.Length != 1 || !compressedDoneFlags[0])
				{
					Debug.WriteLine("WARNING: Some sample points are not complete.");
				}

				var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
				return result;

			}
			else
			{
				var cRs = new FPValues(samplePointsX);

				var subSectionGeneratorVector = new SubSectionGeneratorVector(fixedPointFormat, targetIterations, threshold);

				for (int j = 0; j < samplePointsY.Length; j++)
				{
					var cIs = new FPValues(samplePointsY[j], stride);
					//Array.Copy(doneFlags, j * stride, rowDoneFlags, 0, stride);
					var rowCounts = subSectionGeneratorVector.GenerateMapSection(cRs, cIs, out var rowDoneFlags);
					Array.Copy(rowCounts, 0, counts, j * stride, stride);
					Array.Copy(rowDoneFlags, 0, doneFlags, j * stride, stride);
				}

				//Debug.WriteLine($"Completed: real: {s1} ({startingCx}), imaginary: {s2} ({startingCy}). ACarries: {aCarries}, MCarries:{mCarries}.");
				//Debug.WriteLine($"Completed: BP: {blockPos}. Real: {s1}, {s2}. Delta: {s3}. ACarries: {subSectionGeneratorVector.NumberOfACarries}, MCarries:{subSectionGeneratorVector.NumberOfMCarries}.");
				Debug.WriteLine($"{s1}, {s2}: Adds: {subSectionGeneratorVector.NumberOfACarries}\tSubtracts: {subSectionGeneratorVector.NumberOfMCarries}.");

				var escapeVelocities = new ushort[blockSize.NumberOfCells];
				var compressedDoneFlags = CompressTheDoneFlags(doneFlags);

				if (compressedDoneFlags.Length != 1 || !compressedDoneFlags[0])
				{
					Debug.WriteLine("WARNING: Some sample points are not complete.");
				}

				var result = new MapSectionResponse(mapSectionRequest, counts, escapeVelocities, compressedDoneFlags, zValues: null);
				return result;

			}
		}

		private bool[] CompressTheDoneFlags(bool[] doneFlags)
		{
			bool[] result;

			if (!doneFlags.Any(x => !x))
			{
				// All reached the target
				result = new bool[] { true };
			}
			else if (!doneFlags.Any(x => x))
			{
				// none reached the target
				result = new bool[] { false };
			}
			else
			{
				// Mix
				result = doneFlags;
			}

			return result;
		}

	}
}
