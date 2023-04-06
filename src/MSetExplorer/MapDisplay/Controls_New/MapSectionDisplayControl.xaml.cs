using MSetExplorer.XPoc;
using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionDisplayControl : UserControl
	{
		private MapSectionDisplayViewModel _vm;

		public MapSectionDisplayControl()
		{
			_vm = (MapSectionDisplayViewModel)DataContext;

			Loaded += MapSectionDisplayControl_Loaded;

			InitializeComponent();

			BitmapGridControl1.ViewPortSizeInBlocksChanged += BitmapGridControl1_ViewPortSizeInBlocksChanged;
			BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

		}

		private void MapSectionDisplayControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapSectionDisplayControl is being loaded.");
				return;
			}
			else
			{
				_vm = (MapSectionDisplayViewModel)DataContext;
				ReportSizes("Loaded.");
				Debug.WriteLine("The MapSectionDisplayControlis now loaded");

			}
		}


		#region BitmapGridControl Handlers

		private void BitmapGridControl1_ViewPortSizeInBlocksChanged(object? sender, (SizeInt, SizeInt) e)
		{
			Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort SizeInBlocks Changed. Prev: {e.Item1}, New: {e.Item2}.");
		}

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (Size, Size) e)
		{
			Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort Size Changed. Prev: {e.Item1}, New: {e.Item2}.");
		}

		#endregion

		private void ReportSizes(string label)
		{
			var bmgcSize = new SizeInt(BitmapGridControl1.ActualWidth, BitmapGridControl1.ActualHeight);

			//var cSize = new SizeInt(MainCanvas.ActualWidth, MainCanvas.ActualHeight);
			var cSize = new SizeInt();

			//var iSize = new SizeInt(myImage.ActualWidth, myImage.ActualHeight);
			var iSize = new SizeInt();

			//var bSize = _vm == null ? new SizeInt() : new SizeInt(_vm.Bitmap.Width, _vm.Bitmap.Height);
			var bSize = new SizeInt();

			Debug.WriteLine($"At {label}, the sizes are BmGrid: {bmgcSize}, Canvas: {cSize}, Image: {iSize}, Bitmap: {bSize}.");
		}


	}
}
