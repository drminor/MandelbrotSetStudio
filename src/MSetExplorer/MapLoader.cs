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
		private readonly IMapSectionProvider _mapSectionProvider;

		public MapLoader(IMapSectionProvider mapSectionProvider)
		{
			_mapSectionProvider = mapSectionProvider;
		}

		public Task LoadMap(Job job, Action<MapSection> callback)
		{
			var task = GetSectionsAsync(job.MSetInfo, job.Subdivision, job.CanvasOffset, callback);
			return task;
		}

		public async Task GetSectionsAsync(MSetInfo mSetInfo, Subdivision subdivision, PointDbl canvasOffset, Action<MapSection> callback)
		{
			var colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);

			SizeInt blockSize = subdivision.BlockSize;

			for (var yBlockPtr = -3; yBlockPtr < 3; yBlockPtr++)
			{
				for (var xBlockPtr = -4; xBlockPtr < 2; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);

					var mapSectionResponse = await _mapSectionProvider.GenerateMapSectionAsync(subdivision, blockPosition, mSetInfo.MapCalcSettings);

					var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, colorMap);
					var position = new PointDbl(mapSectionResponse.BlockPosition).Scale(blockSize).Translate(canvasOffset);
					var mapSection = new MapSection(position, blockSize, pixels1d);

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
				// Calculate the array index for the beginning of this 
				// destination and source row.
				// The Destination's origin is at the top, left.
				// The Source's origin is at the bottom, left.

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
