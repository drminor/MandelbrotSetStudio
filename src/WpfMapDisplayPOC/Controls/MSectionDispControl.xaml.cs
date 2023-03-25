using SkiaSharp;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for MSectionDispControl.xaml
	/// </summary>
	public partial class MSectionDispControl : UserControl
	{
		private bool _isLoaded;

		public MSectionDispControl()
		{
			_isLoaded = false;

			Loaded += MSectionDispControl_Loaded;
			Initialized += MSectionDispControl_Initialized;
			SizeChanged += MSectionDispControl_SizeChanged;

			InitializeComponent();
		}

		//public BitmapSource? BitmapSource
		//{
		//	get
		//	{
		//		if (_isLoaded)
		//		{
		//			return BitmapGrid1.BitmapSource;
		//		}
		//		else
		//		{
		//			return null;
		//		}
		//	}

		//	set
		//	{
		//		if (_isLoaded)
		//		{
		//			BitmapGrid1.BitmapSource = value;
		//			BitmapGrid1.InvalidateVisual();
		//		}
		//	}
		//}

		public void ClearCanvas()
		{
			if (_isLoaded)
			{
				BitmapGrid1.ClearCanvas();
			}
		}

		public void PlaceBitmap(SKBitmap sKBitmap, SKPoint sKPoint, bool clearCanvas)
		{
			if (_isLoaded)
			{
				BitmapGrid1.PlaceBitmap(sKBitmap, sKPoint, clearCanvas);
				BitmapGrid1.InvalidateVisual();

			}
		}

		private void MSectionDispControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//if (_isLoaded)
			//{
			//	BitmapGrid1.ClearCanvas();
			//	BitmapGrid1.InvalidateVisual();
			//}
		}

		protected override void ParentLayoutInvalidated(UIElement child)
		{
			base.ParentLayoutInvalidated(child);
			BitmapGrid1.InvalidateVisual();
		}

		private void MSectionDispControl_Initialized(object? sender, System.EventArgs e)
		{
			Debug.WriteLine("The MSectionDispControl is initialized.");
		}

		private void MSectionDispControl_Loaded(object sender, RoutedEventArgs e)
		{
			_isLoaded = true; 
			
			//BitmapGrid1.ClearCanvas();
			//BitmapGrid1.InvalidateVisual();
		}
	}
}
