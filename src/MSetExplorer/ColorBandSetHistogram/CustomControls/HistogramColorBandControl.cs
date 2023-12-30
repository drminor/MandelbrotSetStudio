using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSetExplorer
{
	public delegate void IsSelectedChanged(int colorBandIndex, bool newValue, bool shiftKeyPressed, bool controlKeyPressed);

	public class HistogramColorBandControl : ContentControl, IContentScaler
	{
		#region Private Fields 

		private readonly static bool CLIP_IMAGE_BLOCKS = false;
		//private const int SELECTION_LINE_UPDATE_THROTTLE_INTERVAL = 200;

		//private ColorBandLayoutViewModel _colorBandLayoutViewModel;

		//private DebounceDispatcher _selectionLineMovedDispatcher;

		private ICbsHistogramViewModel? _vm;

		private FrameworkElement _ourContent;
		private Canvas _canvas;
		private Path? _border;

		private ListCollectionView? _colorBandsView;
		private CbsListView? _cbsListView;

		//private readonly IList<CbsRectangle> _colorBandRectangles;

		//private readonly IList<CbsSelectionLine> _selectionLines;

		private TranslateTransform _canvasTranslateTransform;
		private ScaleTransform _canvasScaleTransform;
		private TransformGroup _canvasRenderTransform;

		private SizeDbl _contentScale;
		private RectangleDbl _translationAndClipSize;

		private SizeDbl _viewportSize;

		private bool _mouseIsEntered;
		//private List<Shape> _hitList;

		//private CbsSelectionLine? _selectionLineBeingDragged;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		static HistogramColorBandControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(HistogramColorBandControl), new FrameworkPropertyMetadata(typeof(HistogramColorBandControl)));
		}

		public HistogramColorBandControl()
		{
			//_selectionLineMovedDispatcher = new DebounceDispatcher
			//{
			//	Priority = DispatcherPriority.Render
			//};

			//_selectionLineBeingDragged = null;

			_ourContent = new FrameworkElement();
			_canvas = new Canvas();
			_border = null;

			_contentScale = new SizeDbl(1);

			//var isHorizontalScrollBarVisible = true;

			//_colorBandsView = null; 
			_cbsListView = null;

			//_colorBandLayoutViewModel = new ColorBandLayoutViewModel(_contentScale, ActualHeight);
			//_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;

			//_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;

			//_colorBandRectangles = new List<CbsRectangle>();

			//_selectionLines = new List<CbsSelectionLine>();

			_canvasTranslateTransform = new TranslateTransform();
			_canvasScaleTransform = new ScaleTransform();

			_canvasRenderTransform = new TransformGroup();
			_canvasRenderTransform.Children.Add(_canvasTranslateTransform);
			_canvasRenderTransform.Children.Add(_canvasScaleTransform);

			_canvas.RenderTransform = _canvasRenderTransform;

			_translationAndClipSize = new RectangleDbl();

			_viewportSize = new SizeDbl();
			_mouseIsEntered = false;
			//_hitList = new List<Shape>();

			_border = null;

			KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
			Focusable = true;
			IsTabStop = true;
			IsEnabled = true;

			//PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;

			MouseEnter += Handle_MouseEnter;
			MouseLeave += Handle_MouseLeave;

			GotFocus += HistogramColorBandControl_GotFocus;

			LostFocus += HistogramColorBandControl_LostFocus;

			KeyDown += HandleKeyDown;
			//KeyUp += HistogramColorBandControl_KeyUp;

			PreviewKeyDown += HandlePreviewKeyDown;
			PreviewKeyUp += Handle_PreviewKeyUp;
		}

		#endregion

		#region Events

		public event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;

		#endregion

		#region Public Properties

		public ListCollectionView? ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				//if (_colorBandsView != null)
				//{
				//	(_colorBandsView as INotifyCollectionChanged).CollectionChanged -= ColorBands_CollectionChanged;
				//}

				_colorBandsView = value;

				//if (_colorBandsView != null)
				//{
				//	(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBands_CollectionChanged;
				//}

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
				}

				if (_colorBandsView != null)
				{
					_cbsListView = new CbsListView(_canvas, _colorBandsView, ActualHeight, ContentScale, UseRealTimePreview, _mouseIsEntered);
				}

				//RemoveSelectionLines();
				//DrawColorBands(_colorBandsView);

				//if (_mouseIsEntered)
				//{
				//	Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on ColorBandsView update. (Have Mouse)");

				//	DrawSelectionLines(_colorBandRectangles);
				//}
			}
		}

		//public ColorBand CurrentColorBand
		//{
		//	get => (ColorBand)GetValue(CurrentColorBandProperty);
		//	set => SetCurrentValue(CurrentColorBandProperty, value);
		//}

		private bool _useRealTimePreview;

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
					var minContentScale = _viewportSize.Width / extent;

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
					_contentScale = value;

					var extent = GetExtent(ColorBandsView);
					var scaledExtent = extent * ContentScale.Width;
					Canvas.Width = scaledExtent;

					if (_cbsListView != null)
					{
						_cbsListView.ContentScale = value;
					}

					//_colorBandLayoutViewModel.ContentScale = new SizeDbl(_contentScale.Width, 1);

					//Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawColorBands on ContentScale update. The Extent is {extent}.");
					//RemoveSelectionLines();
					//DrawColorBands(ColorBandsView);

					//if (_mouseIsEntered)
					//{
					//	Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on ContentScale update. (Have Mouse)");
					//	DrawSelectionLines(_colorBandRectangles);
					//}
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

		//private double CbrElevation
		//{
		//	get => _cbrElevation;
		//	set
		//	{
		//		_cbrElevation = value;
		//		_colorBandLayoutViewModel.CbrElevation = value;
		//	}
		//}

		//private double CbrHeight
		//{
		//	get => _cbrHeight;

		//	set
		//	{
		//		if (value != _cbrHeight)
		//		{
		//			_cbrHeight = value;

		//			_colorBandLayoutViewModel.CbrHeight = value;

		//			RemoveSelectionLines();
		//			DrawColorBands(ColorBandsView);

		//			if (_mouseIsEntered)
		//			{
		//				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on CbrHeight update. (Have Mouse)");
		//				DrawSelectionLines(_colorBandRectangles);
		//			}
		//		}
		//	}
		//}

		#endregion

		#region Public Methods

		public void ShowSelectionLines(bool leftMouseButtonIsPressed)
		{
			_cbsListView?.ShowSelectionLines(leftMouseButtonIsPressed);
		}

		public void HideSelectionLines(bool leftMouseButtonIsPressed)
		{
			_cbsListView?.HideSelectionLines(leftMouseButtonIsPressed);
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

		//private void ColorBandLayoutViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(ColorBandLayoutViewModel.CbrHeight))
		//	{
		//		RemoveSelectionLines();
		//		DrawColorBands(ColorBandsView);

		//		if (_mouseIsEntered)
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on CbrHeight update. (Have Mouse)");
		//			DrawSelectionLines(_colorBandRectangles);
		//		}
		//	}
		//}

		private void Handle_MouseLeave(object sender, MouseEventArgs e)
		{
			_mouseIsEntered = false;

			if (_cbsListView != null)
			{
				_cbsListView.MouseIsEntered = false;
			}
		}

		private void Handle_MouseEnter(object sender, MouseEventArgs e)
		{
			Focus();
			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl on Mouse Enter the Keyboard focus is now on {Keyboard.FocusedElement}.");

			_mouseIsEntered = true;

			if (_cbsListView != null)
			{
				_cbsListView.MouseIsEntered = true;
			}
		}

		//private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//	var cbsView = ColorBandsView;

		//	if (cbsView == null)
		//		return;

		//	var hitPoint = e.GetPosition(Canvas);
		//	if (TryGetSelectionLine(hitPoint, _selectionLines, out var cbSelectionLine, out var cbSelectionLineIndex))
		//	{
		//		var cb = GetColorBandAt(cbsView, cbSelectionLineIndex.Value);
		//		cbsView.MoveCurrentTo(cb);

		//		//Debug.WriteLineIf(_useDetailedDebug, $"Starting Drag. ColorBandIndex = {cbSelectionLineIndex}. ContentScale: {ContentScale}. PosX: {hitPoint.X}. Original X: {cbSelectionLine.SelectionLinePosition}.");
		//		//ReportColorBandRectanglesInPlay(cbSelectionLineIndex.Value);

		//		_selectionLineBeingDragged = cbSelectionLine;
		//		_selectionLineBeingDragged.SelectionLineMoved += HandleSelectionLineMoved;

		//		Debug.WriteIf(_useDetailedDebug, $"HistogramColorBandControl. Starting Drag for cbSelectionLine at index: {cbSelectionLineIndex}, Current View Position: {cbsView.CurrentPosition}, ColorBand: {cb}.");

		//		cbSelectionLine.StartDrag();
		//	}
		//	else
		//	{
		//		if (TryGetColorBandRectangle(hitPoint, _colorBandRectangles, out var cbsRectangle, out var cbRectangleIndex))
		//		{
		//			var cb = GetColorBandAt(cbsView, cbRectangleIndex.Value);
		//			cbsView.MoveCurrentTo(cb);
		//		}
		//	}

		//	var focusResult = Focus();
		//	ReportSetFocus(focusResult);
		//}

		//private void HandleSelectionLineMoved(object? sender, CbsSelectionLineMovedEventArgs e)
		//{
		//	if (_selectionLineBeingDragged == null)
		//	{
		//		Debug.WriteLine("WARNING: _selectionLineBeingDragged is null on HandleSelectionLineMoved.");
		//		return;
		//	}

		//	if (!(sender is CbsSelectionLine selectionLine))
		//	{
		//		throw new InvalidOperationException("The HandleSelectionLineMoved event is being raised by some class other than CbsSelectionLine.");
		//	}
		//	else
		//	{
		//		if (sender != _selectionLineBeingDragged)
		//		{
		//			Debug.WriteLine("WARNING: HandleSelectionLineMoved is being raised by a SelectionLine other than the one that is being dragged.");
		//		}
		//	}

		//	if (e.NewXPosition == 0)
		//	{
		//		Debug.WriteLine($"WARNING: Setting the Cutoff to zero for ColorBandIndex: {e.ColorBandIndex}.");
		//	}

		//	switch (e.Operation)
		//	{
		//		case CbsSelectionLineDragOperation.Move:
		//			UpdateCutoffThrottled(e);
		//			break;

		//		case CbsSelectionLineDragOperation.Complete:
		//			_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
		//			_selectionLineBeingDragged = null;

		//			Debug.WriteLineIf(_useDetailedDebug, "Completing the SelectionBand Drag Operation.");
		//			UpdateCutoffThrottled(e);
		//			break;

		//		case CbsSelectionLineDragOperation.Cancel:
		//			_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
		//			_selectionLineBeingDragged = null;

		//			UpdateCutoffThrottled(e);
		//			break;

		//		default:
		//			throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
		//	}
		//}

		//private void UpdateCutoffThrottled(CbsSelectionLineMovedEventArgs e)
		//{
		//	if (UseRealTimePreview)
		//	{
		//		_selectionLineMovedDispatcher.Throttle(
		//			interval: SELECTION_LINE_UPDATE_THROTTLE_INTERVAL,
		//			action: parm => 
		//			{
		//				UpdateCutoff(e);
		//			},
		//			param: null);
		//	}
		//	else
		//	{
		//		//currentColorBand.Cutoff = newCutoff;
		//		UpdateCutoff(e);
		//	}
		//}

		//private void UpdateCutoff(CbsSelectionLineMovedEventArgs e)
		//{
		//	var cbView = ColorBandsView;

		//	if (cbView == null)
		//		return;

		//	var newCutoff = (int)Math.Round(e.NewXPosition / ContentScale.Width);

		//	var colorBandToUpdate = GetColorBandAt(cbView, e.ColorBandIndex);

		//	if (e.Operation == CbsSelectionLineDragOperation.Move)
		//	{
		//		if (!colorBandToUpdate.IsInEditMode)
		//		{
		//			colorBandToUpdate.BeginEdit();
		//		}

		//		Debug.WriteIf(_useDetailedDebug, $"HistogramColorBandControl. Updating ColorBand for Move Operation at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
		//		colorBandToUpdate.Cutoff = newCutoff;
		//	}
		//	else if (e.Operation == CbsSelectionLineDragOperation.Complete)
		//	{
		//		Debug.WriteIf(_useDetailedDebug, $"HistogramColorBandControl. Updating ColorBand for Operation=Complete at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
		//		colorBandToUpdate.Cutoff = newCutoff;
		//		colorBandToUpdate.EndEdit();
		//	}
		//	else if (e.Operation == CbsSelectionLineDragOperation.Cancel)
		//	{
		//		Debug.WriteIf(_useDetailedDebug, $"HistogramColorBandControl. Updating ColorBand for Operation=Cancel at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
		//		colorBandToUpdate.Cutoff = newCutoff;
		//		colorBandToUpdate.CancelEdit();
		//	}
		//	else
		//	{
		//		throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
		//	}
		//}

		//private void ColorBands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		//{
		//	Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");
		//}

		//private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		//{
		//	if (sender is ColorBand cb)
		//	{
		//		Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl:CurrentColorBand Prop: {e.PropertyName} is changing.");

		//		//var foundUpdate = false;

		//		if (e.PropertyName == nameof(ColorBand.StartColor))
		//		{
		//			//foundUpdate = true;
		//		}
		//		else if (e.PropertyName == nameof(ColorBand.Cutoff))
		//		{
		//			//foundUpdate = true;

		//			if (_selectionLineBeingDragged == null)
		//			{
		//				if (TryGetColorBandIndex(ColorBandsView, cb, out var index))
		//				{
		//					UpdateSelectionLinePosition(index.Value, cb.Cutoff);
		//				}
		//			}
		//		}
		//		else if (e.PropertyName == nameof(ColorBand.BlendStyle))
		//		{
		//			//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
		//			//foundUpdate = true;
		//		}
		//		else
		//		{
		//			if (e.PropertyName == nameof(ColorBand.EndColor))
		//			{
		//				//foundUpdate = true;
		//			}
		//		}
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"WARNING. HistogramColorBandControl: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
		//	}
		//}

		//private void ColorBand_EditEnded(object? sender, EventArgs e)
		//{
		//	if (sender is ColorBand cb)
		//	{

		//		if (TryGetColorBandIndex(ColorBandsView, cb, out var index))
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl. Handling the ColorBand_EditEnded event. Found: {index}, ColorBand: {cb}");
		//			UpdateSelectionLinePosition(index.Value, cb.Cutoff);
		//		}
		//		else
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl. Handling the ColorBand_EditEnded event. NOT Found: ColorBand: {cb}");
		//		}
		//	}
		//}

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

		private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
		{
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

		private void HistogramColorBandControl_LostFocus(object sender, RoutedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is losing focus.");
		}

		private void HistogramColorBandControl_GotFocus(object sender, RoutedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is receiving focus.");
		}


		#endregion

		#region ColorBand Support

		//private void UpdateSelectionLinePosition(int colorBandIndex, int newCutoff)
		//{
		//	if (_selectionLines.Count == 0)
		//	{
		//		return; // false;
		//	}

		//	if (colorBandIndex < 0 || colorBandIndex > _colorBandRectangles.Count - 2)
		//	{
		//		throw new InvalidOperationException($"DrawColorBands. The ColorBandIndex must be between 0 and {_colorBandRectangles.Count - 1}, inclusive.");
		//	}

		//	Debug.WriteLineIf(_useDetailedDebug, $"HistogramColorBandControl. About to call SelectionLine::UpdatePosition. Index = {colorBandIndex}");

		//	var selectionLine = _selectionLines[colorBandIndex];

		//	_ = selectionLine.UpdatePosition(newCutoff * ContentScale.Width);
		//}

		private bool TryGetColorBandIndex(ListCollectionView? colorbandsView, ColorBand cb, [NotNullWhen(true)] out int? index)
		{
			//var colorBandsList = colorbandsView as IList<ColorBand>;
			if (colorbandsView == null)
			{
				index = null;
				return false;
			}

			index = colorbandsView.IndexOf(cb);

			if (index < 0)
			{
				var t = colorbandsView.SourceCollection.Cast<ColorBand>();

				var cbWithMatchingOffset = t.FirstOrDefault(x => x.Cutoff == cb.Cutoff);

				if (cbWithMatchingOffset != null)
				{
					index = colorbandsView.IndexOf(cbWithMatchingOffset);
					Debug.WriteLine($"HistogramColorBandControl. The ColorBandsView does not contain the ColorBand: {cb}, but found an item with a matching offset: {cbWithMatchingOffset} at index: {index}.");

					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		#endregion

		#region Selection Line Support

		//private bool TryGetSelectionLine(Point hitPoint, IList<CbsSelectionLine> cbsSelectionLines, [NotNullWhen(true)] out CbsSelectionLine? cbsSelectionLine, [NotNullWhen(true)] out int? selectionLineIndex)
		//{
		//	cbsSelectionLine = null;
		//	selectionLineIndex = null;

		//	var lineAtHitPoint = GetLineUnderMouse(hitPoint);

		//	if (lineAtHitPoint != null)
		//	{
		//		for (var cbsLinePtr = 0; cbsLinePtr < cbsSelectionLines.Count; cbsLinePtr++)
		//		{
		//			var cbsLine = cbsSelectionLines[cbsLinePtr];

		//			var diffX = cbsLine.SelectionLinePosition - lineAtHitPoint.X1;

		//			if (ScreenTypeHelper.IsDoubleNearZero(diffX))
		//			{
		//				Debug.Assert(cbsLine.ColorBandIndex == cbsLinePtr, "CbsLine.ColorBandIndex Mismatch.");
		//				selectionLineIndex = cbsLinePtr;
		//				cbsSelectionLine = cbsLine;

		//				return true;
		//			}
		//		}
		//	}

		//	return false;
		//}

		//private Line? GetLineUnderMouse(Point hitPoint)
		//{
		//	_hitList.Clear();

		//	var hitArea = new EllipseGeometry(hitPoint, 2.0, 2.0);
		//	var hitTestParams = new GeometryHitTestParameters(hitArea);
		//	VisualTreeHelper.HitTest(Canvas, null, HitTestCallBack, hitTestParams);

		//	foreach (Shape item in _hitList)
		//	{
		//		if (item is Line line)
		//		{
		//			var adjustedPos = line.X1 / ContentScale.Width;
		//			Debug.WriteLineIf(_useDetailedDebug, $"Got a hit for line at position: {line.X1} / {adjustedPos}.");

		//			return line;
		//		}
		//	}

		//	return null;
		//}

		//private HitTestResultBehavior HitTestCallBack(HitTestResult result)
		//{
		//	if (result is GeometryHitTestResult hitTestResult)
		//	{
		//		switch (hitTestResult.IntersectionDetail)
		//		{
		//			case IntersectionDetail.NotCalculated:
		//				return HitTestResultBehavior.Stop;

		//			case IntersectionDetail.Empty:
		//				return HitTestResultBehavior.Stop;

		//			case IntersectionDetail.FullyInside:
		//				if (result.VisualHit is Shape s) _hitList.Add(s);
		//				return HitTestResultBehavior.Continue;

		//			case IntersectionDetail.FullyContains:
		//				if (result.VisualHit is Shape ss) _hitList.Add(ss);
		//				return HitTestResultBehavior.Continue;

		//			case IntersectionDetail.Intersects:
		//				if (result.VisualHit is Shape sss) _hitList.Add(sss);
		//				return HitTestResultBehavior.Continue;

		//			default:
		//				return HitTestResultBehavior.Stop;
		//		}
		//	}
		//	else
		//	{
		//		return HitTestResultBehavior.Stop;
		//	}
		//}

		#endregion

		#region ColorBandRectangle Support

		private bool TryGetColorBandRectangle(Point hitPoint, IList<CbsRectangle> colorBandRectangles, [NotNullWhen(true)] out CbsRectangle? colorBandRectangle, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		{
			colorBandRectangle = null;
			colorBandRectangleIndex = null;

			for (var i = 0; i < colorBandRectangles.Count; i++)
			{
				var cbRectangle = colorBandRectangles[i];

				if (cbRectangle.RectangleGeometry.FillContains(hitPoint))
				{
					colorBandRectangleIndex = i;
					colorBandRectangle = cbRectangle;
					return true;
				}
			}

			return false;
		}

		//private void HilightColorBandRectangle(ColorBand cb, bool on)
		//{
		//	if (TryGetColorBandIndex(ColorBandsView, cb, out var index))
		//	{
		//		if (index.Value < 0 || index.Value > _colorBandRectangles.Count - 1)
		//		{
		//			Debug.WriteLineIf(_useDetailedDebug, $"Cannot Hilight the ColorBandRectangle at index: {index}, it is out of range: {_colorBandRectangles.Count}.");
		//			return;
		//		}

		//		var cbr = _colorBandRectangles[index.Value];

		//		cbr.IsCurrent = on;
		//	}
		//}

		#endregion

		#region ColorBandSet View Support

		private void DrawColorBands(ListCollectionView? listCollectionView)
		{
			//RemoveColorBandRectangles();

			//if (listCollectionView == null || listCollectionView.Count < 2)
			//{
			//	return;
			//}

			//var scaleSize = new SizeDbl(ContentScale.Width, 1);

			////Debug.WriteLine($"****The scale is {scaleSize} on DrawColorBands.");

			//var endPtr = listCollectionView.Count - 1;

			//for (var colorBandIndex = 0; colorBandIndex <= endPtr; colorBandIndex++)
			//{
			//	var colorBand = (ColorBand)listCollectionView.GetItemAt(colorBandIndex);

			//	var xPosition = colorBand.PreviousCutoff ?? 0;
			//	var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
			//	var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			//	var isCurrent = colorBandIndex == _vm?.CurrentColorBandIndex;
			//	//var isSelected = _vm?.SelectedItemsArray[colorBandIndex]?.IsColorBandSelected ?? false;

			//	var isSelected = false;

			//	var cbsRectangle = new CbsRectangle(colorBandIndex, isCurrent, isSelected, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel, _canvas, CbRectangleIsSelectedChanged);

			//	_colorBandRectangles.Add(cbsRectangle);
			//}
		}



		private void DrawSelectionLines(IList<CbsRectangle> colorBandRectangles)
		{
			//RemoveSelectionLines();

			//for (var colorBandIndex = 0; colorBandIndex < colorBandRectangles.Count - 1; colorBandIndex++) 
			//{
			//	var gLeft = colorBandRectangles[colorBandIndex].RectangleGeometry;
			//	var gRight = colorBandRectangles[colorBandIndex + 1].RectangleGeometry;

			//	var gSelLeft = colorBandRectangles[colorBandIndex].SelRectangleGeometry;
			//	var gSelRight = colorBandRectangles[colorBandIndex + 1].SelRectangleGeometry;

			//	if (gLeft == null || gRight == null)
			//	{
			//		throw new InvalidOperationException("DrawSelectionLines. Either the left, right or both ColorBandRectangle geometrys are new RectangleGeometrys");
			//	}

			//	// This corresponds to the ColorBands Cutoff
			//	var xPosition = gLeft.Rect.Right;

			//	if (xPosition < 2)
			//	{
			//		Debug.WriteLine($"DrawSelectionLines found an xPosition with a value < 2.");
			//	}

			//	//var isSelected = _vm?.SelectedItemsArray[colorBandIndex]?.IsCutoffSelected ?? false;
			//	var isSelected = false;
			//	var sl = new CbsSelectionLine(colorBandIndex, isSelected, xPosition, gLeft, gRight, gSelLeft, gSelRight, _colorBandLayoutViewModel, _canvas, OffsetIsSelectedChanged);

			//	_selectionLines.Add(sl);
			//}
		}

		private int GetExtent(ListCollectionView? listCollectionView)
		{
			if (listCollectionView == null)
			{
				return 0;
			}

			var cnt = listCollectionView.Count;

			if (cnt < 2)
			{
				return 0;
			}

			var d = listCollectionView.GetItemAt(cnt - 1) as ColorBand;

			if (d != null)
			{
				return d.Cutoff;
			}
			else
			{
				return 0;
			}
		}

		//private void RemoveColorBandRectangles()
		//{
		//	//Debug.WriteLine($"Before remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");

		//	foreach (var colorBandRectangle in _colorBandRectangles)
		//	{
		//		colorBandRectangle.TearDown();
		//	}

		//	_colorBandRectangles.Clear();

		//	//Debug.WriteLine($"After remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");
		//}

		//private void RemoveSelectionLines()
		//{
		//	if (_selectionLineBeingDragged != null)
		//	{
		//		_selectionLineBeingDragged.CancelDrag();
		//		_selectionLineBeingDragged = null;
		//	}

		//	foreach (var selectionLine in _selectionLines)
		//	{
		//		selectionLine.TearDown();
		//	}

		//	_selectionLines.Clear();
		//}

		//private void HideSelectionLinesInternal()
		//{
		//	foreach (var selectionLine in _selectionLines)
		//	{
		//		selectionLine.Hide();
		//	}
		//}

		//private void ShowSelectionLinesInternal()
		//{
		//	foreach (var selectionLine in _selectionLines)
		//	{
		//		selectionLine.Show();
		//	}
		//}

		//private ColorBand GetColorBandAt(ListCollectionView cbsView, int index)
		//{
		//	try
		//	{
		//		var result = (ColorBand)cbsView.GetItemAt(index);
		//		return result;
		//	}
		//	catch (ArgumentOutOfRangeException aore)
		//	{
		//		throw new InvalidOperationException($"No item exists at index {index} within the ColorBandsView.", aore);
		//	}
		//	catch (InvalidCastException ice)
		//	{
		//		throw new InvalidOperationException($"The item at index {index} is not of type ColorBand.", ice);
		//	}
		//}

		#endregion

		#region Private Methods - Canvas

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
				Focusable = false,
				IsHitTestVisible = false
			};

			_canvas.Children.Add(result);
			_border = result;
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

		//[Conditional("DEGUG2")]
		//private void ReportColorBandRectanglesInPlay(int currentColorBandIndex)
		//{
		//	var sb = new StringBuilder();

		//	sb.AppendLine($"ColorBandRectangles for positions: {currentColorBandIndex} and {currentColorBandIndex + 1}.");

		//	var cbRectangleLeft = _colorBandRectangles[currentColorBandIndex];

		//	sb.AppendLine($"cbRectangleLeft: {cbRectangleLeft.RectangleGeometry}");

		//	var cbRectangleRight = _colorBandRectangles[currentColorBandIndex + 1];

		//	sb.AppendLine($"cbRectangleRight: {cbRectangleRight.RectangleGeometry}");

		//	Debug.WriteLine(sb);
		//}

		[Conditional("DEBUG2")]
		private void ReportTranslationTransformX(RectangleDbl previousValue, RectangleDbl newValue)
		{
			var previousXValue = previousValue.Position.X * ContentScale.Width;
			var newXValue = newValue.Position.X * ContentScale.Width;
			Debug.WriteLine(_useDetailedDebug, $"The HistogramColorBandControl's CanvasTranslationTransform is being set from {previousXValue} to {newXValue}.");
		}

		[Conditional("DEBUG")]
		private void ReportSetFocus(bool focusResult)
		{
			var elementWithFocus = Keyboard.FocusedElement;
			var elementWithLogicalFocus = FocusManager.GetFocusedElement(this);
			var focusScope = FocusManager.GetFocusScope(this);

			Debug.WriteLine($"HistogramColorBandControl. HandlePreviewLeftButtonDown. The Keyboard focus is now on {elementWithFocus}. The focus is at {elementWithLogicalFocus}. FocusScope: {focusScope}. FocusResult: {focusResult}.");

		}

		#endregion
	}
}
