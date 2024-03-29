﻿using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CoordsEditorWindow.xaml
	/// </summary>
	public partial class CoordsEditorWindow : Window
	{
		private CoordsEditorViewModel? _vm;

		#region Constructor
		
		public CoordsEditorWindow()
		{
			Loaded += CoordsEditorWindow_Loaded;
			InitializeComponent();
		}

		private void CoordsEditorWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the CoordsEditor Window is being loaded.");
				return;
			}
			else
			{
				_vm = (CoordsEditorViewModel)DataContext;

				_vm.StartingX.ValueName = "X1:";
				x1.DataContext = _vm.StartingX;

				_vm.EndingX.ValueName = "X2:";
				x2.DataContext = _vm.EndingX;

				_vm.StartingY.ValueName = "Y1:";
				y1.DataContext = _vm.StartingY;

				_vm.EndingY.ValueName = "Y2:";
				y2.DataContext = _vm.EndingY;

				mapCoordsNormalized.DataContext = _vm.MapCoordsDetail1;
				_vm.MapCoordsDetail1.HeaderName = "Normalized Coordinates";

				mapCoordsAdjusted.DataContext = _vm.MapCoordsDetail2;
				_vm.MapCoordsDetail2.HeaderName = "Adjusted Coordinates";

				btnSave.Visibility = _vm.EditsAllowed ? Visibility.Visible : Visibility.Collapsed;

				Debug.WriteLine("The CoordsEditor Window is now loaded");
			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TestButton_Click(object sender, RoutedEventArgs e)
		{
			if (_vm == null)
			{
				return;
			}

			var rRectangle = _vm.Coords;
			Debug.Print($"{rRectangle}");
		}

		#endregion

	}
}
