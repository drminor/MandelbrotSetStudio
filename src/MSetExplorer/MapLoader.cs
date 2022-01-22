using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private MapSectionRequestProcessor _mapSectionRequestProcessor;

		public MapLoader(MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_mapSectionRequestProcessor = mapSectionRequestProcessor;
		}

		public void LoadMap(Job job, bool refreshMapSections, Action<MapSection> callback)
		{
			if (refreshMapSections)
			{
				_mapSectionRequestProcessor.ClearMapSections(job.Subdivision.Id.ToString());
			}

			GetSections(job.MSetInfo, job.Subdivision, job.CanvasSizeInBlocks, job.MapBlockOffset, callback);
		}

		public void Stop()
		{
			if (!(_mapSectionRequestProcessor is null))
			{
				_mapSectionRequestProcessor.Stop(immediately: false);
			}
		}

		private SizeInt _blockSize;
		private SizeInt _mapBlockOffset;
		private Action<MapSection> _callback;
		private ColorMap _colorMap;

		private void GetSections(MSetInfo mSetInfo, Subdivision subdivision, SizeInt canvasSizeInBlocks, SizeInt mapBlockOffset, Action<MapSection> callback)
		{
			_blockSize = subdivision.BlockSize;
			_mapBlockOffset = mapBlockOffset;
			_callback = callback;

			_colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);

			for (var yBlockPtr = 0; yBlockPtr < canvasSizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < canvasSizeInBlocks.Width ; xBlockPtr++)
				{
					// Translate to subdivision coordinates.
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr).Translate(mapBlockOffset);
					var mapSectionRequest = MapSectionHelper.CreateRequest(subdivision, blockPosition, mSetInfo.MapCalcSettings);
					_mapSectionRequestProcessor.AddWork(mapSectionRequest, HandleResponse);
				}
			}
		}

		private void HandleResponse(MapSectionResponse mapSectionResponse)
		{
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, _blockSize, _colorMap);

			// Translate subdivision coordinates to screen coordinates.
			var position = mapSectionResponse.BlockPosition.Diff(_mapBlockOffset).Scale(_blockSize);
			var mapSection = new MapSection(position, _blockSize, pixels1d);

			_callback(mapSection);
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
