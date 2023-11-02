using MSetExplorer.ScreenHelpers;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for SamplePointDeltaTestWindow.xaml
	/// </summary>
	public partial class SamplePointDeltaTestWindow : Window, IHaveAppNavRequestResponse
	{
		private SamplePointDeltaTestViewModel _vm;


		#region Constructor

		public SamplePointDeltaTestWindow(AppNavRequestResponse appNavRequestResponse)
		{
			_vm = (SamplePointDeltaTestViewModel)DataContext;
			AppNavRequestResponse = appNavRequestResponse;

			Loaded += SamplePointDeltaTestWindow_Loaded;
			InitializeComponent();
		}

		private void SamplePointDeltaTestWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the SamplePointDeltaTest Window is being loaded.");
				return;
			}
			else
			{
				_vm = (SamplePointDeltaTestViewModel)DataContext;

				PrepareCanvasSizeSection();
				PrepareZoomFactorSection();

				_vm.SelectionSize = new SizeDbl(16);

				Debug.WriteLine("The SamplePointDeltaTest Window is now loaded");
			}
		}

		private void PrepareCanvasSizeSection()
		{
			//_vm.MapAreaInfoViewModelCanS.SectionTitle = "Canvas Size";
			//mapAreaInfoCanS.DataContext = _vm.MapAreaInfoViewModelCanS;

			//_vm.MapAreaInfoViewModelCanN.SectionTitle = "Canvas Size Normalized";
			//mapAreaInfoCanN.DataContext = _vm.MapAreaInfoViewModelCanN;

			var minWidth = 128 * 4;             // 512

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

		private void PrepareZoomFactorSection()
		{
			//_vm.MapAreaInfoViewModelSelS.SectionTitle = "Selection Size";
			//mapAreaInfoSelS.DataContext = _vm.MapAreaInfoViewModelSelS;

			//_vm.MapAreaInfoViewModelSelN.SectionTitle = "Selection Size Normalized";
			//mapAreaInfoSelN.DataContext = _vm.MapAreaInfoViewModelSelN;


			sldrSelectionWidthPercentage.Minimum = 0;
			sldrSelectionWidthPercentage.Maximum = 100;

			sldrSelectionWidthPercentage.TickFrequency = 0.125;
			sldrSelectionWidthPercentage.SmallChange = 0.125;
			sldrSelectionWidthPercentage.LargeChange = 1;
			sldrSelectionWidthPercentage.TickPlacement = TickPlacement.None;

			sldrSelectionWidthPercentage.Value = 12.5;
			_vm.SelectionWidthPercentage = 12.5;

			sldrSelectionWidthPercentage.ValueChanged += SldrSelectionWidthPercentage_ValueChanged;
		}

		#endregion

		#region Event Handlers

		private void SldrCanvasWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.ScreenSize = new SizeInt((int)Math.Round(e.NewValue));
		}

		private void SldrSelectionWidthPercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.SelectionWidthPercentage = e.NewValue;
		}

		#endregion

		#region Button Handlers

		private void SelWidthPerNumeratorUpButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.SelectionWidthPerNumerator += 1;
		}


		private void SelWidthPerNumeratorDownButton_Click(object sender, RoutedEventArgs e)
		{
			_vm.SelectionWidthPerNumerator -= 1;
		}

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

		private void Button_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
