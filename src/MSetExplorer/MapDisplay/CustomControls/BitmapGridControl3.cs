using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Security.Cryptography.Certificates;

namespace MSetExplorer
{
	//[TemplatePart(Name = "MainCanvasElement", Type = typeof(Canvas))]
	public partial class BitmapGridControl3: ContentControl
	{
		#region Private Fields

		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement? _content;

		private Canvas _canvas;
		//private Canvas _mainCanvasElement;


		private Image _image;

		private SizeDbl _viewPortSizeInternal;
		private SizeDbl _viewPortSize;

		#endregion

		#region Constructor

		static BitmapGridControl3()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(BitmapGridControl3), new FrameworkPropertyMetadata(typeof(BitmapGridControl3)));
		}

		public BitmapGridControl3()
		{
			_viewPortSizeDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_content = null;
			_canvas = new Canvas();

			//_mainCanvasElement = new Canvas();

			_image = new Image();
			//_canvas.Children.Add(_image);

			_viewPortSizeInternal = new SizeDbl();
			_viewPortSize = new SizeDbl();
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine("Image SizeChanged");
			UpdateImageOffset(ImageOffset);
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewPortSizeChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas = value;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
			}
		}

		//public Canvas MainCanvasElement
		//{
		//	get => _mainCanvasElement;

		//	set
		//	{

		//		_mainCanvasElement = value;
		//		_mainCanvasElement.ClipToBounds = CLIP_IMAGE_BLOCKS;
		//	}
		//}

		public Image Image
		{
			get => _image;
			set
			{
				if (_image != value)
				{
					_image.SizeChanged -= Image_SizeChanged;

					_image = value;
					_image.SizeChanged += Image_SizeChanged;

					_image.Source = BitmapGridImageSource;

					UpdateImageOffset(ImageOffset);
				}
			}
		}


		private bool IsImageAChildOfCanvas(Image image, Canvas canvas)
		{
			foreach(var v in canvas.Children)
			{
				if (v == image)
				{
					return true;
				}
			}

			return false;
		}

		private SizeDbl ViewPortSizeInternal
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

		//		if (_content != null)
		//		{
		//			_content.Arrange(new Rect(theNewSize));
		//		}
		//	}

		//	return theNewSize;
		//}

		//protected override Size ArrangeOverride(Size finalSizeRaw)
		//{
		//	var finalSize = ForceSize(finalSizeRaw);

		//	var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(finalSize);

		//	if (ViewPortSizeInternal != newSizeDbl)
		//	{
		//		ViewPortSizeInternal = newSizeDbl;
		//	}

		//	return finalSize;
		//}

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_content?.Measure(availableSize);

			//UpdateViewportSize(availableSize);

			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

			if (ViewPortSizeInternal != newSizeDbl)
			{
				ViewPortSizeInternal = newSizeDbl;
			}

			double width = availableSize.Width;
			double height = availableSize.Height;

			if (double.IsInfinity(width))
			{
				width = childSize.Width;
			}

			if (double.IsInfinity(height))
			{
				height = childSize.Height;
			}

			var result = new Size(width, height);

			Debug.WriteLine($"PanAndZoom Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			// TODO: Figure out when its best to call UpdateViewportSize.
			//UpdateViewportSize(childSize);
			//UpdateViewportSize(result);

			return result;
		}


		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSizeRaw)
		{
			var finalSize = ForceSize(finalSizeRaw);
			Size childSize = base.ArrangeOverride(finalSize);

			if (childSize != finalSize) Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");

			if (_content != null)
			{
				if (Canvas != null)
				{
					var canvas = Canvas;
					Debug.WriteLine($"Before _content.Arrange({finalSize}. Base returns {childSize}. The canvas size is {new Size(canvas.Width, canvas.Height)} / {new Size(canvas.ActualWidth, canvas.ActualHeight)}.");
					
					_content.Arrange(new Rect(finalSize));

					if (canvas.ActualWidth != childSize.Width)
					{
						canvas.Width = childSize.Width;
					}

					if (canvas.ActualHeight != childSize.Height)
					{
						canvas.Height = childSize.Height;
					}

					Debug.WriteLine($"After _content.Arrange(The canvas size is {new Size(canvas.Width, canvas.Height)} / {new Size(canvas.ActualWidth, canvas.ActualHeight)}.");
				}
			}

			//UpdateViewportSize(childSize);

			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			if (ViewPortSizeInternal != newSizeDbl)
			{
				ViewPortSizeInternal = newSizeDbl;
			}

			return finalSize;
		}

		private Size ForceSize(Size finalSize)
		{
			if (finalSize.Width > 1020 && finalSize.Width < 1030 && finalSize.Height > 1020 && finalSize.Height < 1030)
			{
				return new Size(1024, 1024);
			}
			else
			{
				return finalSize;
			}
		}

		public override void OnApplyTemplate()
		{
			//base.OnApplyTemplate();

			_content = Template.FindName("PART_Content", this) as FrameworkElement;
			if (_content != null)
			{
				Debug.WriteLine($"Found the BitmapGridControl3_Content template.");

				(Canvas, Image) = BuildContentModel(_content);

				Debug.Assert(IsImageAChildOfCanvas(Image, Canvas), "Image is not a child of the Canvas.");

			}
			else
			{
				Debug.WriteLine($"WARNING: Did not find the BitmapGridControl3_Content template.");
			}

			//MainCanvasElement = GetTemplateChild("MainCanvas") as Canvas ?? new Canvas();
		}

		private (Canvas, Image) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					if (ca.Children[0] is Image im)
					{
						return (ca, im);
					}
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the BitmapGrid3's Content, or the Content is not a Canvas element.");
		}

		private bool UpdateImageOffset(VectorDbl newValue)
		{
			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)Image.GetValue(Canvas.LeftProperty),
				(double)Image.GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				Image.SetValue(Canvas.LeftProperty, invertedValue.X);
				Image.SetValue(Canvas.BottomProperty, invertedValue.Y);

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
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl3),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, BitmapGridImageSource_PropertyChanged));

		private static void BitmapGridImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl3)o;
			var previousValue = (ImageSource)e.OldValue;
			var value = (ImageSource)e.NewValue;

			if (value != previousValue)
			{
				c.Image.Source = value;
			}
		}

		#endregion

		#region ImageOffset Dependency Property

		public static readonly DependencyProperty ImageOffsetProperty = DependencyProperty.Register(
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl3),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl3 c = (BitmapGridControl3)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		#endregion

	}
}
