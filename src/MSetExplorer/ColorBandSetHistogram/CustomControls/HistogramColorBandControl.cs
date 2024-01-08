using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal delegate void IsSelectedChanged(int colorBandIndex, bool newValue, bool shiftKeyPressed, bool controlKeyPressed);
	internal delegate void ContextMenuDisplayRequest(CbsListViewItem cbsListViewItem, ColorBandSelectionType sourceType);

	public class HistogramColorBandControl : ContentControl, IContentScaler
	{
		#region Private Fields 

		private readonly static bool CLIP_IMAGE_BLOCKS = false;

		private ICbsHistogramViewModel? _vm;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Path? _border;

		private ListCollectionView _colorBandsView;
		private CbsListView? _cbsListView;

		private TranslateTransform _canvasTranslateTransform;
		//private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;

		private SizeDbl _viewportSize;
		private bool _useRealTimePreview;

		private bool _parentIsFocused;

		private ContextMenu? _lastKnownContextMenu;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static HistogramColorBandControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramColorBandControl), new FrameworkPropertyMetadata(typeof(HistogramColorBandControl)));
		}

		public HistogramColorBandControl()
		{
			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_border = null;

			_contentScale = new SizeDbl(42, 42);

			//var isHorizontalScrollBarVisible = true;

			_colorBandsView = GetEmptyListCollectionView(); 
			_cbsListView = null;

			_canvasTranslateTransform = new TranslateTransform();
			//_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			//_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_translationAndClipSize = new RectangleDbl();

			_viewportSize = new SizeDbl();
			_parentIsFocused = false;

			_border = null;

			KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Continue);
			Focusable = true;
			IsTabStop = false;
			IsEnabled = true;

			MousePositionWhenContextMenuWasOpened = new Point(double.NaN, double.NaN);

			_lastKnownContextMenu = null;

			Loaded += HistogramColorBandControl_Loaded;
			Unloaded += HistogramColorBandControl_Unloaded;
		}

		private void HistogramColorBandControl_Loaded(object sender, RoutedEventArgs e)
		{

			MouseEnter += Handle_MouseEnter;
			//MouseLeave += Handle_MouseLeave;


			GotFocus += HistogramColorBandControl_GotFocus;
			LostFocus += HistogramColorBandControl_LostFocus;

			KeyDown += HandleKeyDown;
			PreviewKeyUp += Handle_PreviewKeyUp;
		}

		private void HistogramColorBandControl_Unloaded(object sender, RoutedEventArgs e)
		{
			MouseEnter -= Handle_MouseEnter;
			//MouseLeave -= Handle_MouseLeave;

			GotFocus -= HistogramColorBandControl_GotFocus;
			LostFocus -= HistogramColorBandControl_LostFocus;
			
			KeyDown -= HandleKeyDown;
			PreviewKeyUp -= Handle_PreviewKeyUp;

			Loaded -= HistogramColorBandControl_Loaded;
			Unloaded -= HistogramColorBandControl_Unloaded;
		}

		#endregion

		#region Events

		public event EventHandler<(SizeDbl, SizeDbl)>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		public Point MousePositionWhenContextMenuWasOpened { get; private set; }

		public ListCollectionView ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				_colorBandsView = value;

				var extent = GetExtent(ColorBandsView);
				var scaledExtent = extent * ContentScale.Width;

				if (ScreenTypeHelper.IsDoubleChanged(scaledExtent, Canvas.Width))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling ColorBandsView update. The Canvas Width is being updated from: {Canvas.Width} to {scaledExtent} and redrawing the ColorBands on ColorBandsView update.");
					Canvas.Width = scaledExtent;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling ColorBandsView update. The Canvas Width remains the same at {Canvas.Width}. The extent is {extent}.");
				}

				if (_cbsListView != null)
				{
					_cbsListView.TearDown();
					_cbsListView = null;
				}

				if (_colorBandsView.Count > 0)
				{
					_cbsListView = new CbsListView(_canvas, _colorBandsView, ActualHeight, ContentScale, UseRealTimePreview, _parentIsFocused, ShowContextMenu);
				}
			}
		}

		public bool UseRealTimePreview
		{
			get => _useRealTimePreview;
			set
			{
				if (value != _useRealTimePreview)
				{
					_useRealTimePreview = value;

					if (_cbsListView != null)
					{
						_cbsListView.UseRealTimePreview = value;
					}
				}
			}
		}

		public Canvas Canvas
		{
			get => _canvas;
			set
			{
				_canvas.SizeChanged -= Handle_SizeChanged;
				_canvas = value;
				_canvas.SizeChanged += Handle_SizeChanged;

				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.RenderTransform = _canvasRenderTransform;
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (ScreenTypeHelper.IsSizeDblChanged(ViewportSize, value))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is having its ViewportSize updated to {value}, the current value is {_viewportSize}; will raise the ViewportSizeChanged event.");

					var previousValue = ViewportSize;
					_viewportSize = value;

					// Recalculate the minimum display zoom and if the current zoom is less, update the display zoom.
					var extent = GetExtent(ColorBandsView);
					var minContentScale = extent == 0 ? 0.1 : _viewportSize.Width / extent;

					if (_contentScale.Width < minContentScale)
					{
						_contentScale = new SizeDbl(minContentScale, _contentScale.Height);
					}

					// TODO: Update the IContentScaler interface to support updating the ContentScale and MinimumContentScale as well as the ViewportSize.
					ViewportSizeChanged?.Invoke(this, (previousValue, value));
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is having its ViewportSize updated to {value}, the current value is already: {_viewportSize}; not raising the ViewportSizeChanged event.");
				}
			}
		}

		public SizeDbl ContentScale
		{
			get => _contentScale;
			set
			{
				if (value != _contentScale)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl. The ContentScale is being updated from: {_contentScale} to {value}.");

					_contentScale = value;

					var extent = GetExtent(ColorBandsView);
					var scaledExtent = extent * ContentScale.Width;
					Canvas.Width = scaledExtent;

					if (_cbsListView != null)
					{
						_cbsListView.ContentScale = value;
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl. The ContentScale is being set to the value it already has: {value}.");
				}
			}
		}

		public RectangleDbl TranslationAndClipSize
		{
			get => _translationAndClipSize; 
			set
			{
				var previousVal = _translationAndClipSize;
				_translationAndClipSize = value;

				ClipAndOffset(previousVal, value);
				DrawBorder(value);
			}
		}

		//public bool IsHorizontalScrollBarVisible
		//{
		//	get => _colorBandLayoutViewModel.IsHorizontalScrollBarVisible;
		//	set
		//	{
		//		_colorBandLayoutViewModel.IsHorizontalScrollBarVisible = value;
		//	}
		//}

		public bool ParentIsFocused
		{
			get => _parentIsFocused;

			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					if (_cbsListView != null)
					{
						_cbsListView.ParentIsFocused = value;
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public int? GetIndexOfItemUnderMouse(Point hitPoint)
		{
			if (double.IsNaN(hitPoint.X) || double.IsNaN(hitPoint.Y))
			{
				return null;
			}

			var x = _cbsListView?.ItemAtMousePosition(hitPoint) ?? null;

			if (x != null)
			{
				return x.Value.Item1.CbsRectangle.ColorBandIndex;
			}
			else
			{
				return null;
			}
		}

		public ColorBand? GetItemUnderMouse(Point hitPoint)
		{
			if (double.IsNaN(hitPoint.X) || double.IsNaN(hitPoint.Y))
			{
				return null;
			}

			var lvItemAndSelType = _cbsListView?.ItemAtMousePosition(hitPoint) ?? null;

			if (lvItemAndSelType != null)
			{
				var cbsListViewItem = lvItemAndSelType.Value.Item1;
				return cbsListViewItem.ColorBand;
			}
			else
			{
				return null;
			}
		}

		//public void ShowSelectionLines(bool leftMouseButtonIsPressed)
		//{
		//	_cbsListView?.ShowSelectionLines(leftMouseButtonIsPressed);
		//	_mouseIsEntered = true;
		//}

		//public void HideSelectionLines(bool leftMouseButtonIsPressed)
		//{
		//	_cbsListView?.HideSelectionLines(leftMouseButtonIsPressed);
		//	_mouseIsEntered = false;
		//}

		#endregion

		#region Private Methods - Control

		/// <summary>
		/// Measure the control and it's children.
		/// </summary>
		protected override Size MeasureOverride(Size availableSize)
		{
			Size childSize = base.MeasureOverride(availableSize);

			_ourContent.Measure(availableSize);

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

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl Measure. Available: {availableSize}. Base returns {childSize}, using {result}.");

			return result;
		}

		/// <summary>
		/// Arrange the control and it's children.
		/// </summary>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Size childSize = base.ArrangeOverride(finalSize);

			if (childSize != finalSize) Debug.WriteLine($"WARNING: The result from ArrangeOverride does not match the input to ArrangeOverride. {childSize}, vs. {finalSize}.");

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl - Before Arrange{finalSize}. Base returns {childSize}. The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

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

			ViewportSize = ScreenTypeHelper.ConvertToSizeDbl(childSize);

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl - After Arrange: The canvas size is {new Size(Canvas.Width, Canvas.Height)} / {new Size(Canvas.ActualWidth, Canvas.ActualHeight)}.");

			return finalSize;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Content = Template.FindName("PART_Content", this) as FrameworkElement;

			if (Content != null)
			{
				_ourContent = (Content as FrameworkElement) ?? new FrameworkElement();
				//(Canvas, Image) = BuildContentModel(_ourContent);
				Canvas = BuildContentModel(_ourContent);

				_vm = (ICbsHistogramViewModel)DataContext;
			}
			else
			{
				throw new InvalidOperationException("Did not find the HistogramColorBandControl_Content template.");
			}
		}

		private Canvas BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Canvas ca)
				{
					return ca;
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the HistogramColorBandControl's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region Event Handlers

		private void Handle_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling the SizeChanged event.");

			//_colorBandLayoutViewModel.ControlHeight = e.NewSize.Height;

			if (_cbsListView != null)
			{
				_cbsListView.ControlHeight = e.NewSize.Height;
			}
		}

		//private void Handle_MouseLeave(object sender, MouseEventArgs e)
		//{
		//	//HideSelectionLines(e.LeftButton == MouseButtonState.Pressed);

		//	if (e.LeftButton != MouseButtonState.Pressed)
		//	{
		//		if (_cbsListView != null)
		//		{
		//			_cbsListView.CancelDrag();
		//		}
		//	}
		//}

		private void Handle_MouseEnter(object sender, MouseEventArgs e)
		{
			Focus();

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl on Mouse Enter the Keyboard focus is now on {Keyboard.FocusedElement}.");
			//ShowSelectionLines(e.LeftButton == MouseButtonState.Pressed);
		}

		#endregion

		#region Keyboard Event Handlers

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling KeyDown. The Key is {e.Key}. The sender is {sender}.");

			if (e.Key == Key.Escape && _cbsListView != null)
			{
				if (_cbsListView.CancelDrag())
				{
					e.Handled = true;
				}
			}
		}

		private void Handle_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling PreviewKeyUp. The Key is {e.Key}. The sender is {sender}.");

			if (_cbsListView?.IsDragSelectionLineInProgress != true)
			{
				if (e.Key == Key.Left)
				{
					_vm?.TryMoveCurrentColorBandToPrevious();
					e.Handled = true;
				}
				else if (e.Key == Key.Right)
				{
					_vm?.TryMoveCurrentColorBandToNext();
					e.Handled = true;
				}
			}
		}

		#endregion

		#region Context Menu Event Handlers

		private void ShowContextMenu(CbsListViewItem sender, ColorBandSelectionType colorBandSelectionType)
		{
			ContextMenu.IsOpen = true;
			//MessageBox.Show($"There will, one day, be a context menu here. Index: {sender.CbsSelectionLine.ColorBandIndex}; Source: {colorBandSelectionType}.");
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == ContextMenuProperty)
			{
				if (_lastKnownContextMenu != null)
				{
					_lastKnownContextMenu.Opened -= ContextMenu_Opened;
					_lastKnownContextMenu.Closed -= ContextMenu_Closed;
				}

				_lastKnownContextMenu = (ContextMenu?)this.GetValue(ContextMenuProperty);

				if (_lastKnownContextMenu != null)
				{
					_lastKnownContextMenu.Opened += ContextMenu_Opened;
					_lastKnownContextMenu.Closed += ContextMenu_Closed;
				}
			}

		}

		private void ContextMenu_Closed(object sender, RoutedEventArgs e)
		{
			MousePositionWhenContextMenuWasOpened = new Point(double.NaN, double.NaN);
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			MousePositionWhenContextMenuWasOpened = Mouse.GetPosition(_canvas);
		}

		#endregion

		#region Private Methods

		private void ClipAndOffset(RectangleDbl previousValue, RectangleDbl newValue)
		{
			ReportTranslationTransformX(previousValue, newValue);
			_canvasTranslateTransform.X = newValue.Position.X * ContentScale.Width;
		}

		private void DrawBorder(RectangleDbl newValue)
		{
			if (_border != null)
			{
				_canvas.Children.Remove(_border);
				_border = null;
			}

			if (_cbsListView == null)
			{
				return;
			}

			var cbrElevation = _cbsListView.CbrElevation;
			var xPosition = newValue.Position.X * ContentScale.Width;
			var area = new RectangleDbl(new PointDbl(xPosition, cbrElevation), new SizeDbl(ActualWidth, ActualHeight - cbrElevation));

			var cbRectangle = new RectangleGeometry(ScreenTypeHelper.ConvertToRect(area));

			var result = new Path()
			{
				Fill = Brushes.Transparent,
				Stroke = Brushes.DarkGray,
				StrokeThickness = 2,
				Data = cbRectangle,
				//Focusable = false,
				IsHitTestVisible = false
			};

			//_canvas.Children.Add(result);
			_border = result;
		}

		private int GetExtent(ListCollectionView? listCollectionView)
		{
			if (listCollectionView == null || listCollectionView.Count == 0)
			{
				return 0;
			}

			var result = (listCollectionView.GetItemAt(listCollectionView.Count - 1) as ColorBand)?.Cutoff ?? 0;

			return result;
		}
		

		private ListCollectionView GetEmptyListCollectionView()
		{
			var newCollection = new ObservableCollection<ColorBand>();
			var result = (ListCollectionView)CollectionViewSource.GetDefaultView(newCollection);

			return result;
		}


		#endregion

		#region Dependency Property Declarations

		//public static readonly DependencyProperty CurrentColorBandProperty =
		//DependencyProperty.Register("CurrentColorBand", typeof(ColorBand), typeof(HistogramColorBandControl),
		//							new FrameworkPropertyMetadata(ColorBand.NewEmpty, CurrentColorBandProperty_Changed));

		//private static void CurrentColorBandProperty_Changed(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//	//HistogramColorBandControl c = (HistogramColorBandControl)o;

		//	//var oldColorBand = (ColorBand?)e.OldValue;


		//	//Debug.WriteLineIf(c._useDetailedDebug, $"HistogramColorBandControl. The CurrentColorBandProperty is changing. Old: {oldColorBand}, New: {(ColorBand)e.NewValue}.");


		//	//if (oldColorBand != null)
		//	//{
		//	//	oldColorBand.PropertyChanged -= c.ColorBand_PropertyChanged;
		//	//	c.HilightColorBandRectangle(oldColorBand, on: false);
		//	//}

		//	//var newColorBand = (ColorBand)e.NewValue;

		//	//if (newColorBand != null)
		//	//{
		//	//	newColorBand.PropertyChanged += c.ColorBand_PropertyChanged;
		//	//	newColorBand.EditEnded += c.ColorBand_EditEnded;
		//	//	c.HilightColorBandRectangle(newColorBand, on: true);
		//	//}
		//}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportTranslationTransformX(RectangleDbl previousValue, RectangleDbl newValue)
		{
			var previousXValue = previousValue.Position.X * ContentScale.Width;
			var newXValue = newValue.Position.X * ContentScale.Width;
			Debug.WriteLine(_useDetailedDebug, $"The HistogramColorBandControl's CanvasTranslationTransform is being set from {previousXValue} to {newXValue}.");
		}

		private void HistogramColorBandControl_LostFocus(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine($"The HistogramColorBandControl has lost focus.");
		}

		private void HistogramColorBandControl_GotFocus(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine($"The HistogramColorBandControl is receiving focus.");
		}

		#endregion
	}
}
