﻿using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ImageBuilder
{
	public class PngBuilder
	{
		private const int VALUE_FACTOR = 10000;

		private readonly string _imageOutputFolder;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly IMEngineClient _mEngineClient;

		private readonly MapSectionHelper _mapSectionHelper;

		public PngBuilder(string imageOutputFolder, IMEngineClient mEngineClient, IMapSectionAdapter mapSectionAdapter)
		{
			_imageOutputFolder = imageOutputFolder;
			_mEngineClient = mEngineClient;
			_mapSectionAdapter = mapSectionAdapter;
			_mapSectionHelper = new MapSectionHelper();
		}

		public void Build(Poster poster, bool useEscapeVelocities)
		{
			var projectName = poster.Name;

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
			var imagePath = GetImageFilename(projectName, imageSize.Width, _imageOutputFolder);

			Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {imageSizeInBlocks}. The ColorMap has Id: {poster.ColorBandSet.Id}.");

			using var pngImage = new PngImage(imagePath, imageSize.Width, imageSize.Height);

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
						var countsForThisLine = GetOneLineFromCountsBlock(mapSectionResponse?.Counts, lPtr, blockSize.Width); // mapSectionReader.GetCounts(key, lPtr);

						if (countsForThisLine != null)
						{
							BuildPngImageLineSegment(hBPtr * blockSize.Width, countsForThisLine, iLine, colorMap);
						}
						else
						{
							BuildBlankPngImageLineSegment(hBPtr * blockSize.Width, blockSize.Width, iLine);
						}

						//BuildPngImageLineSegment(hBPtr * blockSize.Width, countsForThisLine, iLine, maxIterations, colorMap);
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

		//private int[] GetOneLineFromCountsBlock(int[] counts, int lPtr)
		//{
		//	int[] result = new int[_blockSize.Width];

		//	Array.Copy(counts, lPtr * _blockSize.Width, result, 0, _blockSize.Width);
		//	return result;
		//}


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

				//var escapeVelocity = GetEscVel(counts[xPtr], out var cnt);


				//if (cnt == maxIterations)
				//{
				//	cComps = colorMap.HighColorBand.StartColor.ColorComps ?? new byte[] { 000000 };
				//}
				//else
				//{
				//	cComps = colorMap.GetColor(cnt, escapeVelocity);
				//}

				colorMap.PlaceColor(countVal, escapeVelocity, dest);

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[0], cComps[1], cComps[2]);
			}
		}

		private void BuildBlankPngImageLineSegment(int pixPtr, int len, ImageLine iLine)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		//private double GetEscVel(int rawCount, out int count)
		//{
		//	var result = rawCount / 10000d;
		//	count = (int)Math.Truncate(result);
		//	result -= count;
		//	return result;
		//}

		private string GetImageFilename(string fn, int imageWidth, string basePath)
		{
			var result = Path.Combine(basePath, $"{fn}_{imageWidth}_v1.png");
			return result;
		}

	}
}

