using MSS.Types;
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
		private MainWindowViewModel _vm;

		#region Constructor 

		public MainWindow()
		{
			_vm = (MainWindowViewModel)DataContext;

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
				//MSectionDispControl1.ClearCanvas();

				//WindowState = WindowState.Normal;
				Debug.WriteLine("The Main (WpfMapDisplayPOC) Window is now loaded");
			}
		}

		private void MainWindow_ContentRendered(object? sender, EventArgs e)
		{
			Debug.WriteLine("Handling the MainWindow ContentRendered Event");
		}

		#endregion

		#region Public Properties
		
		public string? JobId { get; set; }

		#endregion

		#region Button Handlers

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

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			//DialogResult = false;
			Close();
		}

		#endregion

		#region Private Methods

		private List<MapSection> GetMapSections(int jobNumber)
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			var result = new List<MapSection>();

			var sw = Stopwatch.StartNew();

			var totalSectionsFound = 0;

			foreach (var mapSectionRequest in _vm.MapSectionRequests)
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

			sw.Stop();
			var fetchTime = sw.ElapsedMilliseconds;

			Debug.WriteLine($"Retrieved {totalSectionsFound} sections in {fetchTime}ms.");

			return result;
		}

		private void LoadJobBGThread()
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			var sw = Stopwatch.StartNew();

			//int jobNumber = _vm.Load("641b56e43811e2c2a6e1bbff");
			int jobNumber = _vm.Load(JobId);

			sw.Stop();
			var loadTime = sw.ElapsedMilliseconds;

			sw.Restart();

			var mapSections = GetMapSections(jobNumber);
			sw.Stop();
			
			var fetchTime = sw.ElapsedMilliseconds;

			sw.Restart();
			var totalSectionsFound = 0;

			foreach (var mapSection in mapSections)
			{
				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;

					MapSectionReadyBGThread(mapSection, jobNumber, isLastSection: false);
					//MapSectionReadyGetPixelsOnly(mapSection, jobNumber, isLastSection: false);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection.");
				}
			}

			sw.Stop();
			var renderTime = sw.ElapsedMilliseconds;

			Debug.WriteLine($"BG:: Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms.");
		}

		private void MapSectionReadyBGThread(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			if (_vm.TryGetPixelArray(mapSection, out var pixelArray))
			{
				//var sKBitmap = SkiaHelper.ArrayToImage(pixelArray, RMapConstants.BLOCK_SIZE);
				//var skPoint = new SKPoint(mapSection.BlockPosition.X * 128, mapSection.BlockPosition.Y * 128);

				//MapSectionDispControl1.PlaceBitmapBuf(sKBitmap, skPoint);

				Dispatcher.Invoke(new Action(() =>
				{
					//MapSectionDispControl1.PlaceBitmap(sKBitmap, skPoint);

					//var rect = new Int32Rect((int)skPoint.X, (int)skPoint.Y, sKBitmap.Width, sKBitmap.Height);

					var size = RMapConstants.BLOCK_SIZE;
					var loc = new Point(mapSection.BlockPosition.X * 128, mapSection.BlockPosition.Y * 128);

					var rect = new Int32Rect(0, 0, size.Width, size.Height);
					MapSectionDispControl1.PlaceBitmap(pixelArray, rect, loc);

					MapSectionDispControl1.CallForUpdate(rect);

				}), DispatcherPriority.Render);
			}
		}

		private void LoadJobUiThread()
		{
			if (JobId == null)
			{
				throw new InvalidOperationException("The JobId is null.");
			}

			var sw = Stopwatch.StartNew();

			//int jobNumber = _vm.Load("641b56e43811e2c2a6e1bbff");
			int jobNumber = _vm.Load(JobId);

			sw.Stop();
			var loadTime = sw.ElapsedMilliseconds;

			sw.Restart();

			var mapSections = GetMapSections(jobNumber);
			sw.Stop();

			var fetchTime = sw.ElapsedMilliseconds;

			sw.Restart();
			var totalSectionsFound = 0;

			foreach (var mapSection in mapSections)
			{
				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;

					//MapSectionReadyGetPixelsOnly(mapSection, jobNumber, isLastSection: false);
					MapSectionReadyUiThread(mapSection, jobNumber, isLastSection: false);
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection.");
				}
			}

			sw.Stop();
			var renderTime = sw.ElapsedMilliseconds;

			Debug.WriteLine($"Ui:: Found {totalSectionsFound} sections in {loadTime}ms. Fetched in {fetchTime}ms. Drew in {renderTime}ms.");
		}

		private void MapSectionReadyUiThread(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			if (_vm.TryGetPixelArray(mapSection, out var pixelArray))
			{
				var sKBitmap = SkiaHelper.ArrayToImage(pixelArray, RMapConstants.BLOCK_SIZE);
				var skPoint = new SKPoint(mapSection.BlockPosition.X * 128, mapSection.BlockPosition.Y * 128);

				//MapSectionDispControl1.PlaceBitmap(sKBitmap, skPoint);

				MapSectionDispControl1.PlaceBitmapBuf(sKBitmap, skPoint);

				var rect = new Int32Rect((int)skPoint.X, (int)skPoint.Y, sKBitmap.Width, sKBitmap.Height);
				MapSectionDispControl1.CallForUpdate(rect);

			}
		}

		private void MapSectionReadyGetPixelsOnly(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			if (_vm.TryGetPixelArray(mapSection, out var pixelArray))
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
}
