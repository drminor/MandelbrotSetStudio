using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapDisplayZoomControl.xaml
	/// </summary>
	public partial class MapDisplayZoomControl : UserControl
	{
		#region Constructor

		public MapDisplayZoomControl()
		{
			Loaded += MapDisplayZoomControl_Loaded;
			InitializeComponent();
		}

		private void MapDisplayZoomControl_Loaded(object sender, RoutedEventArgs e)
		{
			var minimumScale = 0.0625;
			SetScrollBarSettings(scrollBar1, minimumScale);
			scrollBar1.ValueChanged += ScrollBar1_ValueChanged;
			Debug.WriteLine("The MapDisplayZoom Control is now loaded.");
		}

		private void ScrollBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			textBlock1.Text = e.NewValue.ToString("F3");
		}

		#endregion

		#region Min Max Button Handlers

		private void ButtonSetMaxZoom_Click(object sender, RoutedEventArgs e)
		{
			scrollBar1.Value = scrollBar1.Maximum;
		}

		private void ButtonSetMinZoom_Click(object sender, RoutedEventArgs e)
		{
			scrollBar1.Value = scrollBar1.Minimum;
		}

		#endregion

		private void SetScrollBarSettings(ScrollBar sb, double minimumScale)
		{
			sb.Minimum = minimumScale;
			sb.Value = 1;

			sb.Maximum = 1;
			sb.SmallChange = minimumScale;
			sb.LargeChange = minimumScale * 2;

			sb.Value = 1;
		}

		//private double GetZoomValue(ScrollEventType et, double val)
		//{
		//	return et switch
		//	{
		//		ScrollEventType.EndScroll => val,
		//		ScrollEventType.First => val, // _vm.MaximumDisplayZoom,
		//		ScrollEventType.LargeDecrement => val,
		//		ScrollEventType.LargeIncrement => val,
		//		ScrollEventType.Last => 1,
		//		ScrollEventType.SmallDecrement => val,
		//		ScrollEventType.SmallIncrement => val,
		//		ScrollEventType.ThumbPosition => val,
		//		ScrollEventType.ThumbTrack => val,
		//		_ => -1,
		//	};
		//}
	}
}
