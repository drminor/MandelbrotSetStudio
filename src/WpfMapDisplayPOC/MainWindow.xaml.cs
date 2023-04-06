using MSS.Types;
using MSS.Types.MSet;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private SizeInt _blockSize;
		private MainWindowViewModel _vm;
		private int _maxYPtr;

		#region Constructor 

		public MainWindow()
		{
			_blockSize = RMapConstants.BLOCK_SIZE;
			_vm = (MainWindowViewModel)DataContext;
			_maxYPtr = (1024 / 128) - 1;
			RenderMethod = RenderMethod.Wp;

			Loaded += MainWindow_Loaded;
			ContentRendered += MainWindow_ContentRendered;
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the Main (WpfMapDisplayPOC) Window is being loaded.");
				return;
			}
			else
			{
				_vm = (MainWindowViewModel)DataContext;

				Debug.WriteLine("The Main (WpfMapDisplayPOC) Window is now loaded");
			}
		}

		private void MainWindow_ContentRendered(object? sender, EventArgs e)
		{
			var h = (int) MapSectionDispControl1.ActualHeight;
			var w = (int)MapSectionDispControl1.ActualWidth;

			var size = new SizeInt(w, h);

			Debug.WriteLine($"Window Size is {size}.");
		}

		#endregion

		#region Public Properties
		
		public string? JobId { get; set; }
		public RenderMethod RenderMethod { get; set; }

		#endregion

		#region Button Handlers

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			MapSectionDispControl1.ClearCanvas();
		}

		private void LoadButtonUiThread_Click(object sender, RoutedEventArgs e)
		{
			var crevice1_HomeJobId = "641b58493811e2c2a6e1c18c";
			JobId = crevice1_HomeJobId;

			MapSectionDispControl1.ClearCanvas();

			LoadJobUiThread();
		}

		private void LoadButtonBgThread_Click(object sender, RoutedEventArgs e)
		{
			var crevice1_HomeJobId = "641b58493811e2c2a6e1c18c";
			JobId = crevice1_HomeJobId;

			MapSectionDispControl1.ClearCanvas();

			var renderThread = new Thread(new ThreadStart(LoadJobBGThread));
			renderThread.Start();
		}

		private void LoadButtonUiThreadPixOnly_Click(object sender, RoutedEventArgs e)
		{
			var crevice1_HomeJobId = "641b58493811e2c2a6e1c18c";
			JobId = crevice1_HomeJobId;

			MapSectionDispControl1.ClearCanvas();

			LoadJobUiThreadPixelsOnly();
		}

		private void LoadButtonMapLoader_Click(object sender, RoutedEventArgs e)
		{
			//int jobId = _vm.Load("641b56e43811e2c2a6e1bbff");
			var crevice1_HomeJobId = "641b58493811e2c2a6e1c18c";
			JobId = crevice1_HomeJobId;

			MapSectionDispControl1.ClearCanvas();

			UseMapLoader();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		#endregion

		#region Private Methods

		private void LoadJobUiThread()
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			int jobNumber = 1;

			// Load MapSectionIds
			var sw = Stopwatch.StartNew();
			var mapSectionRequests = _vm.GetSectionRequestsForJob(JobId);
			sw.Stop();
			var loadTime = sw.ElapsedMilliseconds;

			// GetMapSections
			sw.Restart();
			var mapSections = GetMapSections(jobNumber, mapSectionRequests);
			sw.Stop();
			var fetchTime = sw.ElapsedMilliseconds;

			// Render
			sw.Restart();
			var totalSectionsFound = 0;
			var totalGetPixDuration = TimeSpan.Zero;

			foreach (var mapSection in mapSections)
			{
				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;
					MapSectionReadyUiThread(mapSection, jobNumber, isLastSection: false, out var getPixDuration);
					totalGetPixDuration = totalGetPixDuration.Add(getPixDuration);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection.");
				}
			}

			sw.Stop();
			var renderTime = sw.ElapsedMilliseconds;

			Debug.WriteLine($"Ui:: Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. Got Pixles in {totalGetPixDuration.TotalMilliseconds}ms.");
			_vm.UiResults = $"Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. Got Pixles in {totalGetPixDuration.TotalMilliseconds}ms.";
		}

		private void MapSectionReadyUiThread(MapSection mapSection, int jobNumber, bool isLastSection, out TimeSpan getPixDuration)
		{
			if (_vm.TryGetPixelArray(mapSection, out getPixDuration, out var pixelArray))
			{
				PlaceBitmap(RenderMethod, mapSection, pixelArray);
			}
		}

		private void LoadJobBGThread()
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			int jobNumber = 1;

			// Load MapSectionIds
			var sw = Stopwatch.StartNew();
			var mapSectionRequests = _vm.GetSectionRequestsForJob(JobId);
			sw.Stop();
			var loadTime = sw.ElapsedMilliseconds;

			// GetMapSections
			sw.Restart();
			var mapSections = GetMapSections(jobNumber, mapSectionRequests);
			sw.Stop();
			var fetchTime = sw.ElapsedMilliseconds;

			// Render
			sw.Restart();
			var totalSectionsFound = 0;
			var totalGetPixDuration = TimeSpan.Zero;

			foreach (var mapSection in mapSections)
			{
				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;
					MapSectionReadyBGThread(mapSection, jobNumber, isLastSection: false, out var getPixDuration);
					totalGetPixDuration = totalGetPixDuration.Add(getPixDuration);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection.");
				}
			}

			sw.Stop();
			var renderTime = sw.ElapsedMilliseconds;

			Debug.WriteLine($"BG:: Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. GotPixles in {totalGetPixDuration.TotalMilliseconds}ms.");
			_vm.BgResults = $"Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. Got Pixles in {totalGetPixDuration.TotalMilliseconds}ms.";
		}

		private void MapSectionReadyBGThread(MapSection mapSection, int jobNumber, bool isLastSection, out TimeSpan getPixDuration)
		{
			if (_vm.TryGetPixelArray(mapSection, out getPixDuration, out var pixelArray))
			{
				Dispatcher.Invoke(new Action(() =>
				{
					PlaceBitmap(RenderMethod, mapSection, pixelArray);
				}), DispatcherPriority.Render);
			}
		}

		private void LoadJobUiThreadPixelsOnly()
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			int jobNumber = 1;

			// Load MapSectionIds
			var sw = Stopwatch.StartNew();
			var mapSectionRequests = _vm.GetSectionRequestsForJob(JobId);
			sw.Stop();
			var loadTime = sw.ElapsedMilliseconds;

			// GetMapSections
			sw.Restart();
			var mapSections = GetMapSections(jobNumber, mapSectionRequests);
			sw.Stop();
			var fetchTime = sw.ElapsedMilliseconds;

			// Render
			sw.Restart();
			var totalSectionsFound = 0;
			var totalGetPixDuration = TimeSpan.Zero;

			foreach (var mapSection in mapSections)
			{
				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;
					MapSectionReadyGetPixelsOnly(mapSection, jobNumber, isLastSection: false, out var getPixDuration);
					totalGetPixDuration = totalGetPixDuration.Add(getPixDuration);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection.");
				}
			}

			sw.Stop();
			var renderTime = sw.ElapsedMilliseconds;
			
			Debug.WriteLine($"Op:: Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. Got Pixles in {totalGetPixDuration.TotalMilliseconds}ms.");
			_vm.OpResults = $"Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms. Got Pixles in {totalGetPixDuration.TotalMilliseconds}ms.";
		}


		private void UseMapLoader()
		{
			_vm.RunHomeJob(MapSectionReady);
		}

		private void MapSectionReady(MapSection mapSection)
		{
			//_bitmap.Dispatcher.Invoke(GetAndPlacePixels, new object[] { mapSection });

			if (_vm.TryGetPixelArray(mapSection, out var getPixDuration, out var pixelArray))
			{
				Dispatcher.Invoke(new Action(() =>
				{
					PlaceBitmap(RenderMethod, mapSection, pixelArray);
				}), DispatcherPriority.Render);
			}

		}


		#endregion

		#region Support Methods

		// TODO: Test the MapSectionHelper's FillBackBuffer method.
		//void FillBackBuffer(IntPtr backBuffer, int backBufferStride, PointInt destination, SizeInt destSize, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert, bool useEscapeVelocities)

		private void PlaceBitmap(RenderMethod renderMethod, MapSection mapSection, byte[] pixelArray)
		{
			switch (renderMethod)
			{
				case RenderMethod.Wp:
					PlaceBitmap(mapSection, pixelArray);
					break;
				case RenderMethod.Skia:
					PlaceBitMapSkia(mapSection, pixelArray);
					break;
				case RenderMethod.SkiaBuf:
					PlaceBitMapSkiaBuf(mapSection, pixelArray);
					break;
				default:
					throw new NotSupportedException($"The RenderMethod: {renderMethod} is not supported.");
			}
		}

		private void PlaceBitmap(MapSection mapSection, byte[] pixelArray)
		{
			var size = RMapConstants.BLOCK_SIZE;

			//var loc = new Point(mapSection.BlockPosition.X * 128, mapSection.BlockPosition.Y * 128);

			var invBp = GetInvertedBlockPos(mapSection.ScreenPosition);
			var sInvBp = invBp.Scale(_blockSize);
			var loc = new Point(sInvBp.X, sInvBp.Y);


			var sourceRect = new Int32Rect(0, 0, size.Width, size.Height);
			MapSectionDispControl1.PlaceBitmap(pixelArray, sourceRect, loc);

			var rect = new Int32Rect((int)loc.X, (int)loc.Y, size.Width, size.Height);
			MapSectionDispControl1.CallForUpdate(rect);
		}

		private void PlaceBitMapSkia(MapSection mapSection, byte[] pixelArray)
		{
			var sKBitmap = SkiaHelper.ArrayToImage(pixelArray, RMapConstants.BLOCK_SIZE);

			//var skPoint = new SKPoint(mapSection.BlockPosition.X * 128, mapSection.BlockPosition.Y * 128);
			var invBp = GetInvertedBlockPos(mapSection.ScreenPosition);
			var sInvBp = invBp.Scale(_blockSize);
			var skPoint = new SKPoint(sInvBp.X, sInvBp.Y);


			MapSectionDispControl1.PlaceBitmap(sKBitmap, skPoint);

			var rect = new Int32Rect((int)skPoint.X, (int)skPoint.Y, sKBitmap.Width, sKBitmap.Height);
			MapSectionDispControl1.CallForUpdate(rect);
		}

		private void PlaceBitMapSkiaBuf(MapSection mapSection, byte[] pixelArray)
		{
			var sKBitmap = SkiaHelper.ArrayToImage(pixelArray, RMapConstants.BLOCK_SIZE);
			
			var invBp = GetInvertedBlockPos(mapSection.ScreenPosition);
			var sInvBp = invBp.Scale(_blockSize);
			var skPoint = new SKPoint(sInvBp.X, sInvBp.Y);
			
			MapSectionDispControl1.PlaceBitmapBuf(sKBitmap, skPoint);

			var rect = new Int32Rect((int)skPoint.X, (int)skPoint.Y, sKBitmap.Width, sKBitmap.Height);
			MapSectionDispControl1.CallForUpdate(rect);
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			return result;
		}

		private List<MapSection> GetMapSections(int jobNumber, IList<MapSectionRequest> mapSectionRequests)
		{
			var result = new List<MapSection>();

			var totalSectionsFound = 0;

			foreach (var mapSectionRequest in mapSectionRequests)
			{
				var mapSection = _vm.GetMapSection(mapSectionRequest, jobNumber);

				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;
					result.Add(mapSection);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection with Screen Position: {mapSectionRequest.ScreenPosition}.");
				}
			}

			return result;
		}

		private void MapSectionReadyGetPixelsOnly(MapSection mapSection, int jobNumber, bool isLastSection, out TimeSpan getPixDuration)
		{
			if (_vm.TryGetPixelArray(mapSection, out getPixDuration, out var pixelArray))
			{
				if (pixelArray.Length < 10)
				{
					Debug.WriteLine("The pixel array was short.");
				}
			}
			else
			{
				Debug.WriteLine($"Could not get the pixels.");
			}
		}

		#endregion
	}

	public enum RenderMethod
	{
		Wp,
		Skia,
		SkiaBuf
	}
}
