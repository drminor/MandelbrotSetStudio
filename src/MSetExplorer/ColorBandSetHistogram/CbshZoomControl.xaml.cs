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
			var maximumScale = 10;
			SetScrollBarSettings(scrollBar1, maximumScale);

			Debug.WriteLine("The CbshZoom Control is now loaded.");
		}

		private void SetScrollBarSettings(ScrollBar sb, double maximumScale)
		{
			sb.Minimum = 1;

			sb.Maximum = maximumScale;
			sb.SmallChange = sb.Minimum;
			sb.LargeChange = sb.Minimum * 2;

			sb.Value = 1;
		}

		#endregion


	}
}
