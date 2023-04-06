using MSetExplorer.ScreenHelpers;
using MSetExplorer.XPoc.BitmapGridControl;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
			Initialized += BitmapGridTestWindow_Initialized;
			InitializeComponent();
		}

		private void BitmapGridTestWindow_Initialized(object? sender, EventArgs e)
		{
			ReportSizes("Initialized.");
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
			var cSize = new SizeInt(MainCanvas.ActualWidth, MainCanvas.ActualHeight);
			var iSize = new SizeInt(myImage.ActualWidth, myImage.ActualHeight);

			var bSize = _vm == null ? new SizeInt() : new SizeInt(_vm.Bitmap.Width, _vm.Bitmap.Height);

			Debug.WriteLine($"At {label}, the sizes are BmGrid: {bmgcSize}, Canvas: {cSize}, Image: {iSize}, Bitmap: {bSize}.");
		}

	}

}
