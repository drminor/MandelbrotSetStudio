using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
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
	internal class CbListView
	{
		#region Private Fields

		private ContextMenuDisplayRequest _displayContextMenu;

		private Action<ColorBandSetEditMode> _currentCbEditModeChanged;

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		private int _currentColorBandIndex;

		//private List<Shape> _hitList;

		private CbSectionLine? _sectionLineBeingDragged;

		private CbListViewItem? _sectionLineUnderMouse;
		private CbListViewItem? _itemUnderMouse;

		private int? _selectedItemsRangeAnchorIndex;
		private int? _indexOfMostRecentlySelectedItem;

		private ColorBandSetEditMode _currentCbEditMode;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListView(Canvas canvas, ListCollectionView colorBandsView, double controlHeight, SizeDbl contentScale, bool useRealTimePreview, bool parentIsFocused, ColorBandSetEditMode currentCbEditMode, 
			ContextMenuDisplayRequest displayContextMenu, Action<ColorBandSetEditMode> currentCbEditModeChanged)
		{
			_canvas = canvas;
			_colorBandsView = colorBandsView;

			_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBandsView_CollectionChanged;

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(contentScale, controlHeight, parentIsFocused);

			UseRealTimePreview = useRealTimePreview;
			_displayContextMenu = displayContextMenu;
			_currentCbEditModeChanged = currentCbEditModeChanged;

			ListViewItems = new List<CbListViewItem>();

			_selectedItemsRangeAnchorIndex = null;
			_indexOfMostRecentlySelectedItem = null;
			_currentCbEditMode = currentCbEditMode;

			//_hitList = new List<Shape>();

			DrawColorBands(_colorBandsView, showSectionLines: ParentIsFocused);

			_canvas.MouseMove += Handle_MouseMove;
			_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;
		}

		#endregion

		#region Public Properties

		public List<CbListViewItem> ListViewItems { get; set; }
		
		public bool IsEmpty => ListViewItems.Count == 0;

		public int CurrentColorBandIndex
		{
			get => _currentColorBandIndex;
			set
			{
				//if ( !(_colorBandsView.IsCurrentBeforeFirst || _colorBandsView.IsCurrentAfterLast) )
				//{
				//	//UpdateCbRectangleIsCurrent(_currentColorBandIndex, newState: false);
				//}

				if (_currentColorBandIndex >= 0 && _currentColorBandIndex < ListViewItems.Count)
				{
					ListViewItems[_currentColorBandIndex].IsCurrent = false;

				}

				_currentColorBandIndex = value;

				if (!(_colorBandsView.IsCurrentBeforeFirst || _colorBandsView.IsCurrentAfterLast))
				{
					//UpdateCbRectangleIsCurrent(_currentColorBandIndex, newState: true);
					ListViewItems[_currentColorBandIndex].IsCurrent = true;
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

		public bool IsDragSectionLineInProgress => _sectionLineBeingDragged != null && _sectionLineBeingDragged.DragState != DragState.None;

		public bool ParentIsFocused
		{
			get => _colorBandLayoutViewModel.ParentIsFocused;
			set => _colorBandLayoutViewModel.ParentIsFocused = value;
		}

		public ColorBandSetEditMode CurrentCbEditMode
		{
			get => _currentCbEditMode;
			set
			{
				if (value != _currentCbEditMode)
				{
					_currentCbEditMode = value;
					_currentCbEditModeChanged(value);

					UpdateListViewItemsWithNewSelectionType(value);
				}
			}
		}

		private CbListViewItem? ItemUnderMouse
		{
			get => _itemUnderMouse;
			set
			{
				if (value != _itemUnderMouse)
				{
					if (_itemUnderMouse != null) _itemUnderMouse.CbRectangle.IsUnderMouse = false;

					_itemUnderMouse = value;

					if (SectionLineUnderMouse == null)
					{
						if (_itemUnderMouse != null)
						{
							_itemUnderMouse.CbRectangle.IsUnderMouse = true;
						}

						Debug.WriteLine($"The Mouse is now over Rectangle: {ItemUnderMouse?.ColorBandIndex ?? -1}.");
					}
				}
			}
		}

		private CbListViewItem? SectionLineUnderMouse
		{
			get => _sectionLineUnderMouse;
			set
			{
				if (value != _sectionLineUnderMouse)
				{
					if (_sectionLineUnderMouse != null) _sectionLineUnderMouse.CbSectionLine.IsUnderMouse = false;

					_sectionLineUnderMouse = value;

					if (_sectionLineUnderMouse != null)
					{
						_sectionLineUnderMouse.CbSectionLine.IsUnderMouse = true;
						Debug.WriteLine($"The Mouse is now over SectionLine: {_sectionLineUnderMouse.ColorBandIndex}.");
					}
					else
					{
						if (_itemUnderMouse != null)
						{
							_itemUnderMouse.CbRectangle.IsUnderMouse = true;
							Debug.WriteLine($"The Mouse is now over Rectangle: {_itemUnderMouse.ColorBandIndex}.");
						}
					}

				}
			}
		}

		#endregion

		#region Public Methods

		// User pressed the Left Arrow or Right Arrow key.
		public void SelectedIndexWasMoved(int newColorBandIndex, int direction)
		{
			ItemUnderMouse = null;

			var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			if (shiftKeyPressed)
			{
				var numberSelected = GetSelectedItems().Count;

				if (numberSelected == 0)
				{
					Debug.Assert(_selectedItemsRangeAnchorIndex == null, "NumberSelected = 0 but RangeAnchor is not null.");

					var formerIndex = newColorBandIndex - direction;

					// Make the previously visted item the anchor.
					_selectedItemsRangeAnchorIndex = formerIndex;

					// Select the previously visited item.
					ListViewItems[formerIndex].SelectionType = ColorBandSelectionType.Band;

					// Select the newly visited item.
					ListViewItems[newColorBandIndex].SelectionType = ColorBandSelectionType.Band;
				}
				else
				{
					if (newColorBandIndex == _selectedItemsRangeAnchorIndex)
					{
						// Returning to the anchor.
						ResetAllIsSelected();
					}
					else
					{
						if (_selectedItemsRangeAnchorIndex == null)
						{
							_selectedItemsRangeAnchorIndex = _indexOfMostRecentlySelectedItem;
						}

						if (_selectedItemsRangeAnchorIndex != null)
						{
							SetItemsInSelectedRange(newColorBandIndex, _selectedItemsRangeAnchorIndex.Value, CurrentCbEditMode);
						}
					}
				}
			}
			else if (controlKeyPressed)
			{
				// The selection is not changed.
				// The IsCurrent property is updated as the CurrentColorBandIndex property is updated.
			}
			else
			{
				ResetAllIsSelected();
			}

			_indexOfMostRecentlySelectedItem = newColorBandIndex;
		}

		public (CbListViewItem, ColorBandSelectionType)? ItemAtMousePosition(Point hitPoint)
		{
			if (TryGetSectionLine(hitPoint, ListViewItems, out var distance, out var cbListViewItem) && distance < 4)
			{
				return (cbListViewItem, ColorBandSelectionType.Cutoff);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbListViewItem))
				{
					return (cbListViewItem, ColorBandSelectionType.Band);
				}
			}

			return null;
		}

		public void ClearSelectedItems()
		{
			// TODO: Set each CbRectangle's and each CbSectionLine's IsSelected to false.
		}

		public bool CancelDrag()
		{
			if (_sectionLineBeingDragged != null)
			{
				_sectionLineBeingDragged.CancelDrag();
				_sectionLineBeingDragged = null;

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

		private void Handle_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsDragSectionLineInProgress)
			{
				//Debug.WriteLine("WARNING: CbListView is receiving a MouseMove event, as a Drag SectionLine is in progress.");
				return;
			}

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSectionLine(hitPoint, ListViewItems, out var distance, out var cbListViewItem) && distance < 5)
			{
				SectionLineUnderMouse = cbListViewItem;

				// Positive if the mouse is to the right of the selection line, negative if to the left.
				var hitPointDistance = hitPoint.X - cbListViewItem.SectionLinePosition;

				// True if we will be updating the Current ColorBand's PreviousCutoff value, false if updating the Cutoff
				bool updatingPrevious = hitPointDistance > 0;

				if (updatingPrevious)
				{
					var index = cbListViewItem.ColorBandIndex;
					ItemUnderMouse = ListViewItems[index + 1];
				}
				else
				{
					ItemUnderMouse = cbListViewItem;
				}

				//e.Handled = true;
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbListViewItem))
				{
					SectionLineUnderMouse = null;
					ItemUnderMouse = cbListViewItem;
					//e.Handled = true;
				}
			}
		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling PreviewMouseLeftButtonDown. ContentScale is {ContentScale}.");

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSectionLine(hitPoint, ListViewItems, out var distance, out var cbListViewItem) && distance < 4)
			{
				e.Handled = true;

				//Debug.WriteLine($"Got SectionLine. SelLineIndex: {cbListViewItem.ColorBandIndex}.");
				StartDrag(cbListViewItem, hitPoint);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbListViewItem))
				{
					//e.Handled = true;

					var cb = cbListViewItem.ColorBand;

					//var cbOld = GetColorBandAt(cbsView, cbRectangleIndex.Value);
					//Debug.Assert(cb == cbOld, "ColorBand MisMatch Rectangle.");

					Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling PreviewMouseLeftButtonDown. Moving Current to CbRectangle: {cbListViewItem.ColorBandIndex}");

					_colorBandsView.MoveCurrentTo(cb);
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling PreviewMouseLeftButtonDown. No SectionLine or Rectangle found.");
				}
			}
		}

		private void StartDrag(CbListViewItem cbListViewItem, Point hitPoint)
		{
			var colorBandIndex = cbListViewItem.ColorBandIndex;

			if (cbListViewItem.IsLast)
			{
				// Cannot change the position of the last Selection Line.
				return;
			}

			// Positive if the mouse is to the right of the selection line, negative if to the left.
			var hitPointDistance = hitPoint.X - cbListViewItem.SectionLinePosition;

			// True if we will be updating the Current ColorBand's PreviousCutoff value, false if updating the Cutoff
			bool updatingPrevious = hitPointDistance > 0;

			var indexForCurrentItem = updatingPrevious ? colorBandIndex + 1 : colorBandIndex;
			_colorBandsView?.MoveCurrentToPosition(indexForCurrentItem);

			var cbSectionLine = cbListViewItem.CbSectionLine;
			_sectionLineBeingDragged = cbSectionLine;

			//var prevMsg = updatingPrevious ? "Previous" : "Current";
			//Debug.WriteIf(_useDetailedDebug, $"CbListView. Moving Current To CbRectangle {indexForCurrentItem} and starting Drag for SectionLine: {colorBandIndex}.");
			Debug.WriteIf(_useDetailedDebug, $"CbListView. Moving Current To CbRectangle {indexForCurrentItem} and starting Drag for SectionLine: {colorBandIndex}.");

			ReportColorBandRectanglesInPlay(ListViewItems, colorBandIndex, indexForCurrentItem);

			var gLeft = ListViewItems[colorBandIndex].CbRectangle.RectangleGeometry;
			var gRight = ListViewItems[colorBandIndex + 1].CbRectangle.RectangleGeometry;

			cbSectionLine.StartDrag(gLeft.Rect.Width, gRight.Rect.Width, updatingPrevious);
		}

		private void HandleSectionLineMoved(object? sender, CbSectionLineMovedEventArgs e)
		{
			CheckSectionLineBeingMoved(sender, e);

			if (e.Operation == CbSectionLineDragOperation.NotStarted)
			{
				Debug.WriteIf(_useDetailedDebug, $"CbListView. Drag not started. CbRectangle: {CurrentColorBandIndex} is now current.");

				_sectionLineBeingDragged = null;
				return;
			}

			switch (e.Operation)
			{
				case CbSectionLineDragOperation.Move:
					UpdateCutoff(e);
					break;

				case CbSectionLineDragOperation.Complete:
					_sectionLineBeingDragged = null;

					Debug.WriteLineIf(_useDetailedDebug, "Completing the SectionLine Drag Operation.");
					UpdateCutoff(e);
					break;

				case CbSectionLineDragOperation.Cancel:
					_sectionLineBeingDragged = null;

					UpdateCutoff(e);
					break;

				default:
					throw new InvalidOperationException($"The {e.Operation} CbSectionLineDragOperation is not supported.");
			}
		}

		private void CheckSectionLineBeingMoved(object? sender, CbSectionLineMovedEventArgs e)
		{
			if (_sectionLineBeingDragged == null)
			{
				Debug.WriteLine("WARNING: _selectionLineBeingDragged is null on HandleSectionLineMoved.");
				return;
			}

			if (!(sender is CbSectionLine))
			{
				throw new InvalidOperationException("The HandleSectionLineMoved event is being raised by some class other than CbSectionLine.");
			}
			else
			{
				if (sender != _sectionLineBeingDragged)
				{
					Debug.WriteLine("WARNING: HandleSectionLineMoved is being raised by a SectionLine other than the one that is being dragged.");
				}
			}

			if (e.Operation == CbSectionLineDragOperation.NotStarted)
			{
				return;
			}

			if (_sectionLineBeingDragged.DragState != DragState.InProcess)
			{
				Debug.WriteLine($"WARNING: The _selectionLineBeingDragged's DragState is {_sectionLineBeingDragged.DragState}, exepcting: {DragState.InProcess}.");
			}

			if (e.NewCutoff == 0 && !e.UpdatingPrevious)
			{
				Debug.WriteLine($"WARNING: Setting the Cutoff to zero for ColorBandIndex: {e.ColorBandIndex}.");
			}
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			CurrentColorBandIndex = _colorBandsView.CurrentPosition;
		}

		private void ColorBandsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbListView::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Debug.WriteLine($"CbListView is CollectionChanged: Reset.");
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items

				Debug.WriteLine($"CbListView is handling CollectionChanged: Add. There are {e.NewItems?.Count ?? -1} new items.");

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

				Debug.WriteLine($"CbListView is handling CollectionChanged: Remove. There are {e.OldItems?.Count ?? -1} old items.");

				var bands = e.OldItems?.Cast<ColorBand>() ?? new List<ColorBand>();

				var si = int.MaxValue;

				foreach (var colorBand in bands)
				{
					Debug.WriteLine($"CbListView is Removing a ColorBand: {colorBand}");

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

		#region Private Methods

		private bool TryGetSectionLine(Point hitPoint, List<CbListViewItem> cbListViewItems, [NotNullWhen(true)] out double? distance, [NotNullWhen(true)] out CbListViewItem? cbListViewItem)
		{
			cbListViewItem = null;

			double smallestDist = int.MaxValue;

			for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
			{
				var cbsLine = cbListViewItems[cbLinePtr].CbSectionLine;

				var diffX = Math.Abs(hitPoint.X - cbsLine.SectionLinePosition);
				if (diffX < smallestDist)
				{
					smallestDist = diffX;
					cbListViewItem = cbListViewItems[cbLinePtr];
				}
			}

			distance = smallestDist == int.MaxValue ? null : smallestDist;


			return cbListViewItem != null;
		}

		private bool TryGetColorBandRectangle(Point hitPoint, IList<CbListViewItem> cbListViewItems, [NotNullWhen(true)] out CbListViewItem? cbListViewItem)
		{
			cbListViewItem = null;

			for (var i = 0; i < cbListViewItems.Count; i++)
			{
				var cbRectangle = cbListViewItems[i].CbRectangle;

				if (cbRectangle.ContainsPoint(hitPoint))
				{
					cbListViewItem = cbListViewItems[i];

					Debug.Assert(cbListViewItem.CbRectangle.ColorBandIndex == i, "CbListViewItems ColorBandIndex Mismatch.");
					return true;
				}
			}

			return false;
		}

		private void DrawColorBands(ListCollectionView? listCollectionView, bool showSectionLines)
		{
			ClearListViewItems();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			for (var colorBandIndex = 0; colorBandIndex < listCollectionView.Count; colorBandIndex++)
			{
				var colorBand = (ColorBand)listCollectionView.GetItemAt(colorBandIndex);
				var listViewItem = CreateListViewItem(listCollectionView, colorBandIndex, colorBand, showSectionLines);

				ListViewItems.Add(listViewItem);
			}
		}

		private CbListViewItem CreateListViewItem(ListCollectionView listCollectionView, int colorBandIndex, ColorBand colorBand, bool showSectionLine)
		{
			var xPosition = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			var isCurrent = colorBandIndex == listCollectionView.CurrentPosition;
			var isColorBandSelected = false;
			var cbRectangle = new CbRectangle(colorBandIndex, isCurrent, isColorBandSelected, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend,
				_colorBandLayoutViewModel, _canvas, UpdateCbRectangleIsSelected, HandleContextMenuDisplayRequested);

			// Build the Selection Line
			var selectionLinePosition = colorBand.Cutoff;

			var isCutoffSelected = false;
			var isVisible = showSectionLine;

			var CbSectionLine = new CbSectionLine(colorBandIndex, isCutoffSelected, selectionLinePosition,
				_colorBandLayoutViewModel, _canvas, UpdateCbRectangleIsSelected, HandleContextMenuDisplayRequested);

			var listViewItem = new CbListViewItem(colorBand, cbRectangle, CbSectionLine);
			listViewItem.CbSectionLine.SectionLineMoved += HandleSectionLineMoved;

			return listViewItem;
		}

		// The user has pressed the left mouse-button while over a Rectangle or SectionLine.
		private void UpdateCbRectangleIsSelected(int colorBandIndex, ColorBandSetEditMode colorBandEditMode)
		{
			var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			if (shiftKeyPressed)
			{
				if (_selectedItemsRangeAnchorIndex == null)
				{
					_selectedItemsRangeAnchorIndex = _indexOfMostRecentlySelectedItem;
				}

				if (_selectedItemsRangeAnchorIndex != null)
				{
					SetItemsInSelectedRange(colorBandIndex, _selectedItemsRangeAnchorIndex.Value, colorBandEditMode);
				}
			}
			else if (controlKeyPressed)
			{
				var cbListViewItem = ListViewItems[colorBandIndex];

				_ = Toggle(cbListViewItem, colorBandEditMode);

				_selectedItemsRangeAnchorIndex = null;
			}
			else
			{
				if (GetSelectedItems().Count > 0)
				{
					ResetAllIsSelected();
				}

				var cbListViewItem = ListViewItems[colorBandIndex];

				Toggle(cbListViewItem, colorBandEditMode);
			}

			_indexOfMostRecentlySelectedItem = colorBandIndex;
			CurrentCbEditMode = colorBandEditMode;
		}

		private void SetItemsInSelectedRange(int startIndex, int endIndex, ColorBandSetEditMode editMode)
		{
			var selectionType = ColorBandSetViewHelper.GetSelectionType(editMode);


			if (startIndex > endIndex)
			{
				for (var i = 0; i < ListViewItems.Count; i++)
				{
					if (i >= endIndex && i <= startIndex)
					{
						ListViewItems[i].SelectionType = selectionType;
					}
					else
					{
						ListViewItems[i].SelectionType = ColorBandSelectionType.None;
					}
				}
			}
			else
			{
				for (var i = 0; i < ListViewItems.Count; i++)
				{
					if (i >= startIndex && i <= endIndex)
					{
						ListViewItems[i].SelectionType = selectionType;
					}
					else
					{
						ListViewItems[i].SelectionType = ColorBandSelectionType.None;
					}
				}
			}
		}

		private ColorBandSelectionType Toggle(CbListViewItem cbListViewItem, ColorBandSetEditMode editMode)
		{
			if (editMode == ColorBandSetEditMode.Bands)
			{
				cbListViewItem.IsColorBandSelected = !cbListViewItem.IsColorBandSelected;
			}
			else if (editMode == ColorBandSetEditMode.Cutoffs)
			{
				cbListViewItem.IsCutoffSelected = !cbListViewItem.IsCutoffSelected;
			}
			else if (editMode == ColorBandSetEditMode.Colors)
			{
				cbListViewItem.IsColorSelected = !cbListViewItem.IsColorSelected;
			}

			return cbListViewItem.SelectionType;
		}

		private void UpdateListViewItemsWithNewSelectionType(ColorBandSetEditMode editMode)
		{
			var selectionType = ColorBandSetViewHelper.GetSelectionType(editMode);

			var currentlySelected = ListViewItems.Where(x => x.IsItemSelected);

			foreach(var item in currentlySelected)
			{
				item.SelectionType = selectionType;
			}
		}

		private void HandleContextMenuDisplayRequested(int colorBandIndex, ColorBandSetEditMode editMode)
		{
			var cbListViewItem = ListViewItems[colorBandIndex];
			_displayContextMenu(cbListViewItem, editMode);
		}

		//private void UpdateCbRectangleIsCurrent(int colorBandIndex, bool newState)
		//{
		//	if (colorBandIndex >= 0 && colorBandIndex < ListViewItems.Count)
		//	{
		//		var cbr = ListViewItems[colorBandIndex].CbRectangle;
		//		cbr.IsCurrent = newState;
		//	}
		//}

		private void ClearListViewItems()
		{
			//Debug.WriteLine($"Before remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");

			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbSectionLine.SectionLineMoved -= HandleSectionLineMoved;
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

		private void ResetAllIsSelected()
		{
			foreach (var lvItem in ListViewItems)
			{
				lvItem.SelectionType = ColorBandSelectionType.None;
			}

			_selectedItemsRangeAnchorIndex = null;
		}

		private List<CbListViewItem> GetSelectedItems()
		{
			var result = ListViewItems.Where(x => x.IsItemSelected).ToList();
			return result;
		}

		private void UpdateCutoff(CbSectionLineMovedEventArgs e)
		{
			var cbView = _colorBandsView;

			if (cbView == null)
				return;

			var newCutoff = (int)Math.Round(e.NewCutoff / ContentScale.Width);

			var indexToUpdate = e.UpdatingPrevious ? e.ColorBandIndex + 1 : e.ColorBandIndex;

			var colorBandToUpdate = ListViewItems[indexToUpdate].ColorBand;

			//var colorBandToUpdateOld = GetColorBandAt(cbView, e.ColorBandIndex);
			//Debug.Assert(colorBandToUpdate == colorBandToUpdateOld, "Color Band Mismatch from CbSectionLineMovedEventArgs.");

			var prevMsg = e.UpdatingPrevious ? "PreviousCutoff" : "Cutoff";
			Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Updating {prevMsg} for operation: {e.Operation} at index: {indexToUpdate} with new {prevMsg}: {newCutoff}.");


			if (e.Operation == CbSectionLineDragOperation.Move)
			{
				if (!colorBandToUpdate.IsInEditMode)
				{
					colorBandToUpdate.BeginEdit();
				}

				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
			}
			else if (e.Operation == CbSectionLineDragOperation.Complete)
			{
				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.EndEdit();
			}
			else if (e.Operation == CbSectionLineDragOperation.Cancel)
			{
				if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.CancelEdit();
			}
			else
			{
				throw new InvalidOperationException($"The {e.Operation} CbSectionLineDragOperation is not supported.");
			}
		}

		#endregion

		#region Diagnostics

		[Conditional("DEGUG2")]
		private void ReportColorBandRectanglesInPlay(List<CbListViewItem> listViewItems, int currentColorBandIndex, int sectionLineIndex)
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Rectangles in Play for sectionLine Drag operation with SectionLineIndex: {sectionLineIndex}.");

			var cbRectangleLeft = listViewItems[sectionLineIndex].CbRectangle;
			sb.AppendLine($"cbRectangleLeft at index {sectionLineIndex}: {cbRectangleLeft.RectangleGeometry}");

			var cbRectangleRight = listViewItems[sectionLineIndex + 1].CbRectangle;
			sb.AppendLine($"cbRectangleRight at index {sectionLineIndex + 1}: {cbRectangleRight.RectangleGeometry}");

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

		#region Unused HitTest Logic

		//private bool TryGetSectionLine(Point hitPoint, List<CbListViewItem> cbListViewItems, [NotNullWhen(true)] out CbListViewItem? cbListViewItem)
		//{
		//	cbListViewItem = null;

		//	var xPos = GetLineUnderMouse(hitPoint);

		//	if (!double.IsNaN(xPos))
		//	{
		//		for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
		//		{
		//			var cbLine = cbListViewItems[cbLinePtr].CbectionLine;

		//			var diffX = cbLine.SectionLinePosition - xPos;

		//			if (ScreenTypeHelper.IsDoubleNearZero(diffX))
		//			{
		//				Debug.Assert(cbLine.ColorBandIndex == cbLinePtr, "CbLine.ColorBandIndex Mismatch.");
		//				cbListViewItem = cbListViewItems[cbLinePtr];

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
	}

	internal class CbListViewItem
	{
		private ColorBandSelectionType _selectionType;

		private readonly bool _useDetailedDebug = false;

		#region Constructor
		public CbListViewItem(ColorBand colorBand, CbRectangle cbRectangle, CbSectionLine cbSectionLine)
		{
			ColorBand = colorBand;
			CbRectangle = cbRectangle;
			CbSectionLine = cbSectionLine;

			_selectionType = 0;

			ColorBand.PropertyChanged += ColorBand_PropertyChanged;
		}

		#endregion

		#region Public Properties

		public ColorBand ColorBand { get; init; }
		public bool IsFirst => ColorBand.IsFirst;
		public bool IsLast => ColorBand.IsLast;

		public CbRectangle CbRectangle { get; init; }
		public CbSectionLine CbSectionLine { get; init; }

		public int ColorBandIndex
		{
			get => CbRectangle.ColorBandIndex;
			set
			{
				CbRectangle.ColorBandIndex = value;
				CbSectionLine.ColorBandIndex = value;
			}
		}

		public double SectionLinePosition => CbSectionLine.SectionLinePosition;

		public bool IsCurrent
		{
			get => CbRectangle.IsCurrent;
			set => CbRectangle.IsCurrent = value;
		}

		public ColorBandSelectionType SelectionType
		{
			get => _selectionType;

			set
			{
				if (value != _selectionType)
				{
					_selectionType = value;

					IsCutoffSelected = _selectionType.HasFlag(ColorBandSelectionType.Cutoff);
					IsColorSelected = _selectionType.HasFlag(ColorBandSelectionType.Color);
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

					CbSectionLine.IsSelected = value;

					if (value)
					{
						_selectionType |= ColorBandSelectionType.Cutoff;
					}
					else
					{
						_selectionType &= ~ColorBandSelectionType.Cutoff;
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

					CbRectangle.IsSelected = value;

					if (value)
					{
						_selectionType |= ColorBandSelectionType.Color;
					}
					else
					{
						_selectionType &= ~ColorBandSelectionType.Color;
					}
				}
			}
		}

		public bool IsColorBandSelected
		{
			get => IsCutoffSelected && IsColorSelected;
			set
			{
				IsCutoffSelected = value;
				IsColorSelected = value;
			}
		}

		public bool IsItemSelected => IsCutoffSelected | IsColorSelected;

		#endregion

		#region Event Handlers

		private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is ColorBand cb)
			{
				//Debug.WriteLine($"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

				if (e.PropertyName == "Cutoff")
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

					// This ColorBand had its Cutoff updated.

					// This also updates the cutoff
					CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				}
				else
				{
					if (e.PropertyName == "PreviousCutoff")
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling a ColorBand {e.PropertyName} Change for CbRectangle at Index: {ColorBandIndex}.");

						// The ColorBand preceeding this one had its Cutoff updated.
						// This ColorBand had its PreviousCutoff (aka XPosition) updated.
						// This ColorBand's Starting Position (aka XPosition) and Width should be updated to accomodate.
						//CbRectangle.XPosition = cb.PreviousCutoff ?? 0;
						//CbRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);

						// This also updates the width
						CbRectangle.XPosition = cb.PreviousCutoff ?? 0;

					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void TearDown()
		{
			ColorBand.PropertyChanged -= ColorBand_PropertyChanged;
			CbRectangle.TearDown();
			CbSectionLine.TearDown();
		}

		#endregion
	}


}
