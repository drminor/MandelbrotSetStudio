using MEngineDataContracts;
using MSS.Common;
using MSS.Common.MSetRepo;
using MSS.Types;
using MSS.Types.MSet;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class PngBuilder
	{
		private const int VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionHelper _mapSectionHelper;

		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _currentResponses;

		//private int _cntr = 0;

		public PngBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionHelper = new MapSectionHelper();
			_currentJobNumber = null;
			_currentResponses = null;

			_mapLoaderManager.MapSectionReady += MapSectionReady;
		}

		//public void BuildPrep(Poster poster)
		//{
		//	var jobAreaInfo = poster.JobAreaInfo;
		//	var mapCalcSettings = poster.MapCalcSettings;
		//	var jobAreaAndCalcSettings = new JobAreaAndCalcSettings(jobAreaInfo, mapCalcSettings);

		//	_mapLoaderManager.MapSectionReady += MapSectionReady;

		//	_mapLoaderManager.Push(jobAreaAndCalcSettings);
		//}

		public async Task<bool> BuildAsync(string imageFilePath, Poster poster, Action<double> statusCallBack, CancellationToken ct)
		{
			var jobAreaInfo = poster.JobAreaInfo;
			var mapCalcSettings = poster.MapCalcSettings;

			var canvasSize = jobAreaInfo.CanvasSize;
			var blockSize = jobAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(poster.ColorBandSet)
			{
				UseEscapeVelocities = mapCalcSettings.UseEscapeVelocities
			};

			var imageSizeInBlocks = RMapHelper.GetMapExtentInBlocks(canvasSize, blockSize);
			var imageSize = imageSizeInBlocks.Scale(blockSize);

			Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {imageSizeInBlocks}. The ColorMap has Id: {poster.ColorBandSet.Id}.");

			var stream = File.Open(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
			var pngImage = new PngImage(stream, imageFilePath, imageSize.Width, imageSize.Height);

			try
			{

				var w = imageSizeInBlocks.Width;
				var h = imageSizeInBlocks.Height;

				for (var vBPtr = 0; vBPtr < h && !ct.IsCancellationRequested; vBPtr++)
				{
					var blocksForThisRow = await GetAllBlocksForRowAsync(vBPtr, w, jobAreaInfo.MapBlockOffset, jobAreaInfo.Subdivision, mapCalcSettings);

					//var checkCnt = blocksForThisRow.Count;

					//Debug.Assert(checkCnt == w);

					for (var lPtr = 0; lPtr < blockSize.Height; lPtr++)
					{
						var iLine = pngImage.ImageLine;

						for (var hBPtr = 0; hBPtr < w; hBPtr++)
						{
							var mapSection = blocksForThisRow[hBPtr];

							var countsForThisLine = GetOneLineFromCountsBlock(mapSection?.Counts, lPtr, blockSize.Width);

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

					var p = vBPtr / (double)h;
					statusCallBack(100 * p);
				}
			}
			catch
			{
				if (!ct.IsCancellationRequested)
				{
					throw;
				}
			}
			finally
			{
				if (!ct.IsCancellationRequested)
				{
					pngImage.End();
				}
				else
				{
					pngImage.Abort();
				}
			}

			return true;
		}

		//private async Task ProcessOneRowAsync(int rowPtr, int stride, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings, IMapLoaderManager _mapLoaderManager, CancellationToken ct)
		//{
		//	while (!ct.IsCancellationRequested)
		//	{
		//		try
		//		{
		//			var blocksForOneRow = await GetAllBlocksForRowAsync(rowPtr, stride, mapBlockOffset, subdivision, mapCalcSettings);
		//		}
		//		catch (OperationCanceledException)
		//		{
		//			//Debug.WriteLine("The response queue got a OCE.");
		//		}
		//		catch (Exception e)
		//		{
		//			Debug.WriteLine($"ProcessOnRowAsync got an exception. The exception is {e}.");
		//			throw;
		//		}
		//	}
		//}

		//private async Task<IDictionary<int, MapSectionResponse?>> GetAllBlocksForRowAsyncOld (int rowPtr, int stride, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		//{
		//	var result = new Dictionary<int, MapSectionResponse?>();

		//	for (var colPtr = 0; colPtr < stride; colPtr++)
		//	{
		//		var key = new PointInt(colPtr, rowPtr);
		//		var mapSectionRequest = _mapSectionHelper.CreateRequest(key, mapBlockOffset, subdivision, mapCalcSettings);

		//		var mapSectionResponse = await GetMapSectionAsync(mapSectionRequest);

		//		result.Add(colPtr, mapSectionResponse);
		//	}

		//	return result;
		//}

		private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(int rowPtr, int stride, BigVector mapBlockOffset, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var requests = new List<MapSectionRequest>();

			for (var colPtr = 0; colPtr < stride; colPtr++)
			{
				var key = new PointInt(colPtr, rowPtr);
				var mapSectionRequest = _mapSectionHelper.CreateRequest(key, mapBlockOffset, subdivision, mapCalcSettings);
				requests.Add(mapSectionRequest);
			}

			_currentJobNumber = _mapLoaderManager.Push(mapBlockOffset, requests);
			_currentResponses = new Dictionary<int, MapSection?>();

			var task = _mapLoaderManager.GetTaskForJob(_currentJobNumber.Value);

			if (task != null)
			{
				//Task.WaitAny(new Task[] {task, ct.WaitHandle})
				await task;
			}

			return _currentResponses ?? new Dictionary<int, MapSection?>();
		}

		private void MapSectionReady(object? sender, Tuple<MapSection, int> e)
		{
			if (e.Item2 == _currentJobNumber)
			{
				_currentResponses?.Add(e.Item1.BlockPosition.X, e.Item1);
			}
		}

		//private async Task<MapSectionResponse?> GetMapSectionAsync(MapSectionRequest mapSectionRequest)
		//{
		//	var mapSectionResponse = await _mapSectionAdapter.GetMapSectionAsync(mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition);

		//	//if (mapSectionResponse == null)
		//	//{
		//	//	mapSectionResponse = _mEngineClient.GenerateMapSection(mapSectionRequest);
		//	//}

		//	return mapSectionResponse;
		//}

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

