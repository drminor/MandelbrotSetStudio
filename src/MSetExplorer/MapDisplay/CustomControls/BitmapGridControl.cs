﻿using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	//[TemplatePart(Name = "MainCanvasElement", Type = typeof(Canvas))]
	public partial class BitmapGridControl: ContentControl, IContentScaler
	{
		#region Private Fields

		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private DebounceDispatcher _viewPortSizeDispatcher;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Image _image;

		private SizeDbl _viewportSizeInternal;
		private SizeDbl _viewportSize;

		private SizeDbl _contentViewportSize;

		private ScaleTransform _scaleTransform;

		private ScaleTransform _canvasRenderTransform;

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

			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_image = new Image();
			_image.SizeChanged += Image_SizeChanged;

			_viewportSizeInternal = new SizeDbl();
			_viewportSize = new SizeDbl();
			_contentViewportSize = SizeDbl.NaN;

			_scaleTransform = new ScaleTransform();
			_scaleTransform.Changed += ScaleTransform_Changed;

			_canvasRenderTransform = new ScaleTransform();
		}

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLine($"The BitmapGridControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
			UpdateImageOffset(ImageOffset);
		}

		private void ScaleTransform_Changed(object? sender, EventArgs e)
		{
			//SetTheCanvasRenderTransform(ScaleTransform);

			if (sender is ScaleTransform st)
			{
				SetTheCanvasRenderTransform(st);
			}
			else
			{
				throw new InvalidOperationException("Expecting the sender of the ScaleTransform_Changed event to be able to be cast as a ScaleTransform.");
			}
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
					_image.RenderTransform = _canvasRenderTransform;

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
			get => _contentViewportSize.IsNAN() ? ViewportSizeInternal : _contentViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(_contentViewportSize, value))
				{
					var scaleFactor = ZoomSlider.GetScaleFactor(ScaleTransform.ScaleX);
					var newCanvasSize = value.Scale(scaleFactor);

					Debug.WriteLine($"The BitmapGridControl's ContentViewportSize is being set to {value}. Setting the Canvas Size to {newCanvasSize}.");

					_contentViewportSize = value;

					Canvas.Width = newCanvasSize.Width;
					Canvas.Height = newCanvasSize.Height;
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

		public ScaleTransform ScaleTransform
		{
			get => _scaleTransform;
			set
			{
				if (_scaleTransform != value)
				{
					Debug.WriteLine($"The BitmapGridControl's ScaleTransform is being set to {value.ScaleX}, {value.ScaleY}.");

					_scaleTransform.Changed -= ScaleTransform_Changed;
					_scaleTransform = value;
					_scaleTransform.Changed += ScaleTransform_Changed;

					SetTheCanvasRenderTransform(ScaleTransform);
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

			_ourContent.Measure(availableSize);

			UpdateViewportSize(availableSize);

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

			UpdateViewportSize(childSize);

			Debug.WriteLine($"Before _content.Arrange({finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;
					
			if (canvas.ActualWidth != ContentViewportSize.Width)
			{
				canvas.Width = ContentViewportSize.Width;
			}

			if (canvas.ActualHeight != ContentViewportSize.Height)
			{
				canvas.Height = ContentViewportSize.Height;
			}

			Debug.WriteLine($"After _content.Arrange(The canvas size is {new Size(canvas.Width, canvas.Height)} / {new Size(canvas.ActualWidth, canvas.ActualHeight)}.");

			return finalSize;
		}

		private void UpdateViewportSize(Size newValue)
		{
			var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(newValue);

			if (ViewportSizeInternal != newSizeDbl)
			{
				ViewportSizeInternal = newSizeDbl;
			}
		}

		private Size ForceSize(Size finalSize)
		{
			if (finalSize.Width > 1000 && finalSize.Width < 1040 && finalSize.Height > 1000 && finalSize.Height < 1040)
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
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();

				//Debug.WriteLine($"Found the BitmapGridControl3_Content template.");

				(Canvas, Image) = BuildContentModel(_ourContent);

				Debug.Assert(IsImageAChildOfCanvas(Image, Canvas), "Image is not a child of the Canvas.");
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
			var diff = newSize.Sub(originalSize);
			var half = diff.Scale(0.5);
			var result = originalOffset.Sub(half);

			return result;
		}

		private void SetTheCanvasRenderTransform(ScaleTransform st)
		{
			var (baseScale, relativeScale) = ZoomSlider.GetBaseAndRelative(st.ScaleX);

			Debug.WriteLine($"Setting the BitmapGridControl's Canvas RenderTransform to {relativeScale}. The BaseScale is {baseScale}.");

			_canvasRenderTransform.ScaleX = relativeScale;
			_canvasRenderTransform.ScaleY = relativeScale;
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

		protected override DependencyObject GetUIParentCore()
		{
			return base.GetUIParentCore();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property.Name == "RenderTransform" && e.NewValue is ScaleTransform st)
			{
				Debug.WriteLine($"The new RenderTransform ScaleX value is {st.ScaleX}.");
			}

			base.OnPropertyChanged(e);
		}

		#endregion

	}
}
