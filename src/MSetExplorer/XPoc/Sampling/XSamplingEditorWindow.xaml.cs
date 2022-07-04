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

namespace MSetExplorer.XPoc
{
	/// <summary>
	/// Interaction logic for XSamplingEditorWindow.xaml
	/// </summary>
	public partial class XSamplingEditorWindow : Window
	{
		private XSamplingEditorViewModel _vm;

		#region Constructor

		public XSamplingEditorWindow()
		{
			_vm = _vm = (XSamplingEditorViewModel)DataContext;

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
				Debug.WriteLine("The XSamplingEditor Window is now loaded");

			}
		}

		#endregion
	}
}
