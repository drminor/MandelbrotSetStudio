using MSS.Types;
using PngImageLib;
using System;
using System.IO;
using MSS.Common;

namespace ImageBuilder
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

		public void Build(MSetInfo mSetInfo, IMapSectionReader mapSectionReader)
		{
			var projectName = mSetInfo.Name;
			var isHighRes = mSetInfo.IsHighRes;
			var maxIterations = mSetInfo.MaxIterations;
			var colorMap = mSetInfo.ColorMap;

			SizeInt imageSizeInBlocks = mapSectionReader.GetImageSizeInBlocks();

			int w = imageSizeInBlocks.Width;
			int h = imageSizeInBlocks.Height;

			// TODO: Define * operator for SizeInt
			var imageSize = new SizeInt(w * _blockSize.Width, h * _blockSize.Height);

			string imagePath = GetImageFilename(projectName, imageSize.Width, isHighRes, _imageOutputFolder);

			var key = new KPoint(0, 0);

			using PngImage pngImage = new PngImage(imagePath, imageSize.Width, imageSize.Height);
			for (int vBPtr = 0; vBPtr < h; vBPtr++)
			{
				key.Y = vBPtr;
				for (int lPtr = 0; lPtr < 100; lPtr++)
				{
					ImageLine iLine = pngImage.ImageLine;

					for (int hBPtr = 0; hBPtr < w; hBPtr++)
					{
						key.X = hBPtr;

						int[] countsForThisLine = mapSectionReader.GetCounts(key, lPtr);
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
			for (int xPtr = 0; xPtr < counts.Length; xPtr++)
			{
				double escapeVelocity = GetEscVel(counts[xPtr], out int cnt);

				int[] cComps;
				if (cnt == maxIterations)
				{
					cComps = colorMap.HighColorEntry.StartColor.ColorComps;
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
			for (int xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}

		private static double GetEscVel(int rawCount, out int count)
		{
			double result = rawCount / 10000d;
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
