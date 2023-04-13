using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc.BitmapGridControl;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for BitmapGridTestWindow.xaml
	/// </summary>
	public partial class BitmapGridTestWindow : Window, IHaveAppNavRequestResponse
	{
		private BitmapGridTestViewModel _vm;

		public BitmapGridTestWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = (BitmapGridTestViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += TestPanelTestWindow_Loaded;

			InitializeComponent();

			//BitmapGridControl1.ViewPortSizeInBlocksChanged += BitmapGridControl1_ViewPortSizeInBlocksChanged;
			BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

		}

		private void TestPanelTestWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the TestPanelTestWindow Window is being loaded.");
				return;
			}
			else
			{
				_vm = (BitmapGridTestViewModel)DataContext;
				ReportSizes("Loaded.");
				Debug.WriteLine("The TestPanelTestWindow Window is now loaded");

			}
		}

		#region BitmapGridControl Handlers

		//private void BitmapGridControl1_ViewPortSizeInBlocksChanged(object? sender, (SizeInt, SizeInt) e)
		//{
		//	Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort SizeInBlocks Changed. Prev: {e.Item1}, New: {e.Item2}.");
		//}

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		{
			Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort Size Changed. Prev: {e.Item1}, New: {e.Item2}.");
		}

		#endregion

		#region Button Handlers

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.ReturnToTopNav);
			Close();
		}

		private void ExitButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.Close);
			Close();
		}

		#endregion

		private void ReportSizes(string label)
		{
			var bmgcSize = new SizeInt(BitmapGridControl1.ActualWidth, BitmapGridControl1.ActualHeight);

			//var cSize = new SizeInt(MainCanvas.ActualWidth, MainCanvas.ActualHeight);
			var cSize = new SizeInt();

			//var iSize = new SizeInt(myImage.ActualWidth, myImage.ActualHeight);
			var iSize = new SizeInt();

			//var bSize = _vm == null ? new SizeInt() : new SizeInt(_vm.Bitmap.Width, _vm.Bitmap.Height);
			var bSize = new SizeInt();

			Debug.WriteLine($"At {label}, the sizes are BmGrid: {bmgcSize}, Canvas: {cSize}, Image: {iSize}, Bitmap: {bSize}.");
		}

	}

}
