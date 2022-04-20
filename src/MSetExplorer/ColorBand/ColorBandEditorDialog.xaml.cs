using MSS.Types;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for ColorBandEditorDialog.xaml
	/// </summary>
	public partial class ColorBandEditorDialog : Window
	{
		private ColorBand _vm;

		#region Constructor

		public ColorBandEditorDialog()
		{
			_vm = (ColorBand)DataContext;
			InitializeComponent();
			Loaded += ColorBandEditorDialog_Loaded;
		}

		private void ColorBandEditorDialog_Loaded(object sender, RoutedEventArgs e)
		{
			_vm = (ColorBand)DataContext;
			_vm.PropertyChanged += ViewModel_PropertyChanged;

			cbcBtnCtlEndColor.IsEnabled = _vm.BlendStyle == ColorBandBlendStyle.End;

			if (IsLastColorBand)
			{
				cmbBlendStyle.Items.RemoveAt(2);
			}

			Debug.WriteLine("The ColorBandEditorDialog is now loaded");
		}

		#endregion

		#region Public Properties

		public ColorBand? Sucessor { get; set; }
		public bool IsLastColorBand => Sucessor == null;

		#endregion

		#region Event Handlers

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(ColorBand.BlendStyle))
			//{
			//	_vm.ActualEndColor = ColorBandSet.GetActualEndColor(_vm, Sucessor?.StartColor);
			//	cbcBtnCtlEndColor.IsEnabled = _vm.BlendStyle == ColorBandBlendStyle.End;
			//}

			//if (e.PropertyName == nameof(ColorBand.ActualEndColor))
			//{
			//	if (_vm.BlendStyle == ColorBandBlendStyle.End)
			//	{
			//		_vm.EndColor = _vm.ActualEndColor;
			//	}
			//}

			//if (e.PropertyName == nameof(ColorBand.StartColor))
			//{
			//	if (_vm.BlendStyle == ColorBandBlendStyle.None)
			//	{
			//		_vm.ActualEndColor = _vm.StartColor;
			//	}
			//}
		}

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

		#endregion
	}
}
