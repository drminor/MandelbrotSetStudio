using MSetExplorer.Cbs;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	public delegate void IsSelectedChangedCallback(int colorBandIndex, ColorBandSetEditMode editMode);
	internal delegate void ContextMenuDisplayRequest(CbListViewItem cbsListViewItem, ColorBandSetEditMode editMode);
	internal delegate void SectionLineMovedCallback(CbSectionLineMovedEventArgs cbSectionLineMovedEventArgs);

	public class HistogramColorBandControl : ContentControl, IContentScaler
	{
		#region Private Fields 

		// Our display starts at the the very top of the canvas.
		private const int CONTROL_ELEVATION = 0;

		private static readonly Brush TRANSPARENT_BRUSH = new SolidColorBrush(Colors.Transparent);
		private static readonly Brush RED_BRUSH = new SolidColorBrush(Colors.Red);

		private static readonly Brush DEFAULT_BACKGROUND = TRANSPARENT_BRUSH;
		private static readonly Brush DEFAULT_STROKE = RED_BRUSH;

		private readonly static bool CLIP_IMAGE_BLOCKS = false;

		private ICbsHistogramViewModel? _cbsHistogramViewModel;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Border _border;

		//private Rect _borderRect;
		//private readonly Shape _borderPath;

		private ListCollectionView _colorBandsView;
		private CbListView? _cbListView;
		private StoryboardDetails _storyBoardDetails1;

		private CbListViewAnimations? _cbListViewAnimations;

		private TranslateTransform _canvasTranslateTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		//private double _colorBlocksElevation;
		//private SizeDbl _padding;

		private RectangleDbl _translationAndClipSize;

		private SizeDbl _viewportSize;

		private bool _parentIsFocused;
		private ColorBandSetEditMode _currentCbEditMode;

		private ContextMenu? _lastKnownContextMenu;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		static HistogramColorBandControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramColorBandControl), new FrameworkPropertyMetadata(typeof(HistogramColorBandControl)));
		}

		public HistogramColorBandControl()
		{
			_cbsHistogramViewModel = null;
			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_border = new Border();

			_contentScale = new SizeDbl(1, 1);

			_colorBandsView = ColorBandSetViewHelper.GetEmptyListCollectionView();
			_cbListView = null;
			_cbListViewAnimations = null;
			_storyBoardDetails1 = new StoryboardDetails(new Storyboard(), _canvas);

			_canvasTranslateTransform = new TranslateTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			//_translationAndClipSize = new RectangleDbl();
			_translationAndClipSize = new RectangleDbl(0, 1, 0, 10);

			//_colorBlocksElevation = CONTROL_ELEVATION + DEFAULT_SECTION_LINES_HEIGHT;
			//_padding = new SizeDbl(2, 2);

			//_borderRect = CalculateBorderRect(TranslationAndClipSize, ContentScale, _colorBlocksElevation, _padding);
			//_borderPath = CreateBorder(_borderRect);

			_viewportSize = new SizeDbl();
			_parentIsFocused = false;
			_currentCbEditMode = ColorBandSetEditMode.Bands;

			MousePositionWhenContextMenuWasOpened = new Point(double.NaN, double.NaN);

			_lastKnownContextMenu = null;

			Loaded += HistogramColorBandControl_Loaded;
			Unloaded += HistogramColorBandControl_Unloaded;
		}

		private void HistogramColorBandControl_Loaded(object sender, RoutedEventArgs e)
		{
			//_canvas.SizeChanged += Handle_Canvas_SizeChanged;

			PreviewMouseDown += Handle_PreviewMouseDown;
			MouseEnter += Handle_MouseEnter;
			GotFocus += Handle_GotFocus;
			LostFocus += Handle_LostFocus;

			KeyDown += HandleKeyDown;
			PreviewKeyDown += Handle_PreviewKeyDown;

			//SizeChanged += HistogramColorBandControl_SizeChanged;
		}

		private void HistogramColorBandControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (_cbsHistogramViewModel != null)
			{
				_cbsHistogramViewModel = null;
			}

			//_canvas.SizeChanged -= Handle_Canvas_SizeChanged;
			PreviewMouseDown -= Handle_PreviewMouseDown;

			MouseEnter -= Handle_MouseEnter;
			GotFocus -= Handle_GotFocus;
			LostFocus -= Handle_LostFocus;

			KeyDown -= HandleKeyDown;
			PreviewKeyUp -= Handle_PreviewKeyDown;

			Loaded -= HistogramColorBandControl_Loaded;
			Unloaded -= HistogramColorBandControl_Unloaded;
		}

		#endregion

		private ICbsHistogramViewModel? CbsHistogramViewModel
		{
			get => _cbsHistogramViewModel;
			set
			{
				if (value != _cbsHistogramViewModel)
				{
					if (_cbsHistogramViewModel != null)
					{
						_cbsHistogramViewModel.PropertyChanged -= ViewModel_PropertyChanged;
					}

					_cbsHistogramViewModel = value;

					if (_cbsHistogramViewModel != null)
					{
						_cbsHistogramViewModel.PropertyChanged += ViewModel_PropertyChanged;
					}
				}
			}
		}

		#region Events

		public event EventHandler<(SizeDbl, SizeDbl)>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		public Canvas Canvas
		{
			get => _canvas;
			private set
			{
				//_canvas.SizeChanged -= Handle_Canvas_SizeChanged;
				_canvas = value;
				//_canvas.SizeChanged += Handle_Canvas_SizeChanged;

				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				_canvas.RenderTransform = _canvasRenderTransform;

				//_canvas.Background = new SolidColorBrush(Colors.MistyRose);
				_canvas.Background = new SolidColorBrush(Colors.Transparent);
				_storyBoardDetails1 = new StoryboardDetails(new System.Windows.Media.Animation.Storyboard(), _canvas);

				//_borderRect = CalculateBorderRect(TranslationAndClipSize, ContentScale, _colorBlocksElevation, _padding);
				//((RectangleGeometry)((Path)_borderPath).Data).Rect = _borderRect;

				//_canvas.Children.Add(_borderPath);
				//_borderPath.SetValue(Panel.ZIndexProperty, 20);

				//NameScope.SetNameScope(_canvas, new NameScope());
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

					if (_cbListView != null)
					{
						_cbListView.ContentScale = value;
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
				//DrawBorder(value);
			}
		}

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
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling ColorBandsView update. The Canvas Width is being updated from: {Canvas.Width} to {scaledExtent} " +
						$"and redrawing the ColorBands on ColorBandsView update.");

					Canvas.Width = scaledExtent;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling ColorBandsView update. The Canvas Width remains the same at {Canvas.Width}. The extent is {extent}.");
				}

				if (_cbListView != null)
				{
					_cbListView.TearDown();
					_cbListView = null;
				}

				if (_cbListViewAnimations != null)
				{
					_cbListViewAnimations = null;
				}

				if (_colorBandsView.Count > 0)
				{
					var currentCbEditMode = CbsHistogramViewModel?.CurrentCbEditMode ?? ColorBandSetEditMode.Bands;
					
					_cbListView = new CbListView(_canvas, _colorBandsView, CONTROL_ELEVATION, ActualHeight, ContentScale, _parentIsFocused, currentCbEditMode, ShowContextMenu, HandleCbListViewEditModeChanged, _storyBoardDetails1.OurNameScope);
					_cbListViewAnimations = new CbListViewAnimations(_storyBoardDetails1, _cbListView, OnAnimationComplete);
				}
			}
		}
	
		public Point MousePositionWhenContextMenuWasOpened { get; private set; }

		public bool ParentIsFocused
		{
			get => _parentIsFocused;

			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					if (_cbListView != null)
					{
						_cbListView.ParentIsFocused = value;
					}
				}
			}
		}

		public ColorBandSetEditMode CurrentCbEditMode
		{
			get => _currentCbEditMode;
			set
			{
				if (value != _currentCbEditMode)
				{
					Debug.WriteLine($"HistogramColorBandControl: The CurrentDbEditMode is being updated from: {_currentCbEditMode} to {value}.");

					_currentCbEditMode = value;

					if (_cbListView != null)
					{
						_cbListView.CurrentCbEditMode = value;
					}

					if (CbsHistogramViewModel != null)
					{
						CbsHistogramViewModel.CurrentCbEditMode = value;
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void InsertColorBandItem(int colorBandIndex, ColorBandSetEditMode editMode)
		{
			if (_cbsHistogramViewModel == null || _cbListViewAnimations == null)
			{
				return;
			}

			if (_cbsHistogramViewModel.TestInsertItem(colorBandIndex))
			{
				//Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl InsertColorBandItem - EditMode: {editMode}, ColorBandIndex: {colorBandIndex}.");
				Debug.WriteLine($"HistogramColorBandControl InsertColorBandItem - EditMode: {editMode}, ColorBandIndex: {colorBandIndex}.");

				switch (editMode)
				{
					case ColorBandSetEditMode.Cutoffs:
						_cbListViewAnimations.AnimateInsertCutoff(colorBandIndex);
						break;

					case ColorBandSetEditMode.Colors:
						_cbListViewAnimations.AnimateInsertColor(colorBandIndex);
						break;

					case ColorBandSetEditMode.Bands:
						_cbListViewAnimations.AnimateInsertBand(colorBandIndex);
						break;

					default:
						break;
				}
			}
			else
			{
				Debug.WriteLine($"The HistogramColorBandControl was unable to Delete the Item at ColorBandIndex: {colorBandIndex}.");
			}
		}

		// Delete
		public void DeleteColorBandItem(int colorBandIndex, ColorBandSetEditMode editMode)
		{
			if (_cbsHistogramViewModel == null || _cbListViewAnimations == null)
			{
				return;
			}

			if (_cbsHistogramViewModel.TestDeleteItem(colorBandIndex))
			{
				//Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl DeleteColorBandItem - EditMode: {editMode}, ColorBandIndex: {colorBandIndex}.");
				Debug.WriteLine($"HistogramColorBandControl DeleteColorBandItem - EditMode: {editMode}, ColorBandIndex: {colorBandIndex}.");

				switch (editMode)
				{
					case ColorBandSetEditMode.Cutoffs:
						// Delete the Item just after the selected SectionLine
						_cbListViewAnimations.AnimateDeleteCutoff(colorBandIndex + 1);
						break;

					case ColorBandSetEditMode.Colors:
						_cbListViewAnimations.AnimateDeleteColor(colorBandIndex);
						break;

					case ColorBandSetEditMode.Bands:
						_cbListViewAnimations.AnimateDeleteBand(colorBandIndex);
						break;

					default:
						break;
				}
			}
			else
			{
				Debug.WriteLine($"The HistogramColorBandControl was unable to Delete the Item at ColorBandIndex: {colorBandIndex}.");
			}
		}

		private void OnAnimationComplete(ColorBandSetEditOperation editOp, int index,ColorBand? newItem = null)
		{
			switch (editOp)
			{
				case ColorBandSetEditOperation.InsertCutoff:

					if (newItem == null || !(newItem is ColorBand colorBandForInsertCutoff))
					{
						throw new ArgumentException("The newItem object parameter must be convertable to an int when the operation is InsertCutoff.");
					}

					_cbsHistogramViewModel?.CompleteCutoffInsertion(index, colorBandForInsertCutoff);
					break;

				case ColorBandSetEditOperation.InsertColor:

					if (newItem == null || !(newItem is ColorBand colorBandForInsertColor))
					{
						throw new ArgumentException("The newItem object parameter must be convertable to a ColorBand when the operation is InsertColor.");
					}

					_cbsHistogramViewModel?.CompleteColorInsertion(index, colorBandForInsertColor);
					break;

				case ColorBandSetEditOperation.InsertBand:

					if (newItem == null || !(newItem is ColorBand colorBand))
					{
						throw new ArgumentException("The newItem object parameter must be convertable to a ColorBand when the operation is InsertBand.");
					}

					_cbsHistogramViewModel?.CompleteBandInsertion(index, colorBand);
					break;

				case ColorBandSetEditOperation.DeleteCutoff:
					_cbsHistogramViewModel?.CompleteCutoffRemoval(index);
					break;

				case ColorBandSetEditOperation.DeleteColor:
					_cbsHistogramViewModel?.CompleteColorRemoval(index);
					break;

				case ColorBandSetEditOperation.DeleteBand:
					_cbsHistogramViewModel?.CompleteBandRemoval(index);
					break;

				default:
					break;
			}
		}

		public ColorBand? GetItemUnderMouse(Point hitPoint)
		{
			if (double.IsNaN(hitPoint.X) || double.IsNaN(hitPoint.Y))
			{
				return null;
			}

			var lvItemAndSelType = _cbListView?.GetItemAtMousePosition(hitPoint) ?? null;

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

		public int? GetItemIndexUnderMouse(Point hitPoint)
		{
			if (double.IsNaN(hitPoint.X) || double.IsNaN(hitPoint.Y))
			{
				return null;
			}

			var lvItemAndSelType = _cbListView?.GetItemIndexAtMousePosition(hitPoint) ?? null;

			if (lvItemAndSelType != null)
			{
				var result = lvItemAndSelType.Value.Item1;
				return result;
			}
			else
			{
				return null;
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
				var (canvas, border) = BuildContentModel(_ourContent);

				Canvas = canvas;
				_border = border;

				//Debug.WriteLine($"Found Border: {border}");

				CbsHistogramViewModel = (ICbsHistogramViewModel)DataContext;
			}
			else
			{
				throw new InvalidOperationException("Did not find the HistogramColorBandControl_Content template.");
			}
		}

		//private Canvas BuildContentModelOld(FrameworkElement content)
		//{
		//	if (content is ContentPresenter cp)
		//	{
		//		if (cp.Content is Canvas ca)
		//		{
		//			return ca;
		//		}
		//	}

		//	throw new InvalidOperationException("Cannot find a child image element of the HistogramColorBandControl's Content, or the Content is not a Canvas element.");
		//}

		private (Canvas, Border) BuildContentModel(FrameworkElement content)
		{
			if (content is ContentPresenter cp)
			{
				if (cp.Content is Grid gr)
				{
					if (gr.Children[0] is Canvas ca && gr.Children[1] is Border br)
					{
						return (ca, br);
					}
				}
			}

			throw new InvalidOperationException("Cannot find a child image element of the HistogramColorBandControl's Content, or the Content is not a Canvas element.");
		}

		#endregion

		#region Event Handlers

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (_cbListView != null && CbsHistogramViewModel != null)
			{
				if (e.PropertyName == nameof(ICbsHistogramViewModel.CurrentCbEditMode))
				{
					_cbListView.CurrentCbEditMode = CbsHistogramViewModel.CurrentCbEditMode;
				}
			}
		}

		private void Handle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.FocusedElement != this)
			{
				var focusResult = Focus();
				ReportSetFocus(focusResult);
			}
		}

		private void Handle_MouseEnter(object sender, MouseEventArgs e)
		{
			//Focus();

			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl on Mouse Enter the Keyboard focus is now on {Keyboard.FocusedElement}.");
		}

		private void Handle_GotFocus(object sender, RoutedEventArgs e)
		{
			if (!ParentIsFocused)
			{
				Debug.WriteLine($"The HistogramColorBandControl is receiving focus. ParentIsFocused was false, setting it to true.");
				ParentIsFocused = true;
			}
		}

		private void Handle_LostFocus(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine($"The HistogramColorBandControl has lost focus. Setting ParentIsFocused = false, it was: {ParentIsFocused}.");
			ParentIsFocused = false;
		}

		#endregion

		#region Keyboard Event Handlers

		private void HandleKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && _cbListView != null)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling KeyDown. The Key is {e.Key}. The sender is {sender}.");
				if (_cbListView.CancelDrag())
				{
					e.Handled = true;
				}
			}
		}

		private void Handle_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (CbsHistogramViewModel == null || _cbListView == null || _cbListView.IsDragSectionLineInProgress)
			{
				return;
			}

			if (e.Key == Key.Left)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling PreviewKeyUp. The Key is {e.Key}. The sender is {sender}.");
				var wasMoved = CbsHistogramViewModel.TryMoveCurrentColorBandToPrevious();

				if (wasMoved)
				{
					_cbListView.SelectedIndexWasMoved(_cbListView.CurrentColorBandIndex, -1);
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Right)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling PreviewKeyUp. The Key is {e.Key}. The sender is {sender}.");
				var wasMoved = CbsHistogramViewModel.TryMoveCurrentColorBandToNext();

				if (wasMoved)
				{
					_cbListView.SelectedIndexWasMoved(_cbListView.CurrentColorBandIndex, 1);
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Up)
			{
				CbsHistogramViewModel.RetardEditMode();
				e.Handled = true;
			}
			else if (e.Key == Key.Down)
			{
				CbsHistogramViewModel.AdvanceEditMode();
				e.Handled = true;
			}
		}

		private void Handle_Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//_borderRect = CalculateBorderRect(TranslationAndClipSize, ContentScale, _colorBlocksElevation, _padding);
		}

		#endregion

		#region CbListView Callbacks

		private void ShowContextMenu(CbListViewItem sender, ColorBandSetEditMode editMode)
		{
			ContextMenu.IsOpen = true;
			//MessageBox.Show($"There will, one day, be a context menu here. Index: {sender.CbSectionLine.ColorBandIndex}; Source: {colorBandSelectionType}.");
		}

		private void HandleCbListViewEditModeChanged(ColorBandSetEditMode colorBandSetEditMode)
		{
			CurrentCbEditMode = colorBandSetEditMode;
		}

		#endregion

		#region Context Menu Event Handlers

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
			if (newValue.Height > 0 && newValue.Width > 0)
			{
				ReportTranslationTransformX(previousValue, newValue);
				_canvasTranslateTransform.X = newValue.Position.X * ContentScale.Width;

				//_borderRect = CalculateBorderRect(TranslationAndClipSize, ContentScale, _colorBlocksElevation, _padding);
				//((RectangleGeometry)((Path)_borderPath).Data).Rect = _borderRect;
			}
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

		#endregion

		#region Border

		//private Shape CreateBorder(Rect borderArea)
		//{
		//	var rg = new RectangleGeometry(borderArea);

		//	var result = new Path()
		//	{
		//		Fill = DEFAULT_BACKGROUND,
		//		Stroke = DEFAULT_STROKE,
		//		StrokeThickness = 1,
		//		Data = rg,
		//		IsHitTestVisible = false
		//	};

		//	return result;
		//}

		//private Rect CalculateBorderRect(RectangleDbl clip, SizeDbl contentScale, double colorBlockElevation, SizeDbl padding)
		//{
		//	if (clip.Height <= colorBlockElevation)
		//	{
		//		return ScreenTypeHelper.ConvertToRect(clip);
		//	}

		//	var xPosition = clip.Position.X;
		//	var yPosition = colorBlockElevation;

		//	var width = clip.Width;
		//	var height = clip.Height - colorBlockElevation;

		//	var area = new Rect(xPosition, yPosition, width, height);

		//	var result = GetScaledAndPaddedRect(area, contentScale, padding);

		//	return result;
		//}

		//private Rect GetScaledAndPaddedRect(Rect r, SizeDbl contentScale, SizeDbl padding)
		//{
		//	var nr = new Rect(r.Location, r.Size);
		//	nr.Scale(contentScale.Width, 1.0);

		//	Rect result;

		//	if (nr.Width > 4 && nr.Height > 4)
		//	{
		//		result = Rect.Inflate(nr, padding.Width * -1, padding.Height * -1);
		//	}
		//	else
		//	{
		//		result = nr;
		//	}

		//	return result;
		//}

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

		[Conditional("DEBUG2")]
		private void ReportSetFocus(bool focusResult)
		{
			var elementWithFocus = Keyboard.FocusedElement;

			if (elementWithFocus is DependencyObject dp)
			{
				var elementWithLogicalFocus = FocusManager.GetFocusedElement(dp);
				var focusScope = FocusManager.GetFocusScope(dp);
				Debug.WriteLine($"HistogramColorBandControl. HandlePreviewLeftButtonDown. The Keyboard focus is now on {elementWithFocus}. The focus is at {elementWithLogicalFocus}. FocusScope: {focusScope}. FocusResult: {focusResult}.");
			}
			else
			{
				Debug.WriteLine($"HistogramColorBandControl. HandlePreviewLeftButtonDown. The Keyboard focus is now on {elementWithFocus}. The element with logical focus cannot be determined. FocusResult: {focusResult}.");
			}
		}

		#endregion
	}

	public enum ColorBandSetEditOperation
	{
		InsertCutoff,
		DeleteCutoff,

		InsertColor,
		DeleteColor,

		InsertBand,
		DeleteBand
	}
}
