﻿using ImageBuilder;
using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	public class LazyMapPreviewImageProvider 
	{
		private readonly SynchronizationContext? _synchronizationContext;
		private readonly BitmapBuilder _bitmapBuilder;

		private MapAreaInfo2 _mapAreaInfo;           // Coords of the source map for which the preview is being generated.
		private SizeDbl _previewImageSize;
		private MapAreaInfo _previewMapAreaInfo;	// Coods, SampleSize, etc. are ajusted for the previewImageSize

		private readonly ColorBandSet _colorBandSet;
		private readonly MapCalcSettings _mapCalcSettings;
		private readonly bool _useEscapeVelocitites;
		private readonly Color _fallbackColor;

		private CancellationTokenSource _cts;
		private Task? _currentBitmapBuilderTask;

		private MapJobHelper _mapJobHelper;

		#region Constructor

		public LazyMapPreviewImageProvider(MapJobHelper mapJobHelper, BitmapBuilder bitmapBuilder, ObjectId jobId, OwnerType ownerType, MapAreaInfo2 mapAreaInfo, SizeDbl previewImageSize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, Color fallbackColor)
		{
			_mapJobHelper = mapJobHelper;

			_synchronizationContext = SynchronizationContext.Current;
			_bitmapBuilder = bitmapBuilder;

			JobId = jobId;
			OwnerType = ownerType;
			_mapAreaInfo = mapAreaInfo;

			_previewImageSize = previewImageSize;
			_colorBandSet = colorBandSet;
			_mapCalcSettings = mapCalcSettings;
			_useEscapeVelocitites = useEscapeVelocities;
			_fallbackColor = fallbackColor;

			_cts = new CancellationTokenSource();
			_currentBitmapBuilderTask = null;

			//_previewMapAreaInfo = MapJobHelper.GetMapAreaWithSize(_mapAreaInfo, _previewImageSize);
			_previewMapAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(_mapAreaInfo, _previewImageSize);

			Bitmap = CreateBitmap(_previewImageSize.Round());
			FillBitmapWithColor(_fallbackColor, Bitmap);

			QueueBitmapGeneration(JobId, ownerType, _previewMapAreaInfo, _colorBandSet, _mapCalcSettings);
		}

		#endregion

		#region Public Properties

		public event EventHandler? BitmapHasBeenLoaded;

		public WriteableBitmap Bitmap { get; init; }

		public ObjectId JobId { get; set; }

		public OwnerType OwnerType { get; set; }

		public MapAreaInfo2 MapAreaInfo
		{
			get => _mapAreaInfo;
			set
			{
				if (value != _mapAreaInfo)
				{
					_mapAreaInfo = value;

					//_previewMapAreaInfo = MapJobHelper.GetMapAreaWithSize(_mapAreaInfo, _previewImageSize);
					_previewMapAreaInfo = _mapJobHelper.GetMapAreaWithSizeFat(_mapAreaInfo, _previewImageSize);

					FillBitmapWithColor(_fallbackColor, Bitmap);
					QueueBitmapGeneration(JobId, OwnerType, _previewMapAreaInfo, _colorBandSet, _mapCalcSettings);
				}
			}
		}

		public void CancelBitmapGeneration()
		{
			_cts.Cancel();
		}

		#endregion

		#region Private Methods

		private void QueueBitmapGeneration(ObjectId jobId, OwnerType ownerType, MapAreaInfo previewMapArea, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			var previewImageSize = previewMapArea.CanvasSize;
			Debug.WriteLine($"Creating a preview image with size: {previewMapArea.CanvasSize} and map coords: {previewMapArea.Coords}.");

			if (_currentBitmapBuilderTask != null)
			{
				if (!_cts.IsCancellationRequested)
				{
					_cts.Cancel();
				}

				// TODO: Wait on the current task to complete.

				Thread.Sleep(1 * 1000);

				_cts = new CancellationTokenSource();
			}

			_currentBitmapBuilderTask = Task.Run(async () =>
				{
					try
					{
						var pixels = await _bitmapBuilder.BuildAsync(jobId, ownerType, previewMapArea, colorBandSet, mapCalcSettings, _useEscapeVelocitites, _cts.Token);
						if (!_cts.IsCancellationRequested)
						{
							_synchronizationContext?.Post(o => BitmapCompleted(pixels, o), _cts);
						}
					}
					catch (AggregateException agEx)
					{
						Debug.WriteLine($"The BitmapBuilder task failed. The exception is {agEx}.");
					}
				}, _cts.Token
			);
		}

		private void BitmapCompleted(byte[]? pixels, object? state)
		{
			if (state != null && state == _cts && !((CancellationTokenSource) state).IsCancellationRequested)
			{
				if (pixels != null)
				{
					WritePixels(pixels, Bitmap);
				}
				else
				{
					FillBitmapWithColor(Colors.LightCoral, Bitmap);
				}

				BitmapHasBeenLoaded?.Invoke(this, EventArgs.Empty);
			}
		}

		private void WritePixels(byte[] pixels, WriteableBitmap bitmap)
		{
			var w = (int)Math.Round(bitmap.Width);
			var h = (int)Math.Round(bitmap.Height);

			var rect = new Int32Rect(0, 0, w, h);
			var stride = 4 * w;
			bitmap.WritePixels(rect, pixels, stride, 0);
		}

		private void FillBitmapWithColor(Color color, WriteableBitmap bitmap)
		{
			var width = (int) Math.Round(bitmap.Width);

			var pixels = CreateOneRowWithColor(color, width);
			var stride = width * 4;

			var rect = new Int32Rect(0, 0, width, 1);

			for (var i = 0; i < bitmap.Height; i++)
			{
				rect.Y = i;
				bitmap.WritePixels(rect, pixels, stride, 0);
			}
		}

		private byte[] CreateOneRowWithColor(Color color, int rowLength)
		{
			var pixels = new byte[rowLength * 4];

			for (var i = 0; i < rowLength; i++)
			{
				var offSet = i * 4;
				pixels[offSet] = color.B;
				pixels[offSet + 1] = color.G;
				pixels[offSet + 2] = color.R;
				pixels[offSet + 3] = 255;
			}

			return pixels;
		}

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var w = size.Width;
			var h = size.Height;

			var bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
			return bitmap;
		}

		#endregion
	}

}
