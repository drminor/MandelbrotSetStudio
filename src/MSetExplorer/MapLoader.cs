using MapSectionProviderLib;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private readonly MapSectionProvider _mapSectionProvider;

		public MapLoader(MapSectionProvider mapSectionProvider)
		{
			_mapSectionProvider = mapSectionProvider;
		}

		public Task LoadMap(Job job, Action<MapSection> callback)
		{
			var task = GetSectionsAsync(job.MSetInfo, job.Subdivision, callback);
			return task;
		}

		public async Task GetSectionsAsync(MSetInfo mSetInfo, Subdivision subdivision, Action<MapSection> callback)
		{
			var colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);

			SizeInt blockSize = subdivision.BlockSize;

			for (var yBlockPtr = -3; yBlockPtr < 3; yBlockPtr++)
			{
				for (var xBlockPtr = -3; xBlockPtr < 3; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);

					var mapSectionResponse = await _mapSectionProvider.GenerateMapSectionAsync(subdivision, blockPosition, mSetInfo.MapCalcSettings);

					var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, colorMap);

					DPoint canvasPosition = new DPoint(
						384 + mapSectionResponse.BlockPosition.X * blockSize.Width,
						384 + mapSectionResponse.BlockPosition.Y * blockSize.Height
						);

					var mapSection = new MapSection(subdivision, canvasPosition, pixels1d);
					callback(mapSection);
				}
			}
		}

		private byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap)
		{
			var numberofCells = blockSize.NumberOfCells;
			var result = new byte[4 * numberofCells];

			for (var rowPtr = 0; rowPtr < blockSize.Height; rowPtr++)
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
