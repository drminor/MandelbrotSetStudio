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
			InitializeComponent();

			Loaded += ColorBandEditorDialog_Loaded;
		}

		#endregion

		public ColorBand Sucessor { get; set; }
		public bool IsLastColorBand => Sucessor == null;

		#region Event Handlers

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

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBand.BlendStyle))
			{
				_vm.ActualEndColor = ColorBandSet.GetActualEndColor(_vm, Sucessor?.StartColor);
				cbcBtnCtlEndColor.IsEnabled = _vm.BlendStyle == ColorBandBlendStyle.End;
			}

			if (e.PropertyName == nameof(ColorBand.ActualEndColor))
			{
				if (_vm.BlendStyle == ColorBandBlendStyle.End)
				{
					_vm.EndColor = _vm.ActualEndColor;
				}
			}
		}

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			TakeSelection();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TakeSelection()
		{
			//var selItem = (ColorBand)DataContext;
			//if (selItem.BlendStyleUpdated && selItem.BlendStyle == ColorBandBlendStyle.End)
			//{
			//	selItem.ActualEndColor = selItem.EndColor;
			//}

			DialogResult = true;
			Close();
		}

		#endregion
	}
}
