using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfMapDisplayPOC
{
	/// <summary>
	/// Interaction logic for MSectionDispControl.xaml
	/// </summary>
	public partial class MSectionDispControl : UserControl
	{
		#region Private Properties

		private bool _isLoaded;

		private readonly ConcurrentQueue<BitMapOp> _bitMapOps;

		#endregion

		#region Constructor

		public MSectionDispControl()
		{
			_isLoaded = false;

			_bitMapOps = new ConcurrentQueue<BitMapOp>();	

			Loaded += MSectionDispControl_Loaded;
			Initialized += MSectionDispControl_Initialized;
			SizeChanged += MSectionDispControl_SizeChanged;

			InitializeComponent();

			DispatcherTimer timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(20);
			timer.Tick += Timer_Tick;
			timer.Start();
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			var cntr = 0;

			while(cntr < 15)
			{
				if (_bitMapOps.TryDequeue(out var bitMapOp))
				{
					BitmapGrid1.PlaceBitmap(bitMapOp.SkBitmap, bitMapOp.SkPoint);
					cntr++;
				}
				else
				{
					break;
				}

			}

			if (cntr > 0)
			{
				BitmapGrid1.InvalidateVisual();
			}
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

		private void MSectionDispControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//if (_isLoaded)
			//{
			//	BitmapGrid1.ClearCanvas();
			//	BitmapGrid1.InvalidateVisual();
			//}
		}

		#endregion

		#region Public Methods

		public void ClearCanvas()
		{
			if (_isLoaded)
			{
				BitmapGrid1.ClearCanvas();
			}
		}

		public void PlaceBitmap(SKBitmap sKBitmap, SKPoint sKPoint/*, bool clearCanvas*/)
		{
			_bitMapOps.Enqueue(new BitMapOp(sKBitmap, sKPoint/*, clearCanvas*/));

			//if (_isLoaded)
			//{
			//	BitmapGrid1.PlaceBitmap(sKBitmap, sKPoint, clearCanvas);
			//	BitmapGrid1.InvalidateVisual();
			//}
		}

		#endregion

		#region Private Methods and CLasses



		protected override void ParentLayoutInvalidated(UIElement child)
		{
			base.ParentLayoutInvalidated(child);
			BitmapGrid1.InvalidateVisual();
		}

		private class BitMapOp
		{
			public SKBitmap SkBitmap { get; init; }
			public SKPoint SkPoint { get; init; }
			//public bool ClearCanvas { get; init; }

			public BitMapOp(SKBitmap skBitmap, SKPoint skPoint/*, bool clearCanvas*/)
			{
				SkBitmap = skBitmap ?? throw new ArgumentNullException(nameof(skBitmap));
				SkPoint = skPoint;
				//ClearCanvas = clearCanvas;
			}

		}

		#endregion
	}
}
