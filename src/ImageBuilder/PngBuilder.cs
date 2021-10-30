using FSTypes;
using MFile;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageBuilder
{
	using ColorMapEntry = MFile.ColorMapEntry;
	public class PngBuilder
	{
		public readonly string BasePath;
		public readonly int BlockWidth;
		public readonly int BlockHeight;

		public PngBuilder(string basePath, int blockWidth, int blockHeight)
		{
			BasePath = basePath;
			BlockWidth = blockWidth;
			BlockHeight = blockHeight;
		}

		public void Build(string fn, bool hiRez)
		{
			// TODO: HiRez, blockWidth and blockHeight should come from the RepoFile.

			MFileInfo mFileInfo = ReadFromJson(fn);
			int maxIterations = mFileInfo.MaxIterations;
			IList<ColorMapEntry> colorRanges = mFileInfo.ColorMapEntries;

			string repofilename = mFileInfo.Name;

			var countsRepoReader = new CountsRepoReader(repofilename, hiRez, BlockWidth, BlockHeight);
			CanvasSize imageSizeInBlocks = GetImageSizeInBlocks(countsRepoReader);

			int w = imageSizeInBlocks.Width;
			int h = imageSizeInBlocks.Height;

			var imageSize = new CanvasSize(w * BlockWidth, h * BlockHeight);

			string imagePath = GetImageFilename(fn, imageSize.Width, hiRez, BasePath);

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

						int[] countsForThisLine = countsRepoReader.GetCounts(key, lPtr);
						if (countsForThisLine != null)
						{
							// TODO: Fix me
							//BuildPngImageLineSegment(hBPtr * BlockWidth, countsForThisLine, iLine, maxIterations, colorMap);
						}
						else
						{
							BuildBlankPngImageLineSegment(hBPtr * BlockWidth, BlockWidth, iLine);
						}
					}

					pngImage.WriteLine(iLine);
				}
			}
		}

		private MFileInfo ReadFromJson(string fn)
		{
			string fnWithExt = Path.ChangeExtension(fn, "json");
			string path = Path.Combine(BasePath, fnWithExt);

			var mFileReaderWriter = new MFileReaderWriter();
			MFileInfo mFileInfo = mFileReaderWriter.Read(path);
			return mFileInfo;
		}

		private static string GetImageFilename(string fn, int imageWidth, bool hiRez, string basePath)
		{
			string imagePath;
			if (hiRez)
			{
				imagePath = Path.Combine(basePath, $"{fn}_hrez_{imageWidth}.png");
			}
			else
			{
				imagePath = Path.Combine(basePath, $"{fn}_{imageWidth}.png");
			}

			return imagePath;
		}

		//private int[] GetOneLineFromCountsBlock(int[] counts, int lPtr)
		//{
		//	int[] result = new int[BlockWidth];

		//	Array.Copy(counts, lPtr * BlockWidth, result, 0, BlockWidth);
		//	return result;
		//}

		//private int[] GetOneLineFromCountsBlock(uint[] counts, int lPtr)
		//{
		//	int[] result = new int[BlockWidth];
		//	int srcPtr = lPtr * BlockWidth;

		//	for (int i = 0; i < result.Length; i++)
		//		result[i] = (int)counts[srcPtr++];

		//	return result;
		//}

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

		private static CanvasSize GetImageSizeInBlocks(CountsRepoReader countsRepo)
		{

			int w = 10;
			int h = 0;

			KPoint key = new KPoint(w, h);
			bool foundMax = !countsRepo.ContainsKey(key);

			if (foundMax) return new CanvasSize(0, 0);

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

			return new CanvasSize(w, ++h);
		}

	}
}
