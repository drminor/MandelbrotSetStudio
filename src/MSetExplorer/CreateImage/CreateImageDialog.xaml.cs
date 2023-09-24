using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CreateImageDialog.xaml
	/// </summary>
	public partial class CreateImageDialog : Window
	{
		private CreateImageViewModel _vm;

		#region Constructor

		public CreateImageDialog()
		{
			_vm = (CreateImageViewModel)DataContext;
			Loaded += CreateImageDialog_Loaded;
			InitializeComponent();
		}

		private void CreateImageDialog_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the CreateImage Dialog is being loaded.");
				return;
			}
			else
			{
				_vm = (CreateImageViewModel)DataContext;

				txtFileName.LostFocus += TxtFileName_LostFocus;

				_ = txtFileName.Focus();
				btnSave.IsEnabled = _vm.ImageFileName != null;

				Debug.WriteLine("The CreateImage Dialog is now loaded");
			}
		}

		#endregion

		#region Event Handlers

		private void TxtFileName_LostFocus(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtFileName.Text))
			{
			}
			else
			{
			}

			btnSave.IsEnabled = _vm.ImageFileName != null;
		}

		#endregion

		#region Public Properties

		#endregion

		#region Button Handlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion
	}
}
