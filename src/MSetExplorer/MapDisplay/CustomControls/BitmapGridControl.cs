﻿using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGridControl: ContentControl, IContentScaler
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

		private ScaleTransform _controlScaleTransform;

		private TransformGroup _canvasRenderTransform;
		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;

		private VectorDbl _canvasOffset;
		private RectangleGeometry? _canvasClip;

		private bool _useDetailedDebug = false;

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

			_controlScaleTransform = new ScaleTransform();
			_controlScaleTransform.Changed += _controlScaleTransform_Changed; 

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_canvasOffset = new VectorDbl();
			_canvasClip = null;

			//MouseEnter += BitmapGridControl_MouseEnter;
			//MouseLeave += BitmapGridControl_MouseLeave;
		}

		//private void BitmapGridControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		//{
		//	//SetTheCanvasTranslateTransform(new VectorDbl(), _canvasOffset);
		//	//Clip = _clipT;

		//	if (_clipT != null)
		//	{
		//		_canvas.ClipToBounds = false;
		//		_canvas.Clip = _clipT;
		//	}
		//	else
		//	{
		//		_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
		//	}
		//}

		//private void BitmapGridControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		//{
		//	//SetTheCanvasTranslateTransform(_canvasOffset, new VectorDbl());
		//	//Clip = null;

		//	_canvas.ClipToBounds = false;
		//	_canvas.Clip = null;
		//}

		#endregion

		#region Event Handlers

		private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine($"The BitmapGridControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}, Setting the ImageOffset to {ImageOffset}.");
			Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's Image Size has changed. New size: {new SizeDbl(Image.ActualWidth, Image.ActualHeight)}.");

			UpdateImageOffset(ImageOffset);
		}

		private void _controlScaleTransform_Changed(object? sender, EventArgs e)
		{
			SetTheCanvasScaleTransform(_controlScaleTransform);
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
				_canvas.Clip = _canvasClip;
				_canvas.RenderTransform = _canvasRenderTransform;
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

					CheckThatImageIsAChildOfCanvas(Image, Canvas);
				}
			}
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
						Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize immediately. Previous Size: {previousValue}, New Size: {value}.");
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
								Debug.WriteLineIf(_useDetailedDebug, $"Updating the ViewportSize after debounce. Previous Size: {ViewportSize}, New Size: {newViewportSize}.");
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
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					Debug.Assert(_viewportSizeInternal.Diff(value).IsNearZero(), "The container size has been updated since the Debouncer fired.");

					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLine($"The BitmapGridControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
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

		public SizeDbl ContentViewportSize
		{
			get => _contentViewportSize.IsNAN() ? ViewportSizeInternal : _contentViewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(_contentViewportSize, value))
				{
					_contentViewportSize = value;
					SetTheCanvasSize(value, _controlScaleTransform);
				}
			}
		}

		ScaleTransform IContentScaler.ScaleTransform
		{
			get => _controlScaleTransform;
			set
			{
				if (_controlScaleTransform != value)
				{
					_controlScaleTransform.Changed -= _controlScaleTransform_Changed;
					_controlScaleTransform = value;
					_controlScaleTransform.Changed += _controlScaleTransform_Changed;

					SetTheCanvasScaleTransform(_controlScaleTransform);

					UpdateImageOffset(ImageOffset);
				}
			}
		}

		public VectorDbl CanvasOffset
		{
			get => _canvasOffset;
			set
			{
				var previousVal = _canvasOffset;
				_canvasOffset = value;
				SetTheCanvasTranslateTransform(previousVal, value);
			}
		}

		public RectangleGeometry? CanvasClip
		{
			get => _canvasClip;
			set
			{
				_canvasClip = value;
				_canvas.Clip = value;
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

			//UpdateViewportSize(availableSize);

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(availableSize);

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

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGripControl Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

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

			//UpdateViewportSize(childSize);

			ViewportSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			_ourContent.Arrange(new Rect(finalSize));

			var canvas = Canvas;

			if (canvas.ActualWidth != finalSize.Width)
			{
				canvas.Width = finalSize.Width;
			}

			if (canvas.ActualHeight != finalSize.Height)
			{
				canvas.Height = finalSize.Height;
			}

			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGridControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			return finalSize;
		}

		//private void UpdateViewportSize(Size newValue)
		//{
		//	var newSizeDbl = ScreenTypeHelper.ConvertToSizeDbl(newValue);

		//	if (ViewportSizeInternal != newSizeDbl)
		//	{
		//		ViewportSizeInternal = newSizeDbl;
		//	}
		//}

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
				(Canvas, Image) = BuildContentModel(_ourContent);
			}
			else
			{
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

		#endregion

		#region Private Methods - Canvas

		private void SetTheCanvasSize(SizeDbl contentViewportSize, ScaleTransform st)
		{
			var (baseScale, relativeScale) = ZoomSlider.GetBaseAndRelative(st.ScaleX);
			var scaleFactor = ZoomSlider.GetScaleFactorFromBase(baseScale);

			var viewportSize = new SizeDbl(ActualWidth, ActualHeight);

			CompareViewportAndContentViewportSizes(viewportSize, contentViewportSize, scaleFactor, relativeScale);

			//var newCanvasSize = contentViewportSize.Scale(scaleFactor);
			var newCanvasSize = viewportSize.Divide(relativeScale);

			Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's ContentViewportSize is being set to {contentViewportSize} from {_contentViewportSize}. Setting the Canvas Size to {newCanvasSize}.");

			Canvas.Width = newCanvasSize.Width;
			Canvas.Height = newCanvasSize.Height;
		}

		private void SetTheCanvasScaleTransform(ScaleTransform st)
		{
			var (baseScale, relativeScale) = ZoomSlider.GetBaseAndRelative(st.ScaleX);

			var combinedScale = new SizeDbl(st.ScaleX, st.ScaleY);

			var currentScaleX = _canvasScaleTransform.ScaleX;
			Debug.WriteLineIf(_useDetailedDebug, $"\n\nThe BitmapGridControl's Image ScaleTransform is being set to {relativeScale} from {currentScaleX}. CombinedScale: {combinedScale}, BaseScale is {baseScale}. The CanvasOffset is {_canvasOffset}.");

			_canvasScaleTransform.ScaleX = relativeScale;
			_canvasScaleTransform.ScaleY = relativeScale;
		}

		private void SetTheCanvasTranslateTransform(VectorDbl previousValue, VectorDbl canvasOffset)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's CanvasOffset is being set to {canvasOffset} from {previousValue}. The ImageOffset is {ImageOffset}.");

			_canvasTranslateTransform.X = canvasOffset.X;
			_canvasTranslateTransform.Y = canvasOffset.Y;
		}

		private bool UpdateImageOffset(VectorDbl rawValue)
		{
			//var newValue = rawValue.Scale(_scaleTransform.ScaleX);
			//Debug.WriteLine($"Updating ImageOffset: raw: {rawValue}, scaled: {newValue}. CanvasOffset: {_canvasOffset}. ImageScaleTransform: {_scaleTransform.ScaleX}.");

			var newValue = rawValue;


			// For a positive offset, we "pull" the image down and to the left.
			var invertedValue = newValue.Invert();

			VectorDbl currentValue = new VectorDbl(
				(double)Image.GetValue(Canvas.LeftProperty),
				(double)Image.GetValue(Canvas.BottomProperty)
				);

			if (currentValue.IsNAN() || ScreenTypeHelper.IsVectorDblChanged(currentValue, invertedValue, threshold: 0.1))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGridControl's ImageOffset is being set to {newValue} from {currentValue}. CanvasOffset: {_canvasOffset}. ImageScaleTransform: {_controlScaleTransform.ScaleX}.");

				CompareCanvasAndControlHeights();

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

		[Conditional("DEBUG")]
		private void CompareViewportAndContentViewportSizes(SizeDbl viewportSize, SizeDbl contentViewportSize, double scaleFactor, double relativeScale)
		{
			// The contentViewportSize when reduced by the BaseScale Factor
			// should equal the ViewportSize when it is expanded by the RelativeScale

			var contentViewportSizeReduced = contentViewportSize.Scale(scaleFactor);
			var viewportSizeExpanded = viewportSize.Scale(1 / relativeScale);

			if (ScreenTypeHelper.IsSizeDblChanged(contentViewportSizeReduced, viewportSizeExpanded, threshold: 1.1))
			{
				Debug.WriteLine("WARNING: The ContentViewport and Viewport Sizes when scaled appropriately are not equal.");
			}
		}

		[Conditional("DEBUG")]
		private void CompareCanvasAndControlHeights()
		{
			// The contentViewportSize when reduced by the BaseScale Factor
			// should equal the ViewportSize when it is expanded by the RelativeScale

			//var (baseScale, relativeScale) = ZoomSlider.GetBaseAndRelative(_controlScaleTransform.ScaleX);

			//var canvasHeightScaled = Canvas.ActualHeight * relativeScale;

			if (Math.Abs(Canvas.ActualHeight - ActualHeight) > 0.1)
			{
				Debug.WriteLine($"WARNING: The Canvas Height : {Canvas.ActualHeight} does not match the BitmapGridControl's height: {ActualHeight}.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckThatImageIsAChildOfCanvas(Image image, Canvas canvas)
		{
			foreach (var v in canvas.Children)
			{
				if (v == image)
				{
					return;
				}
			}

			throw new InvalidOperationException("The image is not a child of the canvas.");
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

		#endregion
	}
}