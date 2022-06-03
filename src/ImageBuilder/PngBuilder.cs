using MEngineClient;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using PngImageLib;
using System;
using System.Diagnostics;
using System.IO;

namespace ImageBuilder
{
	public class PngBuilder
	{
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

		public void Build(Poster poster)
		{
			var projectName = poster.Name;

			var jobAreaInfo = poster.JobAreaInfo;
			var mapCalcSettings = poster.MapCalcSettings;

			var canvasSize = jobAreaInfo.CanvasSize;
			var maxIterations = mapCalcSettings.TargetIterations;
			var blockSize = jobAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(poster.ColorBandSet);

			var imageSizeInBlocks = RMapHelper.GetMapExtentInBlocks(canvasSize, blockSize);
			var imageSize = imageSizeInBlocks.Scale(blockSize);
			var imagePath = GetImageFilename(projectName, imageSize.Width, _imageOutputFolder);

			Debug.WriteLine($"Processing section requests. The map extent is {imageSizeInBlocks}.");
			using var pngImage = new PngImage(imagePath, imageSize.Width, imageSize.Height);

			var w = imageSizeInBlocks.Width;
			var h = imageSizeInBlocks.Height;

			var key = new PointInt();
			for (var vBPtr = 0; vBPtr < h; vBPtr++)
			{
				key.Y = vBPtr;
				for (var lPtr = 0; lPtr < blockSize.Height; lPtr++)
				{
					var iLine = pngImage.ImageLine;

					for (var hBPtr = 0; hBPtr < w; hBPtr++)
					{
						key.X = hBPtr;
						var mapSectionRequest = _mapSectionHelper.CreateRequest(key, jobAreaInfo.MapBlockOffset, jobAreaInfo.Subdivision, mapCalcSettings);
						var mapSectionResponse = GetMapSection(mapSectionRequest);
						var countsForThisLine = GetOneLineFromCountsBlock(mapSectionResponse.Counts, lPtr); // mapSectionReader.GetCounts(key, lPtr);

						//if (countsForThisLine != null)
						//{
						//	BuildPngImageLineSegment(hBPtr * blockSize.Width, countsForThisLine, iLine, maxIterations, colorMap);
						//}
						//else
						//{
						//	BuildBlankPngImageLineSegment(hBPtr * blockSize.Width, blockSize.Width, iLine);
						//}

						BuildPngImageLineSegment(hBPtr * blockSize.Width, countsForThisLine, iLine, maxIterations, colorMap);
					}

					pngImage.WriteLine(iLine);
				}
			}
		}

		private MapSectionResponse GetMapSection(MapSectionRequest mapSectionRequest)
		{
			var mapSectionResponse = _mapSectionAdapter.GetMapSection(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition);

			if (mapSectionResponse == null)
			{
				mapSectionResponse = _mEngineClient.GenerateMapSection(mapSectionRequest);
			}

			return mapSectionResponse;
		}

		private int[] GetOneLineFromCountsBlock(int[] counts, int lPtr)
		{
			var size = counts.Length;
			int[] result = new int[size];

			Array.Copy(counts, lPtr * size, result, 0, size);
			return result;
		}

		private void BuildPngImageLineSegment(int pixPtr, int[] counts, ImageLine iLine, int maxIterations, ColorMap colorMap)
		{
			for (var xPtr = 0; xPtr < counts.Length; xPtr++)
			{
				var escapeVelocity = GetEscVel(counts[xPtr], out var cnt);

				byte[] cComps;
				if (cnt == maxIterations)
				{
					cComps = colorMap.HighColorBand.StartColor.ColorComps ?? new byte[] { 000000 };
				}
				else
				{
					cComps = colorMap.GetColor(cnt, escapeVelocity);
				}

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[0], cComps[1], cComps[2]);
			}
		}

		//private void BuildBlankPngImageLineSegment(int pixPtr, int len, ImageLine iLine)
		//{
		//	for (var xPtr = 0; xPtr < len; xPtr++)
		//	{
		//		ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
		//	}
		//}

		private double GetEscVel(int rawCount, out int count)
		{
			var result = rawCount / 10000d;
			count = (int)Math.Truncate(result);
			result -= count;
			return result;
		}

		private string GetImageFilename(string fn, int imageWidth, string basePath)
		{
			var result = Path.Combine(basePath, $"{fn}_{imageWidth}_v1.png");
			return result;
		}

	}
}

