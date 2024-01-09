using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MSetExplorer
{
	internal class CbsListView
	{
		#region Private Fields

		private ContextMenuDisplayRequest _displayContextMenu;
		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		//private ColorBand? _currentColorBand;
		private int _currentColorBandIndex;

		//private List<Shape> _hitList;

		private CbsSelectionLine? _selectionLineBeingDragged;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsListView(Canvas canvas, ListCollectionView colorBandsView, double controlHeight, SizeDbl contentScale, bool useRealTimePreview, bool parentIsFocused, ContextMenuDisplayRequest displayContextMenu)
		{
			_canvas = canvas;
			_colorBandsView = colorBandsView;

			_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBandsView_CollectionChanged;

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(contentScale, controlHeight, parentIsFocused);

			UseRealTimePreview = useRealTimePreview;
			_displayContextMenu = displayContextMenu;

			ListViewItems = new List<CbsListViewItem>();

			//_hitList = new List<Shape>();

			DrawColorBands(_colorBandsView, showSectionLines: ParentIsFocused);

			_canvas.MouseMove += Handle_MouseMove;
			_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;
		}

		#endregion

		#region Public Properties

		public List<CbsListViewItem> ListViewItems { get; set; }
		public bool IsEmpty => ListViewItems.Count == 0;

		//public ColorBand? CurrentColorBand
		//{
		//	get => _currentColorBand;
		//	set
		//	{
		//		_currentColorBand = value;
		//	}
		//}

		public int CurrentColorBandIndex
		{
			get => _currentColorBandIndex;
			set
			{
				var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

				if ( !(_colorBandsView.IsCurrentBeforeFirst || _colorBandsView.IsCurrentAfterLast) )
				{
					HilightColorBandRectangle(_currentColorBandIndex, newState: false);
					CbRectangleIsSelectedChanged(_currentColorBandIndex, true, shiftKeyPressed, controlKeyPressed);
				}

				_currentColorBandIndex = value;

				if (!(_colorBandsView.IsCurrentBeforeFirst || _colorBandsView.IsCurrentAfterLast))
				{
					HilightColorBandRectangle(_currentColorBandIndex, newState: true);
					CbRectangleIsSelectedChanged(_currentColorBandIndex, true, shiftKeyPressed, controlKeyPressed);
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
			set => _colorBandLayoutViewModel.ContentScale = value;
		}

		public double CbrElevation => _colorBandLayoutViewModel.CbrElevation;

		public bool IsDragSelectionLineInProgress => _selectionLineBeingDragged != null;

		public bool ParentIsFocused
		{
			get => _colorBandLayoutViewModel.ParentIsFocused;
			set => _colorBandLayoutViewModel.ParentIsFocused = value;
		}

		#endregion

		#region Public Methods

		public (CbsListViewItem, ColorBandSelectionType)? ItemAtMousePosition(Point hitPoint)
		{
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbsListViewItem))
			{
				return (cbsListViewItem, ColorBandSelectionType.Cutoff);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbsListViewItem))
				{
					return (cbsListViewItem, ColorBandSelectionType.Color);
				}
			}

			return null;
		}

		public void ClearSelectedItems()
		{
			// TODO: Set each CbsRectangle's and each CbsSelectionLine's IsSelected to false.
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
			_colorBandsView.CurrentChanged -= ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged -= ColorBandsView_CollectionChanged;

			_canvas.MouseMove -= Handle_MouseMove;
			_canvas.PreviewMouseLeftButtonDown -= Handle_PreviewMouseLeftButtonDown;

			ClearListViewItems();
		}

		#endregion

		#region Event Handlers

		private CbsListViewItem? _itemUnderMouse;

		private CbsListViewItem? ItemUnderMouse
		{
			get => _itemUnderMouse;
			set
			{
				if (value != _itemUnderMouse)
				{
					_itemUnderMouse = value;

					if (SelectionLineUnderMouse != null)
					{
						Debug.WriteLine($"The Mouse is now over SelectionLine: {SelectionLineUnderMouse.ColorBandIndex} and Rectangle: {ItemUnderMouse?.ColorBandIndex ?? -1}.");
					}
					else
					{
						Debug.WriteLine($"The Mouse is now over Rectangle: {ItemUnderMouse?.ColorBandIndex ?? -1}.");
					}

				}
			}
		}

		private CbsListViewItem? _selectionLineUnderMouse;

		private CbsListViewItem? SelectionLineUnderMouse
		{
			get => _selectionLineUnderMouse;
			set
			{
				if (value != _selectionLineUnderMouse)
				{
					if (_selectionLineUnderMouse != null) _selectionLineUnderMouse.CbsSelectionLine.IsUnderMouse = false;
					_selectionLineUnderMouse = value;
					if (_selectionLineUnderMouse != null) _selectionLineUnderMouse.CbsSelectionLine.IsUnderMouse = true;
					Debug.WriteLine($"The ItemUnderMouse is now: {ItemUnderMouse}.");
				}
			}
		}


		private void Handle_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsDragSelectionLineInProgress)
			{
				Debug.WriteLine("WARNING: CbsListView is receiving a MouseMove event, as a Drag SelectionLine is in progress.");
				return;
			}

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbsListViewItem))
			{
				SelectionLineUnderMouse = cbsListViewItem;

				// Positive if the mouse is to the right of the selection line, negative if to the left.
				var hitPointDistance = hitPoint.X - cbsListViewItem.SelectionLinePosition;

				// True if we will be updating the Current ColorBand's PreviousCutoff value, false if updating the Cutoff
				bool updatingPrevious = hitPointDistance > 0;

				if (updatingPrevious)
				{
					var index = cbsListViewItem.ColorBandIndex;
					ItemUnderMouse = ListViewItems[index + 1];
				}
				else
				{
					ItemUnderMouse = cbsListViewItem;
				}

				//e.Handled = true;
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbsListViewItem))
				{
					SelectionLineUnderMouse = null;
					ItemUnderMouse = cbsListViewItem;
					//e.Handled = true;
				}
			}

		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Handling PreviewMouseLeftButtonDown. ContentScale is {ContentScale}.");

			var cbsView = _colorBandsView;

			if (cbsView == null)
				return;

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbsListViewItem))
			{
				e.Handled = true;

				Debug.WriteLine($"Got SelectionLine. SelLineIndex: {cbsListViewItem.ColorBandIndex}.");
				StartDrag(cbsListViewItem, hitPoint);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbsListViewItem))
				{
					//e.Handled = true;

					var cb = cbsListViewItem.ColorBand;

					//var cbOld = GetColorBandAt(cbsView, cbRectangleIndex.Value);
					//Debug.Assert(cb == cbOld, "ColorBand MisMatch Rectangle.");

					cbsView.MoveCurrentTo(cb);
				}
			}
		}

		private void StartDrag(CbsListViewItem cbsListViewItem, Point hitPoint)
		{
			var colorBandIndex = cbsListViewItem.ColorBandIndex;

			if (cbsListViewItem.IsLast)
			{
				// Cannot change the position of the last Selection Line.
				return;
			}

			// Positive if the mouse is to the right of the selection line, negative if to the left.
			var hitPointDistance = hitPoint.X - cbsListViewItem.SelectionLinePosition;

			// True if we will be updating the Current ColorBand's PreviousCutoff value, false if updating the Cutoff
			bool updatingPrevious = hitPointDistance > 0;

			var indexForCurrentItem = updatingPrevious ? colorBandIndex + 1 : colorBandIndex;
			_colorBandsView?.MoveCurrentToPosition(indexForCurrentItem);

			var cbSelectionLine = cbsListViewItem.CbsSelectionLine;
			_selectionLineBeingDragged = cbSelectionLine;

			var prevMsg = updatingPrevious ? "Previous" : "Current";
			Debug.WriteIf(_useDetailedDebug, $"CbsListView. Starting Drag for cbSelectionLine at index: {colorBandIndex} for {prevMsg}.");
			//ReportColorBandRectanglesInPlay(cbSelectionLineIndex.Value);

			var gLeft = ListViewItems[colorBandIndex].CbsRectangle.RectangleGeometry;
			var gRight = ListViewItems[colorBandIndex + 1].CbsRectangle.RectangleGeometry;

			cbSelectionLine.StartDrag(gLeft.Rect.Width, gRight.Rect.Width, updatingPrevious);
		}

		private void HandleSelectionLineMoved(object? sender, CbsSelectionLineMovedEventArgs e)
		{
			if (_selectionLineBeingDragged == null)
			{
				Debug.WriteLine("WARNING: _selectionLineBeingDragged is null on HandleSelectionLineMoved.");
				return;
			}

			if (!(sender is CbsSelectionLine))
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

			if (e.Operation == CbsSelectionLineDragOperation.NotStarted)
			{
				_selectionLineBeingDragged = null;
				return;
			}

			if (_selectionLineBeingDragged.DragState != DragState.InProcess)
			{
				Debug.WriteLine($"WARNING: The _selectionLineBeingDragged's DragState is {_selectionLineBeingDragged.DragState}, exepcting: {DragState.InProcess}.");
			}

			if (e.NewCutoff == 0 && !e.UpdatingPrevious)
			{
				Debug.WriteLine($"WARNING: Setting the Cutoff to zero for ColorBandIndex: {e.ColorBandIndex}.");
			}

			switch (e.Operation)
			{
				case CbsSelectionLineDragOperation.Move:
					UpdateCutoff(e);
					break;

				case CbsSelectionLineDragOperation.Complete:
					_selectionLineBeingDragged = null;

					Debug.WriteLineIf(_useDetailedDebug, "Completing the SelectionBand Drag Operation.");
					UpdateCutoff(e);
					break;

				case CbsSelectionLineDragOperation.Cancel:
					_selectionLineBeingDragged = null;

					UpdateCutoff(e);
					break;

				default:
					throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			CurrentColorBandIndex = _colorBandsView.CurrentPosition;
		}

		private void ColorBandsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Debug.WriteLine($"CbsListView is CollectionChanged: Reset.");
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items

				Debug.WriteLine($"CbsListView is handling CollectionChanged: Add. There are {e.NewItems?.Count ?? -1} new items.");

				var bands = e.NewItems?.Cast<ColorBand>() ?? new List<ColorBand>();
				var idx = e.NewStartingIndex;
				foreach (var colorBand in bands)
				{
					var listViewItem = CreateListViewItem(_colorBandsView, idx, colorBand, showSectionLine: ParentIsFocused);

					ListViewItems.Insert(idx++, listViewItem);
				}

				idx = e.NewStartingIndex; // Math.Max(e.NewStartingIndex - 1, 0);

				Reindex(idx);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items

				Debug.WriteLine($"CbsListView is handling CollectionChanged: Remove. There are {e.OldItems?.Count ?? -1} old items.");

				var bands = e.OldItems?.Cast<ColorBand>() ?? new List<ColorBand>();

				var si = int.MaxValue;

				foreach (var colorBand in bands)
				{
					Debug.WriteLine($"CbsListView is Removing a ColorBand: {colorBand}");

					var lvi = ListViewItems.FirstOrDefault(x => x.ColorBand == colorBand);
					if (lvi != null)
					{
						lvi.TearDown();
						ListViewItems.Remove(lvi);

						if (lvi.ColorBandIndex < si) si = lvi.ColorBandIndex;
					}
				}

				Reindex(si);
			}
		}

		#endregion

		#region ColorBand Support

		private void UpdateCutoff(CbsSelectionLineMovedEventArgs e)
		{
			var cbView = _colorBandsView;

			if (cbView == null)
				return;

			var newCutoff = (int)Math.Round(e.NewCutoff / ContentScale.Width);

			var indexToUpdate = e.UpdatingPrevious ? e.ColorBandIndex + 1 : e.ColorBandIndex;

			var colorBandToUpdate = ListViewItems[indexToUpdate].ColorBand;

			//var colorBandToUpdateOld = GetColorBandAt(cbView, e.ColorBandIndex);
			//Debug.Assert(colorBandToUpdate == colorBandToUpdateOld, "Color Band Mismatch from CbsSelectionLineMovedEventArgs.");

			var prevMsg = e.UpdatingPrevious ? "PreviousCutoff" : "Cutoff";
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Updating {prevMsg} for operation: {e.Operation} at index: {indexToUpdate} with new {prevMsg}: {newCutoff}.");


			if (e.Operation == CbsSelectionLineDragOperation.Move)
			{
				if (!colorBandToUpdate.IsInEditMode)
				{
					colorBandToUpdate.BeginEdit();
				}

				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Complete)
			{
				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.EndEdit();
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Cancel)
			{
				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.CancelEdit();
			}
			else
			{
				throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		#endregion

		#region Selection Line Support

		private bool TryGetSelectionLine(Point hitPoint, List<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsListViewItem? cbsListViewItem)
		{
			cbsListViewItem = null;

			double smallestDist = int.MaxValue;

			for (var cbsLinePtr = 0; cbsLinePtr < cbsListViewItems.Count; cbsLinePtr++)
			{
				var cbsLine = cbsListViewItems[cbsLinePtr].CbsSelectionLine;

				var diffX = Math.Abs(hitPoint.X - cbsLine.SelectionLinePosition);
				if (diffX < smallestDist)
				{
					smallestDist = diffX;
					cbsListViewItem = cbsListViewItems[cbsLinePtr];
				}
			}

			return cbsListViewItem != null;
		}

		//private bool TryGetSelectionLine(Point hitPoint, List<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsListViewItem? cbsListViewItem)
		//{
		//	cbsListViewItem = null;

		//	var xPos = GetLineUnderMouse(hitPoint);

		//	if (!double.IsNaN(xPos))
		//	{
		//		for (var cbsLinePtr = 0; cbsLinePtr < cbsListViewItems.Count; cbsLinePtr++)
		//		{
		//			var cbsLine = cbsListViewItems[cbsLinePtr].CbsSelectionLine;

		//			var diffX = cbsLine.SelectionLinePosition - xPos;

		//			if (ScreenTypeHelper.IsDoubleNearZero(diffX))
		//			{
		//				Debug.Assert(cbsLine.ColorBandIndex == cbsLinePtr, "CbsLine.ColorBandIndex Mismatch.");
		//				cbsListViewItem = cbsListViewItems[cbsLinePtr];

		//				return true;
		//			}
		//		}
		//	}

		//	return false;
		//}

		//private double GetLineUnderMouse(Point hitPoint)
		//{
		//	_hitList.Clear();

		//	var hitArea = new EllipseGeometry(hitPoint, 3.0, 4.0);
		//	var hitTestParams = new GeometryHitTestParameters(hitArea);
		//	VisualTreeHelper.HitTest(_canvas, null, HitTestCallBack, hitTestParams);

		//	var result = double.NaN;
		//	double smallestDist = int.MaxValue;

		//	for (var i = 0; i < _hitList.Count; i++)
		//	{
		//		var item = _hitList[i];

		//		double itemXPos = int.MaxValue;

		//		if (item is Line line)
		//		{
		//			itemXPos = line.X1;
		//			var adjustedPos = itemXPos / ContentScale.Width;
		//			Debug.WriteLineIf(_useDetailedDebug, $"Got a hit for line at position: {itemXPos} / {adjustedPos}.");
		//		}
		//		//else
		//		//{
		//		//	if (item is Polygon p)
		//		//	{
		//		//		itemXPos = GetPolygonXPos(p);
		//		//		var adjustedPos = itemXPos / ContentScale.Width;
		//		//		Debug.WriteLineIf(_useDetailedDebug, $"Got a hit for Polygon at position: {itemXPos} / {adjustedPos}.");
		//		//	}
		//		//}

		//		var dist = Math.Abs(hitPoint.X - itemXPos);

		//		if (dist < smallestDist)
		//		{
		//			smallestDist = dist;
		//			result = itemXPos;
		//		}
		//	}

		//	return result;
		//}

		//private double GetPolygonXPos(Polygon polygon)
		//{
		//	var maxY = polygon.Points.Max(p => p.Y);

		//	var lowestPoint = polygon.Points.FirstOrDefault(p => p.Y == maxY);
		//	var result = lowestPoint == default ? double.NaN : lowestPoint.X;

		//	return result;

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
		//				//if (result.VisualHit is Shape s) _hitList.Add(s);
		//				return HitTestResultBehavior.Continue;

		//			case IntersectionDetail.FullyContains:
		//				//if (result.VisualHit is Shape ss) _hitList.Add(ss);
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

		private bool TryGetColorBandRectangle(Point hitPoint, IList<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsListViewItem? cbsListViewItem)
		{
			cbsListViewItem = null;

			for (var i = 0; i < cbsListViewItems.Count; i++)
			{
				var cbRectangle = cbsListViewItems[i].CbsRectangle;

				if (cbRectangle.ContainsPoint(hitPoint))
				{
					cbsListViewItem = cbsListViewItems[i];

					Debug.Assert(cbsListViewItem.CbsRectangle.ColorBandIndex == i, "CbsListViewItems ColorBandIndex Mismatch.");
					return true;
				}
			}

			return false;
		}

		private void HilightColorBandRectangle(int colorBandIndex, bool newState)
		{
			if (colorBandIndex >= 0 && colorBandIndex < ListViewItems.Count)
			{
				var cbr = ListViewItems[colorBandIndex].CbsRectangle;
				cbr.IsCurrent = newState;
			}
		}

		#endregion

		#region Private Methods

		private void DrawColorBands(ListCollectionView? listCollectionView, bool showSectionLines)
		{
			ClearListViewItems();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			var endPtr = listCollectionView.Count - 1;

			for (var colorBandIndex = 0; colorBandIndex <= endPtr; colorBandIndex++)
			{
				var colorBand = (ColorBand)listCollectionView.GetItemAt(colorBandIndex);
				var listViewItem = CreateListViewItem(listCollectionView, colorBandIndex, colorBand, showSectionLines);

				ListViewItems.Add(listViewItem);
			}
		}

		private CbsListViewItem CreateListViewItem(ListCollectionView listCollectionView, int colorBandIndex, ColorBand colorBand, bool showSectionLine)
		{
			var xPosition = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			var isCurrent = colorBandIndex == listCollectionView.CurrentPosition;
			var isColorBandSelected = false;
			var cbsRectangle = new CbsRectangle(colorBandIndex, isCurrent, isColorBandSelected, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend,
				_colorBandLayoutViewModel, _canvas, CbRectangleIsSelectedChanged, HandleContextMenuDisplayRequested);

			// Build the Selection Line
			var selectionLinePosition = colorBand.Cutoff;

			var isCutoffSelected = false;
			var isVisible = showSectionLine;

			var cbsSelectionLine = new CbsSelectionLine(colorBandIndex, isCutoffSelected, selectionLinePosition,
				_colorBandLayoutViewModel, _canvas, OffsetIsSelectedChanged, HandleContextMenuDisplayRequested);

			var listViewItem = new CbsListViewItem(colorBand, cbsRectangle, cbsSelectionLine);
			listViewItem.CbsSelectionLine.SelectionLineMoved += HandleSelectionLineMoved;

			return listViewItem;
		}

		private void CbRectangleIsSelectedChanged(int colorBandIndex, bool newIsSelectedValue, bool shiftKeyPressed, bool controlKeyPressed)
		{
			if (!(shiftKeyPressed || controlKeyPressed))
			{
				foreach (var lvItem in ListViewItems)
				{
					lvItem.SelectionType = ColorBandSelectionType.None;
				}
			}

			var cbsListViewItem = ListViewItems[colorBandIndex];

			cbsListViewItem.IsColorSelected = newIsSelectedValue;
		}

		private void OffsetIsSelectedChanged(int colorBandIndex, bool newIsSelectedValue, bool shiftKeyPressed, bool controlKeyPressed)
		{
			if (!(shiftKeyPressed || controlKeyPressed))
			{
				foreach (var lvItem in ListViewItems)
				{
					lvItem.SelectionType = ColorBandSelectionType.None;
				}
			}

			var cbsListViewItem = ListViewItems[colorBandIndex];

			cbsListViewItem.IsCutoffSelected = newIsSelectedValue;
		}

		private void HandleContextMenuDisplayRequested(int colorBandIndex, ColorBandSelectionType colorBandSelectionType)
		{
			var cbsListViewItem = ListViewItems[colorBandIndex];
			_displayContextMenu(cbsListViewItem, colorBandSelectionType);
		}

		private void ClearListViewItems()
		{
			//Debug.WriteLine($"Before remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");

			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbsSelectionLine.SelectionLineMoved -= HandleSelectionLineMoved;
				listViewItem.TearDown();
			}

			ListViewItems.Clear();

			//Debug.WriteLine($"After remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");
		}

		private void Reindex(int startingIndex)
		{
			for (var i = startingIndex; i < ListViewItems.Count; i++)
			{
				ListViewItems[i].ColorBandIndex = i;
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

		[Conditional("DEBUG")]
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

	internal class CbsListViewItem
	{
		private ColorBandSelectionType _selectionType;

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public CbsListViewItem(ColorBand colorBand, CbsRectangle cbsRectangle, CbsSelectionLine cbsSelectionLine)
		{
			ColorBand = colorBand;
			CbsRectangle = cbsRectangle;
			CbsSelectionLine = cbsSelectionLine;

			_selectionType = 0;

			ColorBand.PropertyChanged += ColorBand_PropertyChanged;
		}

		#endregion

		#region Public Properties

		public ColorBand ColorBand { get; init; }
		public bool IsFirst => ColorBand.IsFirst;
		public bool IsLast => ColorBand.IsLast;

		public CbsRectangle CbsRectangle { get; init; }
		public CbsSelectionLine CbsSelectionLine { get; init; }

		public double SelectionLinePosition => CbsSelectionLine.SelectionLinePosition;

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
			get => ColorBand.IsCutoffSelected;
			set
			{
				if (value != ColorBand.IsCutoffSelected)
				{
					ColorBand.IsCutoffSelected = value;

					CbsSelectionLine.IsSelected = value;

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
			get => ColorBand.IsColorSelected;
			set
			{
				if (value != ColorBand.IsColorSelected)
				{
					ColorBand.IsColorSelected = value;

					CbsRectangle.IsSelected = value;

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

		public int ColorBandIndex
		{
			get => CbsRectangle.ColorBandIndex;
			set
			{
				CbsRectangle.ColorBandIndex = value;
				CbsSelectionLine.ColorBandIndex = value;
			}
		}

		#endregion

		#region Event Handlers

		private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{

			if (sender is ColorBand cb)
			{
				//Debug.WriteLine($"CbsListView is handling a ColorBand {e.PropertyName} Change for CbsRectangle at Index: {ColorBandIndex}.");

				if (e.PropertyName == "Cutoff")
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsListView is handling a ColorBand {e.PropertyName} Change for CbsRectangle at Index: {ColorBandIndex}.");

					// This ColorBand had its Cutoff updated.
					CbsRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				}
				else
				{
					if (e.PropertyName == "PreviousCutoff")
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbsListView is handling a ColorBand {e.PropertyName} Change for CbsRectangle at Index: {ColorBandIndex}.");

						// The ColorBand preceeding this one had its Cutoff updated.
						// This ColorBand had its PreviousCutoff (aka XPosition) updated.
						// This ColorBand's Starting Position (aka XPosition) and Width should be updated to accomodate.
						CbsRectangle.XPosition = cb.PreviousCutoff ?? 0;
						CbsRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void TearDown()
		{
			ColorBand.PropertyChanged -= ColorBand_PropertyChanged;
			CbsRectangle.TearDown();
			CbsSelectionLine.TearDown();
		}

		#endregion
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
