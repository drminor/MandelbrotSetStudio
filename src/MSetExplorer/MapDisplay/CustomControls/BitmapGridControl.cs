using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public partial class BitmapGridControl : Image
	{
		#region Private Fields

		private DebounceDispatcher _viewPortSizeDispatcher;

		//private FrameworkElement? _content;
		//private Canvas _canvas;
		//private Image _image;

		private SizeDbl _viewPortSizeInternal;
		private SizeDbl _viewPortSize;

		#endregion

		#region Constructor

		static BitmapGridControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl), new FrameworkPropertyMetadata(typeof(BitmapGridControl)));
		}

		public BitmapGridControl()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			//_content = null;
			//_canvas = new Canvas();
			//_image = new Image();
			//_canvas.Children.Add(_image);

			SizeChanged += BitmapGridControl3_SizeChanged;

			_viewPortSizeInternal = new SizeDbl();
			_viewPortSize = new SizeDbl();
		}

		private void BitmapGridControl3_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("Image SizeChanged");
			UpdateImageOffset(ImageOffset);
		}

		//private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//	Debug.WriteLine("Image SizeChanged");
		//	UpdateImageOffset(ImageOffset);
		//}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;

		#endregion

		#region Public Properties

		//public Canvas Canvas
		//{
		//	get => _canvas;
		//	set => _canvas = value;
		//}

		//public Image Image
		//{
		//	get => _image;
		//	set
		//	{
		//		if (_image != value)
		//		{
		//			_image.SizeChanged -= Image_SizeChanged;

		//			_image = value;
		//			_image.SizeChanged += Image_SizeChanged;

		//			_image.Source = BitmapGridImageSource;

		//			UpdateImageOffset(ImageOffset);
		//		}
		//	}
		//}

		public SizeDbl ViewPortSizeInternal
		{
			get => _viewPortSizeInternal;
			set
			{
				if (value.Width > 1 && value.Height > 1 && _viewPortSizeInternal != value)
				{
					var previousValue = _viewPortSizeInternal;
					_viewPortSizeInternal = value;

					//Debug.WriteLine($"BitmapGridControl: ViewPort is changing: Old size: {previousValue}, new size: {_viewPort}.");

					var newViewPortSize = value;

					if (previousValue.Width < 25 || previousValue.Height < 25)
					{
						// Update the 'real' value immediately
						Debug.WriteLine($"Updating the ViewPortSize immediately. Previous Size: {previousValue}, New Size: {value}.");
						ViewPortSize = newViewPortSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
						var tempOffset = GetTempImageOffset(ImageOffset, ViewPortSize, newViewPortSize);
						_ = UpdateImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLine($"Updating the ViewPortSize after debounce. Previous Size: {ViewPortSize}, New Size: {newViewPortSize}.");
								ViewPortSize = newViewPortSize;
							},
							param: null
						);
					}
				}
				else
				{
					Debug.WriteLine($"Skipping the update of the ViewPortSize, the new value {value} is the same as the old value. {ViewPortSizeInternal}.");
				}
			}
		}

		public SizeDbl ViewPortSize
		{
			get => _viewPortSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewPortSize, value))
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewPortSize updated to {value}, the current value is {_viewPortSize}; will raise the ViewPortSizeChanged event.");

					var previousValue = ViewPortSize;
					_viewPortSize = value;

					Debug.Assert(_viewPortSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					//UpdateViewportSize()

					ViewPortSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewPortSize updated to {value}, the current value is already: {_viewPortSize}; not raising the ViewPortSizeChanged event.");
				}
			}
		}

		public ImageSource BitmapGridImageSource
		{
			get => (ImageSource)GetValue(BitmapGridImageSourceProperty);
			set => SetCurrentValue(BitmapGridImageSourceProperty, value);
		}

		public VectorDbl ImageOffset
		{
			get => (VectorDbl)GetValue(ImageOffsetProperty);
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(ImageOffset, value))
				{
					SetCurrentValue(ImageOffsetProperty, value);
				}
			}
		}

		#endregion

		#region Private Methods - Control

		//protected override Size MeasureOverride(Size constraint)
		//{
		//	Size infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
		//	Size childSize = base.MeasureOverride(infiniteSize);

		//	double width = constraint.Width;
		//	double height = constraint.Height;

		//	if (double.IsInfinity(width))
		//	{
		//		// Make sure we don't return infinity!
		//		width = childSize.Width;
		//	}

		//	if (double.IsInfinity(height))
		//	{
		//		// Make sure we don't return infinity!
		//		height = childSize.Height;
		//	}

		//	// Update the size of the viewport onto the content based on the passed in 'constraint'.
		//	var theNewSize = new Size(width, height);

		//	var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(theNewSize);

		//	if (ViewPortSizeInternal != newSizeDbl)
		//	{
		//		ViewPortSizeInternal = newSizeDbl;

		//		//if (_content != null)
		//		//{
		//		//	_content.Arrange(new Rect(theNewSize));
		//		//}
		//	}

		//	return theNewSize;
		//}

		//protected override Size ArrangeOverride(Size finalSize)
		//{
		//	var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(finalSize);

		//	if (ViewPortSizeInternal != newSizeDbl)
		//	{
		//		ViewPortSizeInternal = newSizeDbl;
		//	}

		//	return finalSize;
		//}

		//public override void OnApplyTemplate()
		//{
		//	base.OnApplyTemplate();

		//	_content = Template.FindName("BitmapGridControl_Content", this) as FrameworkElement;
		//	if (_content != null)
		//	{
		//		Debug.WriteLine($"Found the BitmapGridControl_Content template.");

		//		(Canvas, Image) = BuildContentModel(_content);
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"WARNING: Did not find the BitmapGridControl_Content template.");
		//	}
		//}

		//private (Canvas, Image) BuildContentModel(FrameworkElement content)
		//{
		//	if (content is ContentPresenter cp)
		//	{
		//		if (cp.Content is Canvas ca)
		//		{
		//			//return ca;
		//			if (ca.Children[0] is Image im)
		//			{
		//				return (ca, im);
		//			}
		//		}
		//	}

		//	throw new InvalidOperationException("Cannot find the bmgcImage element on the BitmapGridControl_Content.");
		//}

		private bool UpdateImageOffset(VectorDbl newValue)
		{
			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)GetValue(Canvas.LeftProperty),
				(double)GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				SetValue(Canvas.LeftProperty, invertedValue.X);
				SetValue(Canvas.BottomProperty, invertedValue.Y);

				return true;
			}
			else
			{
				return false;
			}
		}

		private VectorDbl GetTempImageOffset(VectorDbl originalOffset, SizeDbl originalSize, SizeDbl newSize)
		{
			var diff = newSize.Diff(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		#endregion

		#region BitmapGridImageSource Dependency Property

		public static readonly DependencyProperty BitmapGridImageSourceProperty = DependencyProperty.Register(
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, BitmapGridImageSource_PropertyChanged));

		private static void BitmapGridImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl)o;
			var previousValue = (ImageSource)e.OldValue;
			var value = (ImageSource)e.NewValue;

			if (value != previousValue)
			{
				//c.Image.Source = value;
				c.Source = value;
			}
		}

		#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		#endregion

	}
}
