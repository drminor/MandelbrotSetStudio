﻿using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private readonly string _mEngineEndPointAddress;
		private Task _generateMapSectionsTask;

		public MainWindowViewModel(string mEngineEndPointAddress)
		{
			_mEngineEndPointAddress = mEngineEndPointAddress;
			_generateMapSectionsTask = null;

		}

		public Subdivision Subdivision { get; private set; }

		public bool IsTaskComplete => _generateMapSectionsTask is null;

		public void GenerateMapSections(MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			if (!IsTaskComplete)
			{
				throw new InvalidOperationException("Cannot call GenerateMapSections until the current task is complete.");
			}

			_generateMapSectionsTask = GetSectionsAsync(mSetInfo, progress);
		}

		public async Task GetSectionsAsync(MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			var dtoMapper = new DtoMapper();
			var mClient = new MClient(_mEngineEndPointAddress);

			Subdivision = MSetInfoHelper.GetSubdivision(mSetInfo);

			var colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss); 

			var yCoordNumerartor = new BigInteger(-3);

			var numVertBlocks = 6;
			var numHoriBlocks = 6;

			for (var yBlockPtr = 0; yBlockPtr < numVertBlocks; yBlockPtr++)
			{
				var xCoordNumerator = new BigInteger(-4);
				for (var xBlockPtr = 0; xBlockPtr < numHoriBlocks; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var mapSectionRequest = new MapSectionRequest
					{
						SubdivisionId = Subdivision.Id.ToString(),
						BlockPosition = blockPosition,
						BlockSize = RMapConstants.BLOCK_SIZE,
						Position = dtoMapper.MapTo(new RPoint(xCoordNumerator++, yCoordNumerartor, -1)),
						SamplePointsDelta = dtoMapper.MapTo(Subdivision.SamplePointDelta),
						MapCalcSettings = mSetInfo.MapCalcSettings
					};

					var mapSectionResponse = await mClient.GenerateMapSectionAsync(mapSectionRequest);

					var pixels1d = GetPixelArray(mapSectionResponse.Counts, Subdivision.BlockSize, colorMap);

					var mapSection = new MapSection(Subdivision, mapSectionResponse.BlockPosition, pixels1d);
					progress.Report(mapSection);
				}
				yCoordNumerartor++;
			}

			_generateMapSectionsTask = null;
		}

		private byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap)
		{
			var numberofCells = blockSize.NumberOfCells;
			var result = new byte[4 * numberofCells];

			for(var rowPtr = 0; rowPtr < blockSize.Height; rowPtr++)
			{
				var resultRowPtr = -1 + blockSize.Height - rowPtr;
				var curResultPtr = resultRowPtr * blockSize.Width * 4;

				var curSourcePtr = rowPtr * blockSize.Width;

				for (var colPtr = 0; colPtr < blockSize.Width; colPtr++)
				{
					var countVal = counts[curSourcePtr++];
					countVal = Math.DivRem(countVal, 1000, out var ev);
					var escapeVel = ev / 1000d;

					var colorComps = colorMap.GetColor(countVal, escapeVel);

					for (var j = 2; j > -1; j--)
					{
						result[curResultPtr++] = colorComps[j];
					}
					result[curResultPtr++] = 255;
				}
			}

			return result;
		}


	}
}
