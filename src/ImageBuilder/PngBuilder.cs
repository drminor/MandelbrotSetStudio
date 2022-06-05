using MEngineDataContracts;
using MSS.Common;
using MSS.Common.MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ImageBuilder
{
	public class PngBuilder
	{
		private const int VALUE_FACTOR = 10000;

		private readonly IMapSectionAdapter _mapSectionAdapter;
		//private readonly IMEngineClient _mEngineClient;
		//private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionHelper _mapSectionHelper;

		//private int _cntr = 0;

		//public PngBuilder(IMEngineClient mEngineClient, IMapSectionAdapter mapSectionAdapter, IMapLoaderManager mapLoaderManager)
		public PngBuilder(IMEngineClient _, IMapSectionAdapter mapSectionAdapter, IMapLoaderManager __)
		{
			//_mEngineClient = mEngineClient;
			_mapSectionAdapter = mapSectionAdapter;
			//_mapLoaderManager = mapLoaderManager;

			_mapSectionHelper = new MapSectionHelper();
		}

		//public void BuildPrep(Poster poster, bool useEscapeVelocities)
		//{
		//	var jobAreaInfo = poster.JobAreaInfo;
		//	var mapCalcSettings = poster.MapCalcSettings;
		//	var jobAreaAndCalcSettings = new JobAreaAndCalcSettings(jobAreaInfo, mapCalcSettings);

		//	_mapLoaderManager.MapSectionReady += MapSectionReady;

		//	_mapLoaderManager.Push(jobAreaAndCalcSettings);
		//}

		// TODO: Have the Poster specify whether or not to use EscapeVelocities
		public void Build(string imageFilePath, Poster poster, bool useEscapeVelocities)
		{
			var jobAreaInfo = poster.JobAreaInfo;
			var mapCalcSettings = poster.MapCalcSettings;

			var canvasSize = jobAreaInfo.CanvasSize;
			var blockSize = jobAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(poster.ColorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			var imageSizeInBlocks = RMapHelper.GetMapExtentInBlocks(canvasSize, blockSize);
			var imageSize = imageSizeInBlocks.Scale(blockSize);

			Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {imageSizeInBlocks}. The ColorMap has Id: {poster.ColorBandSet.Id}.");

			using var pngImage = new PngImage(imageFilePath, imageSize.Width, imageSize.Height);

			var w = imageSizeInBlocks.Width;
			var h = imageSizeInBlocks.Height;

			for (var vBPtr = 0; vBPtr < h; vBPtr++)
			{
				var blocksForThisRow = GetAllBlocksForRow(vBPtr, w, jobAreaInfo.MapBlockOffset, jobAreaInfo.Subdivision, mapCalcSettings);

				for (var lPtr = 0; lPtr < blockSize.Height; lPtr++)
				{
					var iLine = pngImage.ImageLine;

					for (var hBPtr = 0; hBPtr < w; hBPtr++)
					{
						var mapSectionResponse = blocksForThisRow[hBPtr];
						var countsForThisLine = GetOneLineFromCountsBlock(mapSectionResponse?.Counts, lPtr, blockSize.Width);

						if (countsForThisLine != null)
						{
							BuildPngImageLineSegment(hBPtr * blockSize.Width, countsForThisLine, iLine, colorMap);
						}
						else
						{
							BuildBlankPngImageLineSegment(hBPtr * blockSize.Width, blockSize.Width, iLine);
						}
					}

					pngImage.WriteLine(iLine);
				}
			}
		}

		private IDictionary<int, MapSectionResponse?> GetAllBlocksForRow(int rowPtr, int stride, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var result = new Dictionary<int, MapSectionResponse?>();

			for (var colPtr = 0; colPtr < stride; colPtr++)
			{
				var key = new PointInt(colPtr, rowPtr);
				var mapSectionRequest = _mapSectionHelper.CreateRequest(key, mapBlockOffset, subdivision, mapCalcSettings);
				var mapSectionResponse = GetMapSection(mapSectionRequest);

				result.Add(colPtr, mapSectionResponse);
			}

			return result;
		}

		private MapSectionResponse? GetMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapSectionResponse = _mapSectionAdapter.GetMapSection(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition);

			//if (mapSectionResponse == null)
			//{
			//	mapSectionResponse = _mEngineClient.GenerateMapSection(mapSectionRequest);
			//}

			return mapSectionResponse;
		}

		private int[]? GetOneLineFromCountsBlock(int[]? counts, int lPtr, int stride)
		{
			if (counts == null)
			{
				return null;
			}
			else
			{
				int[] result = new int[stride];

				Array.Copy(counts, lPtr * stride, result, 0, stride);
				return result;
			}
		}

		private void BuildPngImageLineSegment(int pixPtr, int[] counts, ImageLine iLine, ColorMap colorMap)
		{
			var cComps = new byte[4];
			var dest = new Span<byte>(cComps);

			for (var xPtr = 0; xPtr < counts.Length; xPtr++)
			{
				var countVal = counts[xPtr];
				countVal = Math.DivRem(countVal, VALUE_FACTOR, out var ev);

				//var escapeVel = useEscapeVelocities ? Math.Max(1, ev / (double)VALUE_FACTOR) : 0;
				var escapeVelocity = colorMap.UseEscapeVelocities ? ev / (double)VALUE_FACTOR : 0;

				if (escapeVelocity > 1.0)
				{
					Debug.WriteLine($"The Escape Velocity is greater that 1.0");
				}

				colorMap.PlaceColor(countVal, escapeVelocity, dest);

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[2], cComps[1], cComps[0]);
			}
		}

		private void BuildBlankPngImageLineSegment(int pixPtr, int len, ImageLine iLine)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		//private void MapSectionReady(object? sender, Tuple<MapSection, int> e)
		//{
		//	if (++_cntr % 10 == 0)
		//	{
		//		Debug.WriteLine($"Received {_cntr} map sections.");
		//	}
		//}

	}
}

