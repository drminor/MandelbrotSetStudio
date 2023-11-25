using ImageBuilderWPF;
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
	public class LazyMapPreviewImageProvider : IDisposable
	{
		#region Private Fields

		private const int BYTES_PER_PIXEL = 4;

		private readonly static PixelFormat PIXEL_FORMAT = PixelFormats.Pbgra32;
		private const int DOTS_PER_INCH = 96;


		private const int MS_WAIT_FOR_CANCELLED_TASK_TO_COMPLETE = 500;

		private const double PREVIEW_CONTAINER_SIZE = 1024;

		private MapJobHelper _mapJobHelper;
		private SynchronizationContext _synchronizationContext;
		private readonly IImageBuilderWPF _bitmapBuilder;

		private readonly ColorBandSet _colorBandSet;
		private readonly MapCalcSettings _mapCalcSettings;
		private readonly bool _useEscapeVelocitites;
		private readonly Color _fallbackColor;

		private MapCenterAndDelta _mapAreaInfo;           // Coords of the source map for which the preview is being generated.
		private SizeDbl _posterSize;
		private SizeDbl _containerSize;

		//private SizeDbl _previewImageSize;
		//private double _scaleFactor;
		//private MapAreaInfo _previewMapAreaInfo;	// Coods, SampleSize, etc. are ajusted for the previewImageSize

		private CancellationTokenSource _cts;
		private Task? _currentBitmapBuilderTask;

		#endregion

		#region Constructor

		public LazyMapPreviewImageProvider(AreaColorAndCalcSettings areaColorAndCalcSettings, SizeDbl posterSize, bool useEscapeVelocities, Color fallbackColor, MapJobHelper mapJobHelper, IImageBuilderWPF bitmapBuilder)
		{
			_mapJobHelper = mapJobHelper;

			_synchronizationContext = SynchronizationContext.Current ?? throw new InvalidOperationException("No SynchronizationContext is available.");
			_bitmapBuilder = bitmapBuilder;

			_colorBandSet = areaColorAndCalcSettings.ColorBandSet;
			_mapCalcSettings = areaColorAndCalcSettings.MapCalcSettings;
			_useEscapeVelocitites = useEscapeVelocities;
			_fallbackColor = fallbackColor;

			_cts = new CancellationTokenSource();
			_currentBitmapBuilderTask = null;

			JobId = areaColorAndCalcSettings.JobId;
			OwnerType = areaColorAndCalcSettings.JobOwnerType;

			_mapAreaInfo = areaColorAndCalcSettings.MapAreaInfo;
			_posterSize = posterSize;

			var containerSize = new SizeDbl(PREVIEW_CONTAINER_SIZE);
			var previewMapAreaInfo = GetMapAreaInfoWithSize(_mapAreaInfo, containerSize, _posterSize);

			Bitmap = CreateBitmap(previewMapAreaInfo.CanvasSize.Round());
			FillBitmapWithColor(_fallbackColor, Bitmap);

			//QueueBitmapGeneration(JobId, OwnerType, previewMapAreaInfo, _colorBandSet, _mapCalcSettings, _synchronizationContext);
		}

		#endregion

		#region Public Events

		public event EventHandler? BitmapHasBeenLoaded;

		#endregion

		#region Public Properties

		public ObjectId JobId { get; set; }
		public OwnerType OwnerType { get; set; }
		public MapCenterAndDelta MapAreaInfo => _mapAreaInfo;
		public SizeDbl ContainerSize => _containerSize;
		public WriteableBitmap Bitmap { get; set; }

		#endregion

		#region Public Methods

		//public void RequestBitmapGenerationOld(MapAreaInfo2 mapAreaInfo, SizeDbl posterSize)
		//{
		//	_mapAreaInfo = mapAreaInfo;
		//	_posterSize = posterSize;

		//	var previewMapAreaInfo = GetMapAreaInfoWithSize(_mapAreaInfo, _posterSize);

		//	Bitmap = CreateBitmap(previewMapAreaInfo.CanvasSize.Round());
		//	FillBitmapWithColor(_fallbackColor, Bitmap);
		//	QueueBitmapGeneration(JobId, OwnerType, previewMapAreaInfo, _colorBandSet, _mapCalcSettings);
		//}

		public WriteableBitmap RequestBitmapGeneration(MapCenterAndDelta mapAreaInfo, SizeDbl containerSize, SizeDbl posterSize)
		{
			if (_synchronizationContext == null)
			{
				if (SynchronizationContext.Current == null)
				{
					throw new InvalidOperationException("Request BitmapGeneration. The LazyMapPreviewImageProvider has no Synchronization Context.");
				}
				else
				{
					_synchronizationContext = SynchronizationContext.Current;
				}
			}

			_mapAreaInfo = mapAreaInfo;
			_posterSize = posterSize;

			var previewMapAreaInfo = GetMapAreaInfoWithSize(mapAreaInfo, containerSize, _posterSize);

			Bitmap = CreateBitmap(previewMapAreaInfo.CanvasSize.Round());
			//FillBitmapWithColor(_fallbackColor, Bitmap);
			QueueBitmapGeneration(JobId, OwnerType, previewMapAreaInfo, _colorBandSet, _mapCalcSettings, _synchronizationContext);

			return Bitmap;
		}

		public void CancelBitmapGeneration()
		{
			_cts.Cancel();
		}

		public void UpdateContainerSize(SizeDbl value)
		{
			if (value != _containerSize)
			{
				// TODO: Use the given Container Size and find the closest SamplePointDelta at or below the standard SPD for this map.
				Debug.WriteLine($"THe LazyMapPreviewImageProvider is having its Container Size updated from: {_containerSize} to {value}.");
				_containerSize = value;
			}
		}

		#endregion

		#region Private Methods

		// Calculate the map coordinates needed to display the entire poster content in the Size Editor preview window.
		private MapPositionSizeAndDelta GetMapAreaInfoWithSize(MapCenterAndDelta mapAreaInfo, SizeDbl containerSize, SizeDbl posterSize)
		{
			//var factorW = containerSize.Width / posterSize.Width;
			//var factorH = containerSize.Height / posterSize.Height;
			//var factor = Math.Min(factorW, factorH);
			//factor /= 0.3247802734375;

			var fContainerSize = new SizeDbl(1024);

			//var factorW = 1024d / posterSize.Width;
			//var factorH = 1024d / posterSize.Height;

			////var factorW = containerSize.Width / posterSize.Width;
			////var factorH = containerSize.Height / posterSize.Height;

			//var factor = Math.Min(factorW, factorH);

			var scaleFactor = RMapHelper.GetSmallestScaleFactor(posterSize, fContainerSize);

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoom(mapAreaInfo, scaleFactor, out var diagReciprocal);
			var previewImageSize = posterSize.Scale(scaleFactor);

			//Debug.WriteLine($"MapPreviewImage provider is getting the new coordinates. DiagReciprocal is {diagReciprocal}, Factor: w:{factorW}, h:{factorH}. ScaleFactor: {scaleFactor}.");
			//Debug.Assert(Math.Abs(factor - scaleFactor) < 0.01d, "factor and scalefactor are different.");

			var previewMapAreaInfo = _mapJobHelper.GetMapPositionSizeAndDelta(newMapAreaInfo, previewImageSize);
			Debug.WriteLine($"MapPreviewImage provider is getting the new coordinates. DiagReciprocal is {diagReciprocal}, ScaleFactor: {scaleFactor}. Canvas Size: {previewMapAreaInfo.CanvasSize}. Preview Image Size: {previewImageSize}.");

			return previewMapAreaInfo;
		}

		private void QueueBitmapGeneration(ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta previewMapArea, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, SynchronizationContext synchronizationContext)
		{
			var previewImageSize = previewMapArea.CanvasSize;
			Debug.WriteLine($"Creating a preview image with size: {previewMapArea.CanvasSize} and map coords: {previewMapArea.Coords}.");

			if (_currentBitmapBuilderTask != null && !_currentBitmapBuilderTask.IsCompleted)
			{
				CancelCurrentBitmapGeneration(_currentBitmapBuilderTask, _cts);
			}

			_cts = new CancellationTokenSource();

			var imageSize = previewMapArea.CanvasSize.Round();
			var imageSourceWriter = new ImageSourceWriter(Bitmap, _synchronizationContext);


			_currentBitmapBuilderTask = Task.Run(async () =>
				{
					try
					{
						//var pixels = await _bitmapBuilder.FillAsync(imageSourceWriter, jobId, ownerType, previewMapArea, colorBandSet, mapCalcSettings, _useEscapeVelocitites, _cts.Token, statusCallback: null);
						var wasSuccessful = await _bitmapBuilder.FillAsync(imageSourceWriter, jobId, ownerType, previewMapArea, colorBandSet, mapCalcSettings, _useEscapeVelocitites, _cts.Token, statusCallback: null);

						if (!_cts.IsCancellationRequested)
						{
							_synchronizationContext?.Post(o => BitmapCompleted(wasSuccessful, o), _cts);
						}
					}
					catch (AggregateException agEx)
					{
						Debug.WriteLine($"The BitmapBuilder task failed. The exception is {agEx}.");
					}
				}, _cts.Token
			);
		}

		private void CancelCurrentBitmapGeneration(Task task, CancellationTokenSource cts)
		{
			var stopWatch = Stopwatch.StartNew();

			if (!cts.IsCancellationRequested)
			{
				cts.Cancel(throwOnFirstException: false);
			}

			try
			{
				if (!task.Wait(MS_WAIT_FOR_CANCELLED_TASK_TO_COMPLETE))
				{
					Debug.WriteLine($"WARNING: The current BitmapBuilder task did not complete within {MS_WAIT_FOR_CANCELLED_TASK_TO_COMPLETE}ms.");
				}
			}
			catch (TaskCanceledException)
			{
				Debug.WriteLine("CancelCurrentBitmapGeneration received a TaskCancelledException.");
			}
			catch (AggregateException ae)
			{
				if (!(ae.InnerException is TaskCanceledException))
				{
					Debug.WriteLine($"Received exception: {ae} while waiting for the Bitmap to be generated.");
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Received exception: {e} while waiting for the Bitmap to be generated.");
			}

			Debug.WriteLine($"Canceling the CurrentBitmapGeneration Task took: {stopWatch.ElapsedMilliseconds}ms.");
		}

		//private void BitmapCompleted(byte[]? pixels, object? state)
		//{
		//	if (state != null && state is CancellationTokenSource cts && !cts.IsCancellationRequested)
		//	{
		//		if (pixels != null)
		//		{
		//			WritePixels(pixels, Bitmap);
		//		}
		//		else
		//		{
		//			FillBitmapWithColor(Colors.LightCoral, Bitmap);
		//		}

		//		BitmapHasBeenLoaded?.Invoke(this, EventArgs.Empty);
		//	}
		//}

		private void BitmapCompleted(bool wasSuccessful, object? state)
		{
			if (state != null && state is CancellationTokenSource cts && !cts.IsCancellationRequested)
			{
				if (!wasSuccessful)
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
			var stride = BYTES_PER_PIXEL * w;
			bitmap.WritePixels(rect, pixels, stride, 0);
		}

		private void FillBitmapWithColor(Color color, WriteableBitmap bitmap)
		{
			var width = (int) Math.Round(bitmap.Width);

			var pixels = CreateOneRowWithColor(color, width);
			var stride = width * BYTES_PER_PIXEL;

			var rect = new Int32Rect(0, 0, width, 1);

			for (var i = 0; i < bitmap.Height; i++)
			{
				rect.Y = i;
				bitmap.WritePixels(rect, pixels, stride, 0);
			}
		}

		private byte[] CreateOneRowWithColor(Color color, int rowLength)
		{
			var pixels = new byte[rowLength * BYTES_PER_PIXEL];

			for (var i = 0; i < rowLength; i++)
			{
				var offSet = i * BYTES_PER_PIXEL;
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

			var bitmap = new WriteableBitmap(w, h, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
			return bitmap;
		}

		//private SizeDbl GetPreviewSize(SizeDbl currentSize, double previewImageSideLength)
		//{
		//	var scaleFactor = RMapHelper.GetSmallestScaleFactor(currentSize, new SizeDbl(previewImageSideLength));
		//	var previewSize = currentSize.Scale(scaleFactor);

		//	return previewSize;
		//}

		#endregion

		#region IDisposable

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: set large fields to null
				_disposedValue = true;
			}
		}


		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}

}
