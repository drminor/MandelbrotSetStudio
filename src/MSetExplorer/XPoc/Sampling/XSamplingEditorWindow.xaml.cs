using MSetExplorer.ScreenHelpers;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for XSamplingEditorWindow.xaml
	/// </summary>
	public partial class XSamplingEditorWindow : Window, IHaveAppNavRequestResponse
	{
		private XSamplingEditorViewModel _vm;

		#region Constructor

		public XSamplingEditorWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = _vm = (XSamplingEditorViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += XSamplingEditorWindow_Loaded;
			InitializeComponent();
		}

		private void XSamplingEditorWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the XSamplingEditor Window is being loaded.");
				return;
			}
			else
			{
				_vm = (XSamplingEditorViewModel)DataContext;

				PrepareMapAreaInfo1();
				PrepareMapAreaInfo2();

				Debug.WriteLine("The XSamplingEditor Window is now loaded");
			}
		}

		private void PrepareMapAreaInfo1()
		{
			mapAreaInfo1.DataContext = _vm.MapAreaInfoViewModel1;

			var minWidth = 128 * 4;

			sldrCanvasWidth.Minimum = minWidth;
			sldrCanvasWidth.Maximum = 128 * 17; // 2048 + 128

			sldrCanvasWidth.TickFrequency = 16;
			sldrCanvasWidth.SmallChange = 16;
			sldrCanvasWidth.LargeChange = 128;
			sldrCanvasWidth.TickPlacement = TickPlacement.Both;

			sldrCanvasWidth.Value = 1024;
			_vm.ScreenSize = new SizeInt(1024);

			sldrCanvasWidth.ValueChanged += SldrCanvasWidth_ValueChanged;
		}

		private void PrepareMapAreaInfo2()
		{
			mapAreaInfo2.DataContext = _vm.MapAreaInfoViewModel2;

			var minWidth = 16;

			sldrSelectionWidth.Minimum = minWidth;
			sldrSelectionWidth.Maximum = 16 * 64;

			sldrSelectionWidth.TickFrequency = 16;
			sldrSelectionWidth.SmallChange = 16;
			sldrSelectionWidth.LargeChange = 16;
			sldrSelectionWidth.TickPlacement = TickPlacement.Both;

			sldrSelectionWidth.Value = 16;
			_vm.SelectionSize = new SizeDbl(16);

			sldrSelectionWidth.ValueChanged += SldrSelectionWidth_ValueChanged;
		}

		#endregion

		#region Event Handlers

		private void SldrCanvasWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.ScreenSize = new SizeInt((int)Math.Round(e.NewValue));
		}

		private void SldrSelectionWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.SelectionSize = new SizeDbl(e.NewValue);
		}

		#endregion

		#region Button Handlers

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

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }
	}
}
