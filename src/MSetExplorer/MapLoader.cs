using MapSectionProviderLib;
using MEngineClient;
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
		private MapSectionRequestQueue _mapSectionRequstQueue;

		public MapLoader(MapSectionRequestQueue mapSectionRequestQueue)
		{
			_mapSectionRequstQueue = mapSectionRequestQueue;
		}

		public void LoadMap(Job job, Action<MapSection> callback)
		{
			GetSections(job.MSetInfo, job.Subdivision, job.CanvasOffset, callback);
		}

		public void Stop()
		{
			if (!(_mapSectionRequstQueue is null))
			{
				_mapSectionRequstQueue.Stop(immediately: false);
			}
		}

		private SizeInt _blockSize;
		private ColorMap _colorMap;
		private PointDbl _canvasOffset;
		private Action<MapSection> _callback;

		private void GetSections(MSetInfo mSetInfo, Subdivision subdivision, PointDbl canvasOffset, Action<MapSection> callback)
		{
			_blockSize = subdivision.BlockSize;
			_colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);
			_canvasOffset = canvasOffset;
			_callback = callback;

			SizeInt blockSize = subdivision.BlockSize;

			for (var yBlockPtr = -3; yBlockPtr < 3; yBlockPtr++)
			{
				for (var xBlockPtr = -4; xBlockPtr < 2; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);

					var mapSectionRequest = MapSectionHelper.CreateRequest(subdivision, blockPosition, mSetInfo.MapCalcSettings);

					_mapSectionRequstQueue.AddWork(mapSectionRequest, HandleResponse);
				}
			}
		}

		private void HandleResponse(MapSectionResponse mapSectionResponse)
		{
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, _blockSize, _colorMap);
			var position = new PointDbl(mapSectionResponse.BlockPosition).Scale(_blockSize).Translate(_canvasOffset);
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
