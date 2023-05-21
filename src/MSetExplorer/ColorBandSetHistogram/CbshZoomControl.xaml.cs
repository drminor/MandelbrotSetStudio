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
			var minimumScale = 0.0625;
			SetScrollBarSettings(scrollBar1, minimumScale);

			Debug.WriteLine("The CbshZoom Control is now loaded.");
		}

		private void SetScrollBarSettings(ScrollBar sb, double minimumScale)
		{
			sb.Minimum = minimumScale;
			sb.Value = 1;

			sb.Maximum = 1;
			sb.SmallChange = minimumScale;
			sb.LargeChange = minimumScale * 2;

			sb.Value = 1;
		}

		#endregion


	}
}
