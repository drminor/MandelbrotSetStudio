using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Text;
using System.Windows.Media;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using System.Windows.Media.Media3D;

namespace MSetExplorer
{
	internal class CbsListView
	{
		#region Private Fields

		private const int SELECTION_LINE_UPDATE_THROTTLE_INTERVAL = 200;
		private DebounceDispatcher _selectionLineMovedDispatcher;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private bool _mouseIsEntered;
		private List<Shape> _hitList;

		private CbsSelectionLine? _selectionLineBeingDragged;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsListView(Canvas canvas, ListCollectionView colorBandsView, double controlHeight, SizeDbl contentScale, bool useRealTimePreview, bool mouseIsEntered)
		{
			_selectionLineMovedDispatcher = new DebounceDispatcher
			{
				Priority = DispatcherPriority.Render
			};

			_canvas = canvas;
			_colorBandsView = colorBandsView;
			_colorBandsView.CurrentChanged += _colorBandsView_CurrentChanged;
			UseRealTimePreview = useRealTimePreview;
			_mouseIsEntered = mouseIsEntered;

			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBands_CollectionChanged;

			ListViewItems = new List<CbsListViewItem>();

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(contentScale, controlHeight);

			_hitList = new List<Shape>();

			//RemoveSelectionLines();
			DrawColorBands(_colorBandsView, showSectionLines: _mouseIsEntered);

			if (_mouseIsEntered)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsListView is calling DrawSelectionLines on ColorBandsView update. (Have Mouse)");

				//DrawSelectionLines(ListViewItems);
			}
		}

		private void _colorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			CurrentColorBand = (ColorBand)_colorBandsView.CurrentItem;
		}

		#endregion

		#region Public Properties

		public List<CbsListViewItem> ListViewItems { get; set; }
		public bool IsEmpty => ListViewItems.Count == 0;

		public ColorBand? CurrentColorBand
		{
			get => _currentColorBand;
			set
			{
				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged -= ColorBand_PropertyChanged;
					HilightColorBandRectangle(_currentColorBand, on: false);
				}

				_currentColorBand = value;

				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged += ColorBand_PropertyChanged;
					_currentColorBand.EditEnded += ColorBand_EditEnded;
					HilightColorBandRectangle(_currentColorBand, on: true);
				}
			}
		}

		public bool UseRealTimePreview { get; set; }


		public double ControlHeight
		{
			get => _colorBandLayoutViewModel.ControlHeight;
			set => _colorBandLayoutViewModel.ControlHeight = value;
		}

		public SizeDbl ContentScale
		{
			get => _colorBandLayoutViewModel.ContentScale;
			set => _colorBandLayoutViewModel.ContentScale = new SizeDbl(value.Width, 1);
		}

		public double CbrElevation => _colorBandLayoutViewModel.CbrElevation;

		public bool IsDragSelectionLineInProgress => _selectionLineBeingDragged != null;


		public bool MouseIsEntered
		{
			get => _mouseIsEntered;
			set
			{
				if (value != _mouseIsEntered)
				{
					_mouseIsEntered = value;
					
					if (value)
					{
						ShowSelectionLinesInternal();
					}
					else
					{
						HideSelectionLinesInternal();
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void ShowSelectionLines(bool leftMouseButtonIsPressed)
		{
			if (_selectionLineBeingDragged != null)
			{
				if (!leftMouseButtonIsPressed)
				{
					_selectionLineBeingDragged.CancelDrag();
					_selectionLineBeingDragged = null;
				}
				else
				{
					// Drag operation is in process.
					// The selection lines should already be visible.
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on Handle_MouseEnter.");

				if (ListViewItems.Count == 0)
				{
					DrawSelectionLines(ListViewItems);
				}
				else
				{
					ShowSelectionLinesInternal();
				}
			}
		}

		public void HideSelectionLines(bool leftMouseButtonIsPressed)
		{
			if (_selectionLineBeingDragged != null)
			{
				if (!leftMouseButtonIsPressed)
				{
					_selectionLineBeingDragged.CancelDrag();
					_selectionLineBeingDragged = null;
					HideSelectionLinesInternal();
				}
				else
				{
					// Drag operation is in process.
					// Do not hide the selection lines.
				}
			}
			else
			{
				HideSelectionLinesInternal();
			}
		}

		public void Clear()
		{
			ListViewItems.Clear();
		}

		public bool CancelDrag()
		{
			if (_selectionLineBeingDragged != null)
			{
				_selectionLineBeingDragged.CancelDrag();
				_selectionLineBeingDragged = null;

				return true;
			}
			else
			{
				return false;
			}
		}

		public void TearDown()
		{

		}

		public ColorBandSelectionType Select(ColorBand colorBand, ColorBandSelectionType colorBandSelectionType)
		{
			//var selectedItem = SelectedColorBands.FirstOrDefault(x => x.ColorBand == colorBand);

			//if (selectedItem == null)
			//{
			//	selectedItem = new ColorBandViewItem(colorBand);
			//	SelectedColorBands.Add(selectedItem);
			//}

			////selectedItem.IsCutoffSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Cutoff);
			////selectedItem.IsColorSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Color);

			//if (colorBandSelectionType.HasFlag(ColorBandSelectionType.Cutoff))
			//{
			//	selectedItem.IsCutoffSelected = !selectedItem.IsCutoffSelected;
			//}

			//if (colorBandSelectionType.HasFlag(ColorBandSelectionType.Color))
			//{
			//	selectedItem.IsColorSelected = !selectedItem.IsColorSelected;
			//}

			//var result = selectedItem.IsCutoffSelected ? ColorBandSelectionType.Cutoff : ColorBandSelectionType.None;

			//if (selectedItem.IsColorSelected)
			//{
			//	result |= ColorBandSelectionType.Color; // | ColorBandSelectionType.EndColor;
			//}

			var result = ColorBandSelectionType.None;

			return result;
		}

		#endregion


		#region Event Handlers

		private void Handle_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is handling the SizeChanged event.");

			_colorBandLayoutViewModel.ControlHeight = e.NewSize.Height;
		}

		private void ColorBandLayoutViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandLayoutViewModel.CbrHeight))
			{
				//RemoveSelectionLines();
				DrawColorBands(_colorBandsView, showSectionLines: _mouseIsEntered);

				if (_mouseIsEntered)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The HistogramColorBandControl is calling DrawSelectionLines on CbrHeight update. (Have Mouse)");
					DrawSelectionLines(ListViewItems);
				}
			}
		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var cbsView = _colorBandsView;

			if (cbsView == null)
				return;

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbSelectionLine, out var cbSelectionLineIndex))
			{
				var colorBandIndex = cbSelectionLineIndex.Value;

				var cb = GetColorBandAt(cbsView, cbSelectionLineIndex.Value);
				cbsView.MoveCurrentTo(cb);

				//Debug.WriteLineIf(_useDetailedDebug, $"Starting Drag. ColorBandIndex = {cbSelectionLineIndex}. ContentScale: {ContentScale}. PosX: {hitPoint.X}. Original X: {cbSelectionLine.SelectionLinePosition}.");
				//ReportColorBandRectanglesInPlay(cbSelectionLineIndex.Value);

				_selectionLineBeingDragged = cbSelectionLine;
				_selectionLineBeingDragged.SelectionLineMoved += HandleSelectionLineMoved;

				Debug.WriteIf(_useDetailedDebug, $"CbsListView. Starting Drag for cbSelectionLine at index: {cbSelectionLineIndex}, Current View Position: {cbsView.CurrentPosition}, ColorBand: {cb}.");


				var gLeft = ListViewItems[colorBandIndex].CbsRectangle.RectangleGeometry;
				var gRight = ListViewItems[colorBandIndex + 1].CbsRectangle.RectangleGeometry;

				var gSelLeft = ListViewItems[colorBandIndex].CbsRectangle.SelRectangleGeometry;
				var gSelRight = ListViewItems[colorBandIndex + 1].CbsRectangle.SelRectangleGeometry;

				if (gLeft == null || gRight == null)
				{
					throw new InvalidOperationException("DrawSelectionLines. Either the left, right or both ColorBandRectangle geometrys are new RectangleGeometrys");
				}

				var rg = new RectangleGeometries(gLeft, gRight, gSelLeft, gSelRight);

				cbSelectionLine.StartDrag(rg);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out var cbsRectangle, out var cbRectangleIndex))
				{
					var cb = GetColorBandAt(cbsView, cbRectangleIndex.Value);
					cbsView.MoveCurrentTo(cb);
				}
			}

			//var focusResult = Focus();
			//ReportSetFocus(focusResult);
		}

		private void HandleSelectionLineMoved(object? sender, CbsSelectionLineMovedEventArgs e)
		{
			if (_selectionLineBeingDragged == null)
			{
				Debug.WriteLine("WARNING: _selectionLineBeingDragged is null on HandleSelectionLineMoved.");
				return;
			}

			if (!(sender is CbsSelectionLine selectionLine))
			{
				throw new InvalidOperationException("The HandleSelectionLineMoved event is being raised by some class other than CbsSelectionLine.");
			}
			else
			{
				if (sender != _selectionLineBeingDragged)
				{
					Debug.WriteLine("WARNING: HandleSelectionLineMoved is being raised by a SelectionLine other than the one that is being dragged.");
				}
			}

			if (e.NewXPosition == 0)
			{
				Debug.WriteLine($"WARNING: Setting the Cutoff to zero for ColorBandIndex: {e.ColorBandIndex}.");
			}

			switch (e.Operation)
			{
				case CbsSelectionLineDragOperation.Move:
					UpdateCutoffThrottled(e);
					break;

				case CbsSelectionLineDragOperation.Complete:
					_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
					_selectionLineBeingDragged = null;

					Debug.WriteLineIf(_useDetailedDebug, "Completing the SelectionBand Drag Operation.");
					UpdateCutoffThrottled(e);
					break;

				case CbsSelectionLineDragOperation.Cancel:
					_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
					_selectionLineBeingDragged = null;

					UpdateCutoffThrottled(e);
					break;

				default:
					throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		private void UpdateCutoffThrottled(CbsSelectionLineMovedEventArgs e)
		{
			if (UseRealTimePreview)
			{
				_selectionLineMovedDispatcher.Throttle(
					interval: SELECTION_LINE_UPDATE_THROTTLE_INTERVAL,
					action: parm =>
					{
						UpdateCutoff(e);
					},
					param: null);
			}
			else
			{
				//currentColorBand.Cutoff = newCutoff;
				UpdateCutoff(e);
			}
		}

		private void UpdateCutoff(CbsSelectionLineMovedEventArgs e)
		{
			var cbView = _colorBandsView;

			if (cbView == null)
				return;

			var newCutoff = (int)Math.Round(e.NewXPosition / ContentScale.Width);

			var colorBandToUpdate = GetColorBandAt(cbView, e.ColorBandIndex);

			if (e.Operation == CbsSelectionLineDragOperation.Move)
			{
				if (!colorBandToUpdate.IsInEditMode)
				{
					colorBandToUpdate.BeginEdit();
				}

				Debug.WriteIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Move Operation at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Complete)
			{
				Debug.WriteIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Operation=Complete at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.EndEdit();
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Cancel)
			{
				Debug.WriteIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Operation=Cancel at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.CancelEdit();
			}
			else
			{
				throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		private void ColorBands_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");
		}

		private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsListView:CurrentColorBand Prop: {e.PropertyName} is changing.");

				//var foundUpdate = false;

				if (e.PropertyName == nameof(ColorBand.StartColor))
				{
					//foundUpdate = true;
				}
				else if (e.PropertyName == nameof(ColorBand.Cutoff))
				{
					//foundUpdate = true;

					if (_selectionLineBeingDragged == null)
					{
						if (TryGetColorBandIndex(_colorBandsView, cb, out var index))
						{
							UpdateSelectionLinePosition(index.Value, cb.Cutoff);
						}
					}
				}
				else if (e.PropertyName == nameof(ColorBand.BlendStyle))
				{
					//cb.ActualEndColor = cb.BlendStyle == ColorBandBlendStyle.Next ? cb.SuccessorStartColor : cb.BlendStyle == ColorBandBlendStyle.None ? cb.StartColor : cb.EndColor;
					//foundUpdate = true;
				}
				else
				{
					if (e.PropertyName == nameof(ColorBand.EndColor))
					{
						//foundUpdate = true;
					}
				}
			}
			else
			{
				Debug.WriteLine($"WARNING. HistogramColorBandControl: A sender of type {sender?.GetType()} is raising the CurrentColorBand_PropertyChanged event. EXPECTED: {typeof(ColorBand)}.");
			}
		}

		private void ColorBand_EditEnded(object? sender, EventArgs e)
		{
			if (sender is ColorBand cb)
			{

				if (TryGetColorBandIndex(_colorBandsView, cb, out var index))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Handling the ColorBand_EditEnded event. Found: {index}, ColorBand: {cb}");
					UpdateSelectionLinePosition(index.Value, cb.Cutoff);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Handling the ColorBand_EditEnded event. NOT Found: ColorBand: {cb}");
				}
			}
		}

		private ColorBand GetColorBandAt(ListCollectionView cbsView, int index)
		{
			try
			{
				var result = (ColorBand)cbsView.GetItemAt(index);
				return result;
			}
			catch (ArgumentOutOfRangeException aore)
			{
				throw new InvalidOperationException($"No item exists at index {index} within the ColorBandsView.", aore);
			}
			catch (InvalidCastException ice)
			{
				throw new InvalidOperationException($"The item at index {index} is not of type ColorBand.", ice);
			}
		}
		#endregion


		#region ColorBand Support

		private void UpdateSelectionLinePosition(int colorBandIndex, int newCutoff)
		{
			if (ListViewItems.Count == 0)
			{
				return; // false;
			}

			if (colorBandIndex < 0 || colorBandIndex > ListViewItems.Count - 2)
			{
				throw new InvalidOperationException($"CbsListView::UpdateSelectionLinePosition. The ColorBandIndex must be between 0 and {ListViewItems.Count - 1}, inclusive.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. About to call SelectionLine::UpdatePosition. Index = {colorBandIndex}");

			var selectionLine = ListViewItems[colorBandIndex].CbsSelectionLine;

			_ = selectionLine.UpdatePosition(newCutoff * ContentScale.Width);
		}

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
					Debug.WriteLine($"CbsListView. The ColorBandsView does not contain the ColorBand: {cb}, but found an item with a matching offset: {cbWithMatchingOffset} at index: {index}.");

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

		private bool TryGetSelectionLine(Point hitPoint, List<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsSelectionLine? cbsSelectionLine, [NotNullWhen(true)] out int? selectionLineIndex)
		{
			cbsSelectionLine = null;
			selectionLineIndex = null;

			var lineAtHitPoint = GetLineUnderMouse(hitPoint);

			if (lineAtHitPoint != null)
			{
				for (var cbsLinePtr = 0; cbsLinePtr < cbsListViewItems.Count; cbsLinePtr++)
				{
					var cbsLine = cbsListViewItems[cbsLinePtr].CbsSelectionLine;

					var diffX = cbsLine.SelectionLinePosition - lineAtHitPoint.X1;

					if (ScreenTypeHelper.IsDoubleNearZero(diffX))
					{
						Debug.Assert(cbsLine.ColorBandIndex == cbsLinePtr, "CbsLine.ColorBandIndex Mismatch.");
						selectionLineIndex = cbsLinePtr;
						cbsSelectionLine = cbsLine;

						return true;
					}
				}
			}

			return false;
		}

		private Line? GetLineUnderMouse(Point hitPoint)
		{
			_hitList.Clear();

			var hitArea = new EllipseGeometry(hitPoint, 2.0, 2.0);
			var hitTestParams = new GeometryHitTestParameters(hitArea);
			VisualTreeHelper.HitTest(_canvas, null, HitTestCallBack, hitTestParams);

			foreach (Shape item in _hitList)
			{
				if (item is Line line)
				{
					var adjustedPos = line.X1 / ContentScale.Width;
					Debug.WriteLineIf(_useDetailedDebug, $"Got a hit for line at position: {line.X1} / {adjustedPos}.");

					return line;
				}
			}

			return null;
		}

		private HitTestResultBehavior HitTestCallBack(HitTestResult result)
		{
			if (result is GeometryHitTestResult hitTestResult)
			{
				switch (hitTestResult.IntersectionDetail)
				{
					case IntersectionDetail.NotCalculated:
						return HitTestResultBehavior.Stop;

					case IntersectionDetail.Empty:
						return HitTestResultBehavior.Stop;

					case IntersectionDetail.FullyInside:
						if (result.VisualHit is Shape s) _hitList.Add(s);
						return HitTestResultBehavior.Continue;

					case IntersectionDetail.FullyContains:
						if (result.VisualHit is Shape ss) _hitList.Add(ss);
						return HitTestResultBehavior.Continue;

					case IntersectionDetail.Intersects:
						if (result.VisualHit is Shape sss) _hitList.Add(sss);
						return HitTestResultBehavior.Continue;

					default:
						return HitTestResultBehavior.Stop;
				}
			}
			else
			{
				return HitTestResultBehavior.Stop;
			}
		}

		#endregion

		#region ColorBandRectangle Support

		private bool TryGetColorBandRectangle(Point hitPoint, IList<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsRectangle? colorBandRectangle, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		{
			colorBandRectangle = null;
			colorBandRectangleIndex = null;

			for (var i = 0; i < cbsListViewItems.Count; i++)
			{
				var cbRectangle = cbsListViewItems[i].CbsRectangle;

				if (cbRectangle.RectangleGeometry.FillContains(hitPoint))
				{
					colorBandRectangleIndex = i;
					colorBandRectangle = cbRectangle;
					return true;
				}
			}

			return false;
		}

		private void HilightColorBandRectangle(ColorBand cb, bool on)
		{
			if (TryGetColorBandIndex(_colorBandsView, cb, out var index))
			{
				if (index.Value < 0 || index.Value > ListViewItems.Count - 1)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Cannot Hilight the ColorBandRectangle at index: {index}, it is out of range: {ListViewItems.Count}.");
					return;
				}

				var cbr = ListViewItems[index.Value].CbsRectangle;

				cbr.IsCurrent = on;
			}
		}

		#endregion

		#region Private Methods


	//	RemoveSelectionLines();
	//	DrawColorBands(_colorBandsView);

	//		if (_mouseIsEntered)
	//		{
	//			Debug.WriteLineIf(_useDetailedDebug, $"The CbsListView is calling DrawSelectionLines on ColorBandsView update. (Have Mouse)");

	//			DrawSelectionLines(ListViewItems);
	//}

	private void DrawColorBands(ListCollectionView? listCollectionView, bool showSectionLines)
		{
			//RemoveSelectionLines();
			RemoveColorBandRectangles();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			var scaleSize = new SizeDbl(ContentScale.Width, 1);

			//Debug.WriteLine($"****The scale is {scaleSize} on DrawColorBands.");

			var endPtr = listCollectionView.Count - 1;

			for (var colorBandIndex = 0; colorBandIndex <= endPtr; colorBandIndex++)
			{
				var colorBand = (ColorBand)listCollectionView.GetItemAt(colorBandIndex);

				var xPosition = colorBand.PreviousCutoff ?? 0;
				var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
				var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

				//var isCurrent = colorBandIndex == _vm?.CurrentColorBandIndex;
				var isCurrent = colorBandIndex == listCollectionView.CurrentPosition;

				//var isSelected = _vm?.SelectedItemsArray[colorBandIndex]?.IsColorBandSelected ?? false;

				var isColorBandSelected = false;

				var cbsRectangle = new CbsRectangle(colorBandIndex, isCurrent, isColorBandSelected, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel, _canvas, CbRectangleIsSelectedChanged);

				var showSelectionLines = _mouseIsEntered;
				//var isSelected = _vm?.SelectedItemsArray[colorBandIndex]?.IsCutoffSelected ?? false;
				var isCutoffSelected = false;
				var cbsSelectionLine = new CbsSelectionLine(colorBandIndex, isCutoffSelected, xPosition, _colorBandLayoutViewModel, _canvas, OffsetIsSelectedChanged, showSelectionLines);

				ListViewItems.Add(new CbsListViewItem(colorBand, cbsRectangle, cbsSelectionLine));
				//_colorBandRectangles.Add(cbsRectangle);
			}
		}

		private void DrawSelectionLines(List<CbsListViewItem> listViewItems)
		{
			//RemoveSelectionLines();

			//for (var colorBandIndex = 0; colorBandIndex < listViewItems.Count - 1; colorBandIndex++)
			//{
			//	var gLeft = listViewItems[colorBandIndex].CbsRectangle.RectangleGeometry;
			//	var gRight = listViewItems[colorBandIndex + 1].CbsRectangle.RectangleGeometry;

			//	var gSelLeft = listViewItems[colorBandIndex].CbsRectangle.SelRectangleGeometry;
			//	var gSelRight = listViewItems[colorBandIndex + 1].CbsRectangle.SelRectangleGeometry;

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

		private void CbRectangleIsSelectedChanged(int colorBandIndex, bool newIsSelectedValue, bool shiftKeyPressed, bool controlKeyPressed)
		{
			//var cbsView = _colorBandsView;

			//if (_vm == null || cbsView == null)
			//{
			//	return;
			//}

			//if (!(shiftKeyPressed || controlKeyPressed))
			//{
			//	foreach (var selCb in _vm.SelectedItems.SelectedColorBands)
			//	{
			//		if (TryGetColorBandIndex(cbsView, selCb.ColorBand, out var index))
			//		{
			//			_colorBandRectangles[index.Value].IsSelected = false;
			//		}
			//	}

			//	_vm.ClearSelectedItems();
			//}

			//var colorBand = GetColorBandAt(cbsView, colorBandIndex);
			//var resultantSelType = _vm.SelectedItems.Select(colorBand, ColorBandSelectionType.Band);

			//_colorBandRectangles[colorBandIndex].IsSelected = resultantSelType.HasFlag(ColorBandSelectionType.Color);
			////_selectionLines[colorBandIndex].IsSelected = resultantSelType.HasFlag(ColorBandSelectionType.Cutoff);
		}

		private void OffsetIsSelectedChanged(int colorBandIndex, bool newIsSelectedValue, bool shiftKeyPressed, bool controlKeyPressed)
		{
			//var cbsView = _colorBandsView;

			//if (_vm == null || cbsView == null)
			//{
			//	return;
			//}

			//if (  !(shiftKeyPressed || controlKeyPressed)  )
			//{
			//	foreach (var selCb in _vm.SelectedItems.SelectedColorBands)
			//	{
			//		if (TryGetColorBandIndex(cbsView, selCb.ColorBand, out var index))
			//		{
			//			_selectionLines[index.Value].IsSelected = false;
			//		}
			//	}

			//	_vm.ClearSelectedItems();
			//}

			//var colorBand = GetColorBandAt(cbsView, colorBandIndex);
			//var resultantSelType = _vm.SelectedItems.Select(colorBand, ColorBandSelectionType.Cutoff);
			//_selectionLines[colorBandIndex].IsSelected = resultantSelType.HasFlag(ColorBandSelectionType.Cutoff);
		}

		private void RemoveColorBandRectangles()
		{
			//Debug.WriteLine($"Before remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");

			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbsRectangle.TearDown();
			}

			ListViewItems.Clear();

			//Debug.WriteLine($"After remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");
		}

		//private void RemoveSelectionLines()
		//{
		//	if (_selectionLineBeingDragged != null)
		//	{
		//		_selectionLineBeingDragged.CancelDrag();
		//		_selectionLineBeingDragged = null;
		//	}

		//	foreach (var listViewItem in ListViewItems)
		//	{
		//		listViewItem.CbsSelectionLine.TearDown();
		//	}

		//	//ListViewItems.Clear();
		//}

		private void HideSelectionLinesInternal()
		{
			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbsSelectionLine.Hide();
			}
		}

		private void ShowSelectionLinesInternal()
		{
			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbsSelectionLine.Show();
			}
		}

		#endregion

		#region Diagnostics

		[Conditional("DEGUG2")]
		private void ReportColorBandRectanglesInPlay(List<CbsListViewItem> listViewItems, int currentColorBandIndex)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"ColorBandRectangles for positions: {currentColorBandIndex} and {currentColorBandIndex + 1}.");

			var cbRectangleLeft = listViewItems[currentColorBandIndex].CbsRectangle;

			sb.AppendLine($"cbRectangleLeft: {cbRectangleLeft.RectangleGeometry}");

			var cbRectangleRight = listViewItems[currentColorBandIndex + 1].CbsRectangle;

			sb.AppendLine($"cbRectangleRight: {cbRectangleRight.RectangleGeometry}");

			Debug.WriteLine(sb);
		}

		#endregion
	}

	internal class CbsListViewItem
	{
		private bool _isCutoffSelected;
		private bool _isColorSelected;

		private ColorBandSelectionType _selectionType;

		public CbsListViewItem(ColorBand colorBand, CbsRectangle cbsRectangle, CbsSelectionLine cbsSelectionLine)
		{
			ColorBand = colorBand;
			CbsRectangle = cbsRectangle;
			CbsSelectionLine = cbsSelectionLine;

			_isCutoffSelected = false;
			_isColorSelected = false;
			_selectionType = 0;
		}

		public ColorBand ColorBand { get; init; }

		public CbsRectangle CbsRectangle { get; init; }
		public CbsSelectionLine CbsSelectionLine { get; init; }

		public ColorBandSelectionType SelectionType
		{
			get => _selectionType;

			set
			{
				if (value != _selectionType)
				{
					_selectionType = value;

					IsCutoffSelected = _selectionType.HasFlag(ColorBandSelectionType.Cutoff);
					IsColorSelected = _selectionType.HasFlag(ColorBandSelectionType.Cutoff);
				}
			}
		}

		public bool IsCutoffSelected
		{
			get => _isCutoffSelected;
			set
			{
				if (value != _isCutoffSelected)
				{
					_isCutoffSelected = value;

					if (value)
					{
						_selectionType |= ColorBandSelectionType.Cutoff;
					}
					else
					{
						_selectionType &= ColorBandSelectionType.Color;
					}
				}
			}
		}

		public bool IsColorSelected
		{
			get => _isColorSelected;
			set
			{
				if (value != _isCutoffSelected)
				{
					_isColorSelected = value;

					if (value)
					{
						_selectionType |= ColorBandSelectionType.Color;
					}
					else
					{
						_selectionType &= ColorBandSelectionType.Cutoff;
					}
				}
			}
		}

		public bool IsColorBandSelected => IsCutoffSelected & IsColorSelected;

		public bool IsItemSelected => IsCutoffSelected | IsColorSelected;

	}

	[Flags]
	public enum ColorBandSelectionType
	{
		None = 0,
		Cutoff = 1,
		Color = 2,
		Band = 3
	}
}
