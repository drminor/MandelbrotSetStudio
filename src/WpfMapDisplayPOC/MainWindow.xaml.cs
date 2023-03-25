using SkiaSharp;
using System;
using System.Diagnostics;
using System.Windows;
using MSS.Common;
using MSS.Types;

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


		#endregion

		#region Button Handlers

		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{
			var sw = Stopwatch.StartNew();

			//int jobNumber = _vm.Load("641b56e43811e2c2a6e1bbff");
			int jobNumber = _vm.Load("641b58493811e2c2a6e1c18c");

			var totalSectionsFound = 0;
			var totalSectionsDrawn = 0;

			var clearCanvas = true;

			foreach (var mapSectionRequest in _vm.MapSectionRequests)
			{
				var mapSection = _vm.GetMapSection(mapSectionRequest, jobNumber);

				if (mapSection != null)
				{
					//Debug.WriteLine($"Found MapSection with Screen Position: {mapSection.BlockPosition}.");
					totalSectionsFound++;

					if (_vm.TryGetPixelArray(mapSection, out var pixelArray))
					{
						totalSectionsDrawn++;

						var sKBitmap = SkiaHelper.ArrayToImage(pixelArray, RMapConstants.BLOCK_SIZE);
						var skPoint = new SKPoint(mapSection.BlockPosition.X, mapSection.BlockPosition.Y);
						MSectionDispControl1.PlaceBitmap(sKBitmap, skPoint, clearCanvas);
						clearCanvas = false;

						//break;
					}
				}
				else
				{
					Debug.WriteLine($"Cound not find MapSection with Screen Position: {mapSectionRequest.ScreenPosition}.");
				}
			}

			sw.Stop();
			var tMilliseconds = sw.ElapsedMilliseconds;

			Debug.WriteLine($"Drew {totalSectionsDrawn} sections. Found {totalSectionsFound} sections in {tMilliseconds}ms.");
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			//DialogResult = false;
			Close();
		}

		#endregion
	}
}
