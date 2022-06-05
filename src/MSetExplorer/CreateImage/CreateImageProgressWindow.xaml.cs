using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CreateImageProgressWindow.xaml
	/// </summary>
	public partial class CreateImageProgressWindow : Window
	{
		private CreateImageProgressViewModel _vm;

		#region Constructor

		public CreateImageProgressWindow()
		{
			_vm = (CreateImageProgressViewModel)DataContext;
			Loaded += CreateImageProgressWindow_Loaded;
			InitializeComponent();
		}

		private void CreateImageProgressWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the CreateImageProgress Window is being loaded.");
				return;
			}
			else
			{
				_vm = (CreateImageProgressViewModel)DataContext;
				progressBar1.Value = 0;
				_vm.Progress.ProgressChanged += Progress_ProgressChanged;

				Debug.WriteLine("The CreateImageProgress Window is now loaded");
			}
		}

		private void Progress_ProgressChanged(object? sender, int e)
		{
			progressBar1.Value = e;
		}

		#endregion

		#region Button Handlers

		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			_ = MessageBox.Show("Will cancel the Create Image job here.");

			Close();
		}

		#endregion
	}
}
