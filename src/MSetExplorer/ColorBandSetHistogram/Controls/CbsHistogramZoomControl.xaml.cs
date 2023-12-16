using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for CbshZoomControl.xaml
	/// </summary>
	public partial class CbshZoomControl : UserControl
	{
		#region Constructor

		public CbshZoomControl()
		{
			Loaded += CbshZoomControl_Loaded;
			InitializeComponent();
		}

		private void CbshZoomControl_Loaded(object sender, RoutedEventArgs e)
		{
			var maximumScale = 4;
			SetScrollBarSettings(scrollBar1, maximumScale);

			textBlock1.Text = scrollBar1.Value.ToString("F3");

			scrollBar1.ValueChanged += ScrollBar1_ValueChanged;

			Debug.WriteLine("The CbshZoom Control is now loaded.");
		}

		private void ScrollBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			textBlock1.Text = e.NewValue.ToString("F3");
		}

		private void SetScrollBarSettings(ScrollBar sb, double maximumScale)
		{
			sb.Minimum = 1;

			sb.Maximum = maximumScale;
			sb.SmallChange = 0.02;
			sb.LargeChange = 0.5;

			sb.Value = 1;
		}

		#endregion


	}
}
