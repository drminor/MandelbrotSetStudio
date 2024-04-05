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
	/// Interaction logic for PosterDetailsEditorWindow.xaml
	/// </summary>
	public partial class PosterDetailsEditorWindow : Window
	{
		private PosterDetailsViewModel _vm;

		#region Constructor 

		public PosterDetailsEditorWindow()
		{
			_vm = (PosterDetailsViewModel)DataContext;

			Loaded += PosterDetailsEditorWindow_Loaded;
			ContentRendered += PosterDetailsEditorWindow_ContentRendered;
			InitializeComponent();
		}

		private void PosterDetailsEditorWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the PosterDetailsEditor Window is being loaded.");
				return;
			}
			else
			{
				_vm = (PosterDetailsViewModel)DataContext;
				//borderDetails.DataContext = DataContext;

				_vm.PropertyChanged += PosterDetailsViewModel_PropertyChanged;


				Debug.WriteLine("The PosterDetailsEditor Window is now loaded");
			}
		}

		private void PosterDetailsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{

		}

		#endregion

		#region Event Handlers

		private void PosterDetailsEditorWindow_ContentRendered(object? sender, EventArgs e)
		{

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

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion


	}
}
