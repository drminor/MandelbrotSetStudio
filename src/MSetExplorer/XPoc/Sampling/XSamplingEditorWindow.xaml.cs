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

				sldrWidth.Minimum = 128 * 4;
				sldrWidth.Maximum = 128 * 20;

				sldrWidth.Value = 512;

				sldrWidth.TickFrequency = 1; // (sldrWidth.Maximum - sldrWidth.Minimum) / 10;
				sldrWidth.SmallChange = 1;
				sldrWidth.LargeChange = 128;
				sldrWidth.TickPlacement = TickPlacement.None;

				sldrWidth.ValueChanged += SldrWidth_ValueChanged;

				_vm.ScreenSize = new SizeInt(512);

				Debug.WriteLine("The XSamplingEditor Window is now loaded");
			}
		}

		#endregion

		private void SldrWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.ScreenSize = new SizeInt((int)Math.Round(e.NewValue));
		}

		#region Button Handlers

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.ReturnToTopNav);
			Close();
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

	}
}
