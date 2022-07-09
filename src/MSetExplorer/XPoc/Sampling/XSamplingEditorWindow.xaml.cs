using MSetExplorer.ScreenHelpers;
using System;
using System.Diagnostics;
using System.Windows;

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

				sldrWidth.Minimum = 800;
				sldrWidth.Maximum = 3000;

				sldrWidth.SmallChange = 1;
				sldrWidth.LargeChange = 10;

				Debug.WriteLine("The XSamplingEditor Window is now loaded");

			}
		}

		#endregion

		private void sldrWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_vm.CanvasWidth = (int) Math.Round(e.NewValue);
		}


		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.Close);
			Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppNavRequestResponse = AppNavRequestResponse.BuildEmptyRequest(OnCloseBehavior.Close);
			Close();
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			//_vm.DeleteSelected();
		}

		#endregion

		public AppNavRequestResponse AppNavRequestResponse { get; private set; }

	}
}
