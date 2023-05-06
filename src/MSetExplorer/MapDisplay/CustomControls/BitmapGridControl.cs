using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	//[TemplatePart(Name = "MainCanvasElement", Type = typeof(Canvas))]
	public partial class BitmapGridControl: ContentControl
	{
		#region Private Fields

		private readonly static bool CLIP_IMAGE_BLOCKS = false;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement? _content;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewportSizeInternal;
		private SizeDbl _viewportSize;


		private SizeDbl _contentViewportSize;



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

			_content = null;
			_canvas = new Canvas();

			_image = new Image();

			_viewportSizeInternal = new SizeDbl();
			_viewportSize = new SizeDbl();
			
		}

		private void RenderTransform_Changed(object? sender, EventArgs e)
		{
			if (RenderTransform is ScaleTransform st)
			{
				var scaleX = st.ScaleX;
				Debug.WriteLine($"The BitmapGrid's RenderTransform was updated. The new value for scaleX is {scaleX}.");
			}
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine("Image SizeChanged");
			UpdateImageOffset(ImageOffset);
			Debug.WriteLine($"The canvas's RenderTransform is {Canvas.RenderTransform}.");
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

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

					Debug.Assert(IsImageAChildOfCanvas(Image, Canvas), "Image is not a child of the Canvas.");
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

		private SizeDbl ViewportSizeInternal
		{
			get => _viewportSizeInternal;
			set
			{
				if (value.Width > 1 && value.Height > 1 && _viewportSizeInternal != value)
				{
					var previousValue = _viewportSizeInternal;
					_viewportSizeInternal = value;

					//Debug.WriteLine($"BitmapGridControl: Viewport is changing: Old size: {previousValue}, new size: {_viewPort}.");

					var newViewportSize = value;

					if (previousValue.Width < 25 || previousValue.Height < 25)
					{
						// Update the 'real' value immediately
						Debug.WriteLine($"Updating the ViewportSize immediately. Previous Size: {previousValue}, New Size: {value}.");
						ViewportSize = newViewportSize;
					}
					else
					{
						// Update the screen immediately, while we are 'holding' back the update.
						//Debug.WriteLine($"CCO_Int: {value.Invert()}.");
						var tempOffset = GetTempImageOffset(ImageOffset, ViewportSize, newViewportSize);
						_ = UpdateImageOffset(tempOffset);

						// Delay the 'real' update until no futher updates in the last 150ms.
						_viewPortSizeDispatcher.Debounce(
							interval: 150,
							action: parm =>
							{
								Debug.WriteLine($"Updating the ViewportSize after debounce. Previous Size: {ViewportSize}, New Size: {newViewportSize}.");
								ViewportSize = newViewportSize;
							},
							param: null
						);
					}
				}
				else
				{
					Debug.WriteLine($"Skipping the update of the ViewportSize, the new value {value} is the same as the old value. {ViewportSizeInternal}.");
				}
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					Debug.Assert(_viewportSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					//UpdateViewportSize()

					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
				}
			}
		}

		public SizeDbl ContentViewportSize
		{
			get => _contentViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ContentViewportSize, value))
				{
					_contentViewportSize = value;
					_canvas.Width = _contentViewportSize.Width;
					_canvas.Height = _contentViewportSize.Height;
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

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_content?.Measure(availableSize);

			//UpdateViewportSize(availableSize);

			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

			if (ViewportSizeInternal != newSizeDbl)
			{
				ViewportSizeInternal = newSizeDbl;
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
					//Debug.WriteLine($"Before _content.Arrange({finalSize}. Base returns {childSize}. The canvas size is {new Size(canvas.Width, canvas.Height)} / {new Size(canvas.ActualWidth, canvas.ActualHeight)}.");
					
					_content.Arrange(new Rect(finalSize));

					if (canvas.ActualWidth != childSize.Width)
					{
						canvas.Width = childSize.Width;
					}

					if (canvas.ActualHeight != childSize.Height)
					{
						canvas.Height = childSize.Height;
					}

					//Debug.WriteLine($"After _content.Arrange(The canvas size is {new Size(canvas.Width, canvas.Height)} / {new Size(canvas.ActualWidth, canvas.ActualHeight)}.");
				}
			}

			//UpdateViewportSize(childSize);

			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			if (ViewportSizeInternal != newSizeDbl)
			{
				ViewportSizeInternal = newSizeDbl;
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
				//Debug.WriteLine($"Found the BitmapGridControl3_Content template.");

				(Canvas, Image) = BuildContentModel(_content);

				Debug.Assert(IsImageAChildOfCanvas(Image, Canvas), "Image is not a child of the Canvas.");

				Content = _content;
			}
			else
			{
				//Debug.WriteLine($"WARNING: Did not find the BitmapGridControl_Content template.");
				throw new InvalidOperationException("Did not find the BitmapGridControl_Content template.");

			}
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
					"BitmapGridImageSource", typeof(ImageSource), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None, BitmapGridImageSource_PropertyChanged));

		private static void BitmapGridImageSource_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			var c = (BitmapGridControl)o;
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
					"ImageOffset", typeof(VectorDbl), typeof(BitmapGridControl),
					new FrameworkPropertyMetadata(VectorDbl.Zero, FrameworkPropertyMetadataOptions.None, ImageOffset_PropertyChanged));

		private static void ImageOffset_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			BitmapGridControl c = (BitmapGridControl)o;
			//var previousValue = (VectorDbl)e.OldValue;
			var newValue = (VectorDbl)e.NewValue;

			_ = c.UpdateImageOffset(newValue);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
		}

		protected override void ParentLayoutInvalidated(UIElement child)
		{
			base.ParentLayoutInvalidated(child);
		}

		#endregion

	}
}
