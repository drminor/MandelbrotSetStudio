using FSTypes;
using MapSectionRepo;
using MSetInfoRepo;
using PngImageLib;
using System;
using System.IO;

namespace ImageBuilder
{
	public class PngBuilder
	{
		private readonly string _imageOutputFolder;
		private readonly int _blockWidth;
		private readonly int _blockHeight;

		public PngBuilder(string imageOutputFolder, int blockWidth, int blockHeight)
		{
			_imageOutputFolder = imageOutputFolder;
			_blockWidth = blockWidth;
			_blockHeight = blockHeight;
		}

		public void Build(string mFilePath)
		{
			var mSetInfo = MSetInfoReaderWriter.Read(mFilePath);
			bool isHighRes = mSetInfo.IsHighRes;
			var maxIterations = mSetInfo.MaxIterations;
			var colorMap = mSetInfo.ColorMap;
			var repofilename = mSetInfo.Name;

			ICountsRepoReader countsRepoReader = GetReader(repofilename, isHighRes);
			SizeInt imageSizeInBlocks = GetImageSizeInBlocks(countsRepoReader);

			int w = imageSizeInBlocks.W;
			int h = imageSizeInBlocks.H;

			var imageSize = new SizeInt(w * _blockWidth, h * _blockHeight);

			string imagePath = GetImageFilename(mFilePath, imageSize.W, isHighRes, _imageOutputFolder);

			var key = new KPoint(0, 0);

			using PngImage pngImage = new PngImage(imagePath, imageSize.W, imageSize.H);
			for (int vBPtr = 0; vBPtr < h; vBPtr++)
			{
				key.Y = vBPtr;
				for (int lPtr = 0; lPtr < 100; lPtr++)
				{
					ImageLine iLine = pngImage.ImageLine;

					for (int hBPtr = 0; hBPtr < w; hBPtr++)
					{
						key.X = hBPtr;

						int[] countsForThisLine = countsRepoReader.GetCounts(key, lPtr);
						if (countsForThisLine != null)
						{
							BuildPngImageLineSegment(hBPtr * _blockWidth, countsForThisLine, iLine, maxIterations, colorMap);
						}
						else
						{
							BuildBlankPngImageLineSegment(hBPtr * _blockWidth, _blockWidth, iLine);
						}
					}

					pngImage.WriteLine(iLine);
				}
			}
		}

		private ICountsRepoReader GetReader(string fn, bool isHighRes)
		{
			if (isHighRes)
			{
				return new CountsRepoReaderHiRes(fn, _blockWidth, _blockHeight);
			}
			else
			{
				return new CountsRepoReader(fn, _blockWidth, _blockHeight);
			}
		}

		private static string GetImageFilename(string fn, int imageWidth, bool isHighRes, string basePath)
		{
			string imagePath;
			if (isHighRes)
			{
				imagePath = Path.Combine(basePath, $"{fn}_hrez_{imageWidth}.png");
			}
			else
			{
				imagePath = Path.Combine(basePath, $"{fn}_{imageWidth}.png");
			}

			return imagePath;
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

		private static SizeInt GetImageSizeInBlocks(ICountsRepoReader countsRepo)
		{
			int w = 10;
			int h = 0;

			KPoint key = new KPoint(w, h);
			bool foundMax = !countsRepo.ContainsKey(key);

			if (foundMax) return new SizeInt(0, 0);

			// Find max value where w and h are equal.
			while (!foundMax)
			{
				w++;
				h++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			w--;
			h--;
		
			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				h++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			h--;

			foundMax = false;
			// Find max value of h
			while (!foundMax)
			{
				w++;
				key = new KPoint(w, h);
				foundMax = !countsRepo.ContainsKey(key);
			}

			//w--;

			return new SizeInt(w, ++h);
		}

	}
}
