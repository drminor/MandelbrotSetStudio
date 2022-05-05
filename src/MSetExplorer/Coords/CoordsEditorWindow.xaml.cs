using MSS.Common;
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

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				Debug.WriteLine("The CoordsEdito Window is now loaded");
			}
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(MapCoordsEdTestViewModel.StringVal))
			//{

			//}
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

			var precisionX = RValueHelper.GetPrecision(_vm.StartingX.RValue, _vm.EndingX.RValue);
			var newX1Sme = _vm.StartingX.SignManExp.ReducePrecisionTo(precisionX);
			var newX2Sme = _vm.EndingX.SignManExp.ReducePrecisionTo(precisionX);

			var xTest = newX1Sme.GetValueAsString();

			_vm.StartingX.StringVal = newX1Sme.GetValueAsString();
			_vm.EndingX.StringVal = newX2Sme.GetValueAsString();

			var precisionY = RValueHelper.GetPrecision(_vm.StartingX.RValue, _vm.EndingX.RValue);
			var newY1Sme = _vm.StartingY.SignManExp.ReducePrecisionTo(precisionY);
			var newY2Sme = _vm.EndingY.SignManExp.ReducePrecisionTo(precisionY);

			_vm.StartingY.StringVal = newY1Sme.GetValueAsString();
			_vm.EndingY.StringVal = newY2Sme.GetValueAsString();

			var rx1 = RNormalizer.Normalize(_vm.StartingX.RValue, _vm.StartingY.RValue, out var ry1);
			var p1 = new RPoint(rx1.Value, ry1.Value, rx1.Exponent);

			var rx2 = RNormalizer.Normalize(_vm.EndingX.RValue, _vm.EndingY.RValue, out var ry2);
			var p2 = new RPoint(rx2.Value, ry2.Value, rx2.Exponent);

			var rRectangle = new RRectangle(p1.X.Value, p2.X.Value, p1.Y.Value, p2.Y.Value, p1.Exponent);

			Debug.Print($"{rRectangle}");
		}

		#endregion

	}
}
