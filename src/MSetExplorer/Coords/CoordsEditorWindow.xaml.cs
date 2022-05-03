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

			//var mx1 = _vm.StartingX.SignManExp.Mantissa;
			//var mx2 = _vm.EndingX.SignManExp.Mantissa;
			//var my1 = _vm.StartingY.SignManExp.Mantissa;
			//var my2 = _vm.EndingY.SignManExp.Mantissa;

			//var nix = 4 + GetNumOfIdenticalDigits(mx1, mx2);
			//_vm.StartingX.StringVal = _vm.StartingX.StringVal[0..Math.Min(nix, _vm.StartingX.StringVal.Length)];
			//_vm.EndingX.StringVal = _vm.EndingX.StringVal[0..Math.Min(nix, _vm.EndingX.StringVal.Length)];

			//var niy = 4 + GetNumOfIdenticalDigits(my1, my2);
			//_vm.StartingY.StringVal = _vm.StartingY.StringVal[0..Math.Min(niy, _vm.StartingY.StringVal.Length)];
			//_vm.EndingY.StringVal = _vm.EndingY.StringVal[0..Math.Min(niy, _vm.EndingY.StringVal.Length)];

			//var rx1 = _vm.StartingX.RValue;
			//var rx2 = _vm.EndingX.RValue;

			var rx1 = RNormalizer.Normalize(_vm.StartingX.RValue, _vm.StartingY.RValue, out var ry1);

			var p1 = new RPoint(rx1.Value, ry1.Value, rx1.Exponent);

			//var ry1 = _vm.StartingY.RValue;
			//var ry2 = _vm.EndingY.RValue;

			var rx2 = RNormalizer.Normalize(_vm.EndingX.RValue, _vm.EndingY.RValue, out var ry2);
			var p2 = new RPoint(rx2.Value, ry2.Value, rx2.Exponent);

			var rRectangle = new RRectangle(p1.X.Value, p2.X.Value, p1.Y.Value, p2.Y.Value, p1.Exponent);

			Debug.Print($"{rRectangle}");
		}

		private int GetNumOfIdenticalDigits(string s1, string s2)
		{
			var cnt = Math.Min(s1.Length, s2.Length);

			var i = 0;
			for(; i < cnt; i++)
			{
				if (s1[i] != s2[i]) {
					break;
				}
			}

			return i;
		}


		#endregion

	}
}
