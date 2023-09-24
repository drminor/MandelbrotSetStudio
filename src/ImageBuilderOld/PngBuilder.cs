using MSS.Types;
using PngImageLib;
using System;
using System.IO;
using MSS.Common;
using MSS.Types.MSetOld;
using ImageBuilder;

namespace ImageBuilderOld
{
	public class PngBuilder
	{
		private readonly SizeInt _blockSize;
		private readonly string _imageOutputFolder;

		public PngBuilder(string imageOutputFolder, SizeInt blockSize)
		{
			_imageOutputFolder = imageOutputFolder;
			_blockSize = blockSize;
		}

		public void Build(MSetInfoOld mSetInfo, IMapSectionReader mapSectionReader)
		{
			var projectName = mSetInfo.Name;
			var isHighRes = mSetInfo.IsHighRes;
			var maxIterations = mSetInfo.MapCalcSettings.TargetIterations;
			var colorMap = mSetInfo.ColorMap;

			var imageSizeInBlocks = mapSectionReader.GetImageSizeInBlocks();

			var w = imageSizeInBlocks.Width;
			var h = imageSizeInBlocks.Height;

			var imageSize = imageSizeInBlocks.Scale(_blockSize);

			var imagePath = GetImageFilename(projectName, imageSize.Width, isHighRes, _imageOutputFolder);

			var key = new KPoint(0, 0);

			using var pngImage = new PngImage(imagePath, imageSize.Width, imageSize.Height);
			for (var vBPtr = 0; vBPtr < h; vBPtr++)
			{
				key.Y = vBPtr;
				for (var lPtr = 0; lPtr < 100; lPtr++)
				{
					var iLine = pngImage.ImageLine;

					for (var hBPtr = 0; hBPtr < w; hBPtr++)
					{
						key.X = hBPtr;

						var countsForThisLine = mapSectionReader.GetCounts(key, lPtr);
						if (countsForThisLine != null)
						{
							BuildPngImageLineSegment(hBPtr * _blockSize.Width, countsForThisLine, iLine, maxIterations, colorMap);
						}
						else
						{
							BuildBlankPngImageLineSegment(hBPtr * _blockSize.Width, _blockSize.Width, iLine);
						}
					}

					pngImage.WriteLine(iLine);
				}
			}
		}

		public static void BuildPngImageLineSegment(int pixPtr, int[] counts, ImageLine iLine, int maxIterations, ColorMap colorMap)
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

		public static void BuildBlankPngImageLineSegment(int pixPtr, int len, ImageLine iLine)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		private static double GetEscVel(int rawCount, out int count)
		{
			var result = rawCount / 10000d;
			count = (int)Math.Truncate(result);
			result -= count;
			return result;
		}

		private static string GetImageFilename(string fn, int imageWidth, bool isHighRes, string basePath)
		{
			string imagePath;
			if (isHighRes)
			{
				imagePath = Path.Combine(basePath, $"{fn}_hrez_{imageWidth}_test.png");
			}
			else
			{
				imagePath = Path.Combine(basePath, $"{fn}_{imageWidth}_test.png");
			}

			return imagePath;
		}

	}
}
