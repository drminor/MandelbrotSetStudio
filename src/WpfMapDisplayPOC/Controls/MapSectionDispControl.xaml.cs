using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for MapSectionDispControl.xaml
	/// </summary>
	public partial class MapSectionDispControl : UserControl, INotifyPropertyChanged
	{
		#region Private Properties

		private bool _isLoaded;

		private WriteableBitmap _bitmap;
		private SKColor _canvasClearColorEven;
		private SKColor _canvasClearColorOdd;

		private int _clearColorOpCount;

		//private readonly ConcurrentQueue<BitMapOp> _bitMapOps;

		private DebounceDispatcher _resizeThrottle;

		#endregion

		#region Constructor

		public MapSectionDispControl()
		{
			_isLoaded = false;

			_clearColorOpCount = 0;
			_canvasClearColorEven = SKColor.Parse(new SolidColorBrush(Colors.Azure).ToString());
			_canvasClearColorOdd = SKColor.Parse(new SolidColorBrush(Colors.LightSalmon).ToString());

			_resizeThrottle = new DebounceDispatcher();

			//_bitMapOps = new ConcurrentQueue<BitMapOp>();

			Loaded += MSectionDispControl_Loaded;
			SizeChanged += MSectionDispControl_SizeChanged;

			InitializeComponent();

			_bitmap = CreateBitmap();

			myImage.Source = Bitmap;
		}

		private void MSectionDispControl_Loaded(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine($"The MSectionDispControl is Loaded. Width={ActualWidth}.");

			Bitmap = CreateBitmap();
			ClearCanvas();

			_isLoaded = true;
		}

		private void MSectionDispControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (!_isLoaded)
			{
				Debug.WriteLine($"Handling SizeChanged, but we are not yet loaded. Width={ActualWidth}.");
			}
			else
			{
				Debug.WriteLine($"The MSectionDispControl is handling SizeChanged. Width={ActualWidth}.");

				_resizeThrottle.Throttle(
					interval: 500, 
					action: parm =>
						{
							Bitmap = CreateBitmap();
							ClearCanvas();
						}
					);
			}
		}

		#endregion

		#region Public Properties

		private IntPtr bitmapPtr;
		private int bWidth;
		private int bHeight;
		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				bitmapPtr = _bitmap.BackBuffer;
				bWidth = (int)_bitmap.Width;
				bHeight = (int)_bitmap.Height;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		public void ClearCanvas()
		{
			if (_bitmap != null)
			{
				SkiaHelper.ClearCanvas(_bitmap, GetClearColor());
				myImage.Source = Bitmap;
				OnPropertyChanged(nameof(Bitmap));
			}
		}

		private SKColor GetClearColor()
		{
			return ++_clearColorOpCount % 2 == 0 ? _canvasClearColorEven : _canvasClearColorOdd;
		}

		public void PlaceBitmapBuf(SKBitmap sKBitmap, SKPoint sKPoint)
		{
			SkiaHelper.PlaceBitmapBuf(bitmapPtr, bWidth, bHeight, sKBitmap, sKPoint);
		}

		public void PlaceBitmap(SKBitmap sKBitmap, SKPoint sKPoint/*, bool clearCanvas*/)
		{
			SkiaHelper.PlaceBitmap(_bitmap, sKBitmap, sKPoint);
		}

		public void PlaceBitmap(byte[] pixelArray, Int32Rect sourceRect, Point dest)
		{
			_bitmap.WritePixels(sourceRect, pixelArray, sourceRect.Width * 4, (int)dest.X, (int)dest.Y);
		}

		public void CallForUpdate(Int32Rect rect)
		{
			_bitmap.Lock();
			_bitmap.AddDirtyRect(rect);
			_bitmap.Unlock();

			OnPropertyChanged(nameof(Bitmap));
		}

		#endregion

		#region Private Methods and CLasses

		private WriteableBitmap CreateBitmap()
		{
			WriteableBitmap result;

			int width = (int)ActualWidth;
			int height = (int)ActualHeight;

			if (height > 0 && width > 0 && Parent != null)
			{
				result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
			}
			else
			{
				result = new WriteableBitmap(10, 10, 96, 96, PixelFormats.Pbgra32, null);
			}

			return result;
		}

		//protected override void ParentLayoutInvalidated(UIElement child)
		//{
		//	base.ParentLayoutInvalidated(child);
		//	BitmapGrid1.InvalidateVisual();
		//}

		//private class BitMapOp
		//{
		//	public SKBitmap SkBitmap { get; init; }
		//	public SKPoint SkPoint { get; init; }
		//	//public bool ClearCanvas { get; init; }

		//	public BitMapOp(SKBitmap skBitmap, SKPoint skPoint/*, bool clearCanvas*/)
		//	{
		//		SkBitmap = skBitmap ?? throw new ArgumentNullException(nameof(skBitmap));
		//		SkPoint = skPoint;
		//		//ClearCanvas = clearCanvas;
		//	}
		//}

		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
