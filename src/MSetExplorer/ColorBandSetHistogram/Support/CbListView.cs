using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WinRT;

namespace MSetExplorer
{
	internal class CbListView
	{
		#region Private Fields
		
		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private ColorBandSetEditMode _currentCbEditMode;
		private ContextMenuDisplayRequest _displayContextMenu;
		private Action<ColorBandSetEditMode> _currentCbEditModeChanged;

		private int _currentColorBandIndex;

		//private List<Shape> _hitList;

		private CbSectionLine? _sectionLineBeingDragged;

		private CbListViewItem? _sectionLineUnderMouse;
		private CbListViewItem? _blendRectangleUnderMouse;
		private CbListViewItem? _colorBlocksUnderMouse;


		private int? _selectedItemsRangeAnchorIndex;
		private int? _indexOfMostRecentlySelectedItem;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListView(Canvas canvas, ListCollectionView colorBandsView, double controlHeight, SizeDbl contentScale, bool parentIsFocused, ColorBandSetEditMode currentCbEditMode, 
			ContextMenuDisplayRequest displayContextMenu, Action<ColorBandSetEditMode> currentCbEditModeChanged)
		{
			_canvas = canvas;
			_colorBandsView = colorBandsView;

			_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBandsView_CollectionChanged;

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(contentScale, controlHeight, parentIsFocused, _canvas, ListViewItemSelectedChanged, HandleContextMenuDisplayRequested);

			_currentCbEditMode = currentCbEditMode;
			_displayContextMenu = displayContextMenu;
			_currentCbEditModeChanged = currentCbEditModeChanged;

			ListViewItems = new List<CbListViewItem>();
			_currentColorBandIndex = 0;

			_sectionLineBeingDragged = null;
			_sectionLineUnderMouse = null;
			_blendRectangleUnderMouse = null;
			_selectedItemsRangeAnchorIndex = null;
			_indexOfMostRecentlySelectedItem = null;

			//_hitList = new List<Shape>();

			DrawColorBands(_colorBandsView);

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
				if (_currentColorBandIndex >= 0 && _currentColorBandIndex < ListViewItems.Count)
				{
					ListViewItems[_currentColorBandIndex].IsCurrent = false;

				}

				_currentColorBandIndex = value;

				if (!(_colorBandsView.IsCurrentBeforeFirst || _colorBandsView.IsCurrentAfterLast))
				{
					ListViewItems[_currentColorBandIndex].IsCurrent = true;
				}
			}
		}

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

		public double CbrElevation => _colorBandLayoutViewModel.SectionLinesHeight;

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
					UpdateListViewItemsWithNewSelectionType(value);
					_currentCbEditMode = value;
					_currentCbEditModeChanged(value);

					if (BlendRectangleUnderMouse != null)
					{
						BlendRectangleUnderMouse.SetIsRectangleUnderMouse(true, CurrentCbEditMode);
					}
				}
			}
		}

		private CbListViewItem? BlendRectangleUnderMouse
		{
			get => _blendRectangleUnderMouse;
			set
			{
				if (value != _blendRectangleUnderMouse)
				{
					if (_blendRectangleUnderMouse != null)
					{
						_blendRectangleUnderMouse.SetIsRectangleUnderMouse(false, ColorBandSetEditMode.Bands);
					}

					_blendRectangleUnderMouse = value;

					//if (SectionLineUnderMouse == null)
					//{
					//	if (_itemUnderMouse != null)
					//	{
					//		_itemUnderMouse.CbRectangle.IsUnderMouse = true;
					//	}

					//	Debug.WriteLine($"The Mouse is now over Rectangle: {ItemUnderMouse?.ColorBandIndex ?? -1}.");
					//}

					if (_blendRectangleUnderMouse != null)
					{
						_blendRectangleUnderMouse.SetIsRectangleUnderMouse(true, ColorBandSetEditMode.Bands);
						Debug.WriteLine($"The Mouse is now over Rectangle: {_blendRectangleUnderMouse.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
					}
				}
			}
		}

		private CbListViewItem? ColorBlocksUnderMouse
		{
			get => _colorBlocksUnderMouse;
			set
			{
				if (value != _colorBlocksUnderMouse)
				{
					if (_colorBlocksUnderMouse != null)
					{
						_colorBlocksUnderMouse.SetIsRectangleUnderMouse(false, ColorBandSetEditMode.Colors);
					}

					_colorBlocksUnderMouse = value;

					//if (SectionLineUnderMouse == null)
					//{
					//	if (_itemUnderMouse != null)
					//	{
					//		_itemUnderMouse.CbRectangle.IsUnderMouse = true;
					//	}

					//	Debug.WriteLine($"The Mouse is now over Rectangle: {ItemUnderMouse?.ColorBandIndex ?? -1}.");
					//}

					if (_colorBlocksUnderMouse != null)
					{
						_colorBlocksUnderMouse.SetIsRectangleUnderMouse(true, ColorBandSetEditMode.Colors);
						Debug.WriteLine($"The Mouse is now over ColorBlocks: {_colorBlocksUnderMouse.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
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
					if (_sectionLineUnderMouse != null) _sectionLineUnderMouse.SectionLineIsUnderMouse = false;

					_sectionLineUnderMouse = value;

					if (_sectionLineUnderMouse != null)
					{
						_sectionLineUnderMouse.SectionLineIsUnderMouse = true;
						Debug.WriteLine($"The Mouse is now over SectionLine: {_sectionLineUnderMouse.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
					}
					//else
					//{
					//	if (_itemUnderMouse != null)
					//	{
					//		_itemUnderMouse.CbRectangle.IsUnderMouse = true;
					//		Debug.WriteLine($"The Mouse is now over Rectangle: {_itemUnderMouse.ColorBandIndex}.");
					//	}
					//}

				}
			}
		}

		#endregion

		#region Public Methods

		// User pressed the Left Arrow or Right Arrow key.
		public void SelectedIndexWasMoved(int newColorBandIndex, int direction)
		{
			var foundError = false;

			BlendRectangleUnderMouse = null;
			SectionLineUnderMouse = null;

			var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			if (shiftKeyPressed)
			{
				var numberSelected = GetSelectedItems().Count();

				if (numberSelected == 0)
				{
					//Debug.Assert(_selectedItemsRangeAnchorIndex == null, "NumberSelected = 0 but RangeAnchor is not null.");

					var formerIndex = newColorBandIndex - direction;

					if (_selectedItemsRangeAnchorIndex != null)
					{
						foundError = true;
						Debug.WriteLine($"There are zero items selected, but the RangeAnchorIndex = {_selectedItemsRangeAnchorIndex} The formerIndex = {formerIndex}. The new index = {newColorBandIndex}.");
					}

					// Make the previously visted item the anchor.
					_selectedItemsRangeAnchorIndex = formerIndex;

					var selectionType = ColorBandSetViewHelper.GetSelectionType(CurrentCbEditMode);

					// Select the previously visited item.
					ListViewItems[formerIndex].SelectionType = selectionType;

					// Select the newly visited item.
					ListViewItems[newColorBandIndex].SelectionType = selectionType;
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
						else
						{
							Debug.WriteLineIf(_useDetailedDebug, $"WARINING: CbListView. SelectionIndexWasMoved. Shift key was pressed. At least one item is selected but both the range anchor and the most recently selected item is null.");
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

			if (foundError)
			{
				Debug.WriteLine("Look at me.!");
			}
		}

		public (CbListViewItem, ColorBandSelectionType)? ItemAtMousePosition(Point hitPoint)
		{
			//if (TryGetSectionLine(hitPoint, ListViewItems, out var distance, out var cbListViewItem) && Math.Abs(distance.Value) < 4)
			//{
			//	return (cbListViewItem, ColorBandSelectionType.Cutoff);
			//}
			//else
			//{
			//	if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbListViewItem))
			//	{
			//		return (cbListViewItem, ColorBandSelectionType.Band);
			//	}
			//}

			//return null;


			if (TryGetSectionLineIndex(hitPoint, ListViewItems, out var distance, out var cbListViewItemIndex))
			{
				if (Math.Abs(distance) < 6)
				{
					var sectionLineUnderMouse = ListViewItems[cbListViewItemIndex];
					return (sectionLineUnderMouse, ColorBandSelectionType.Cutoff);
				}
				else
				{
					var selIndex = distance > 0 ? cbListViewItemIndex + 1 : cbListViewItemIndex;
					var itemUnderMouse = ListViewItems[selIndex];

					if (hitPoint.Y >= _colorBandLayoutViewModel.BlendRectangesElevation)
					{
						return (itemUnderMouse, ColorBandSelectionType.Band);
					}
					else
					{
						return (itemUnderMouse, ColorBandSelectionType.Color);
					}
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling MouseMove. No SectionLine or Rectangle found. The ListViewItems contains {ListViewItems.Count} items.");
				return null;
			}
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
			if (TryGetSectionLineIndex(hitPoint, ListViewItems, out var distance, out var cbListViewItemIndex))
			{
				if (Math.Abs(distance) < 6)
				{
					BlendRectangleUnderMouse = null;
					ColorBlocksUnderMouse = null;
					SectionLineUnderMouse = ListViewItems[cbListViewItemIndex];
					//e.Handled = true;
				}
				else
				{
					SectionLineUnderMouse = null;

					var selIndex = distance > 0 ? cbListViewItemIndex + 1 : cbListViewItemIndex;

					if (hitPoint.Y >= _colorBandLayoutViewModel.BlendRectangesElevation)
					{
						ColorBlocksUnderMouse = null;
						BlendRectangleUnderMouse = ListViewItems[selIndex];
					}
					else
					{
						BlendRectangleUnderMouse = null;
						ColorBlocksUnderMouse = ListViewItems[selIndex];
					}
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling MouseMove. No SectionLine or Rectangle found. The ListViewItems contains {ListViewItems.Count} items.");
			}
		}

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSectionLineIndex(hitPoint, ListViewItems, out var distance, out var cbListViewItemIndex))
			{
				if (Math.Abs(distance) < 6)
				{
					e.Handled = true;

					//Debug.WriteLine($"Got SectionLine. SelLineIndex: {cbListViewItem.ColorBandIndex}.");
					var cbListViewItem = ListViewItems[cbListViewItemIndex];
					StartDrag(cbListViewItem, hitPoint);
				}
				else
				{
					var selIndex = distance > 0 ? cbListViewItemIndex + 1 : cbListViewItemIndex;
					var cbListViewItem = ListViewItems[selIndex];

					Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling PreviewMouseLeftButtonDown. Moving Current to CbRectangle: {cbListViewItem.ColorBandIndex}");

					_colorBandsView.MoveCurrentTo(cbListViewItem.ColorBand);
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling PreviewMouseLeftButtonDown. No SectionLine or Rectangle found.");
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
					var listViewItem = CreateListViewItem(_colorBandsView, idx, colorBand);
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

		//private bool TryGetSectionLine(Point hitPoint, List<CbListViewItem> cbListViewItems, [NotNullWhen(true)] out double? distance, [NotNullWhen(true)] out CbListViewItem? cbListViewItem)
		//{
		//	cbListViewItem = null;

		//	double smallestDist = int.MaxValue;

		//	for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
		//	{
		//		var cbsLine = cbListViewItems[cbLinePtr].CbSectionLine;

		//		var diffX = Math.Abs(hitPoint.X - cbsLine.SectionLinePosition);
		//		if (diffX < smallestDist)
		//		{
		//			smallestDist = diffX;
		//			cbListViewItem = cbListViewItems[cbLinePtr];
		//		}
		//	}

		//	distance = cbListViewItem == null ? null : hitPoint.X - cbListViewItem.SectionLinePosition;

		//	return cbListViewItem != null;
		//}

		private bool TryGetSectionLineIndex(Point hitPoint, List<CbListViewItem> cbListViewItems, out double distance, out int listViewItemIndex)
		{
			listViewItemIndex = -1;

			double smallestAbsDist = int.MaxValue;
			double smallestDist = int.MaxValue;

			for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
			{
				var cbsLine = cbListViewItems[cbLinePtr].CbSectionLine;

				var dist = hitPoint.X - cbsLine.SectionLinePosition;
				var absDist = Math.Abs(dist);

				if (absDist < smallestAbsDist)
				{
					smallestAbsDist = absDist;
					smallestDist = dist;
					listViewItemIndex = cbLinePtr;
				}
			}

			distance = smallestDist;
			return listViewItemIndex != -1;
		}

		//private bool TryGetColorBandRectangle(Point hitPoint, IList<CbListViewItem> cbListViewItems, [NotNullWhen(true)] out CbListViewItem? cbListViewItem)
		//{
		//	cbListViewItem = null;

		//	for (var i = 0; i < cbListViewItems.Count; i++)
		//	{
		//		var cbRectangle = cbListViewItems[i].CbRectangle;

		//		if (cbRectangle.ContainsPoint(hitPoint))
		//		{
		//			cbListViewItem = cbListViewItems[i];

		//			Debug.Assert(cbListViewItem.CbRectangle.ColorBandIndex == i, "CbListViewItems ColorBandIndex Mismatch.");
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		private void DrawColorBands(ListCollectionView? listCollectionView)
		{
			ClearListViewItems();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			for (var colorBandIndex = 0; colorBandIndex < listCollectionView.Count; colorBandIndex++)
			{
				var colorBand = (ColorBand)listCollectionView.GetItemAt(colorBandIndex);
				var listViewItem = CreateListViewItem(listCollectionView, colorBandIndex, colorBand);

				ListViewItems.Add(listViewItem);
			}
		}

		private CbListViewItem CreateListViewItem(ListCollectionView listCollectionView, int colorBandIndex, ColorBand colorBand)
		{
			// Build the CbRectangle
			var xPosition = colorBand.PreviousCutoff ?? 0;
			var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
			var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

			var isCurrent = colorBandIndex == listCollectionView.CurrentPosition;
			var cbRectangle = new CbRectangle(colorBandIndex, isCurrent, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel);

			// Build the Selection Line
			var selectionLinePosition = colorBand.Cutoff;
			var cbSectionLine = new CbSectionLine(colorBandIndex, selectionLinePosition, _colorBandLayoutViewModel);

			// Build the Color Block
			var cbColorBlock = new CbColorBlock(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel);

			var listViewItem = new CbListViewItem(colorBand, cbRectangle, cbSectionLine, cbColorBlock);
			listViewItem.CbSectionLine.SectionLineMoved += HandleSectionLineMoved;

			return listViewItem;
		}

		// The user has pressed the left mouse-button while over a Rectangle or SectionLine.
		private void ListViewItemSelectedChanged(int colorBandIndex, ColorBandSetEditMode colorBandEditMode)
		{
			CurrentCbEditMode = colorBandEditMode;

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
				_ = Toggle(colorBandIndex, colorBandEditMode);

				_selectedItemsRangeAnchorIndex = null;
			}
			else
			{
				ResetAllIsSelected();

				_ = Toggle(colorBandIndex, colorBandEditMode);
			}

			_indexOfMostRecentlySelectedItem = colorBandIndex;
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

		private ColorBandSelectionType Toggle(int colorBandIndex, ColorBandSetEditMode editMode)
		{
			var cbListViewItem = ListViewItems[colorBandIndex];

			if (editMode == ColorBandSetEditMode.Bands)
			{
				cbListViewItem.SelectionType = cbListViewItem.IsBandSelected ? ColorBandSelectionType.None : ColorBandSelectionType.Band;
			}
			else if (editMode == ColorBandSetEditMode.Cutoffs)
			{
				cbListViewItem.SelectionType = cbListViewItem.IsCutoffSelected ? ColorBandSelectionType.None : ColorBandSelectionType.Cutoff;
			}
			else if (editMode == ColorBandSetEditMode.Colors)
			{
				cbListViewItem.SelectionType = cbListViewItem.IsColorSelected ? ColorBandSelectionType.None : ColorBandSelectionType.Color;
			}

			return cbListViewItem.SelectionType;
		}

		//private ColorBandSelectionType Select(int colorBandIndex, ColorBandSetEditMode editMode)
		//{
		//	var cbListViewItem = ListViewItems[colorBandIndex];

		//	if (editMode == ColorBandSetEditMode.Bands)
		//	{
		//		cbListViewItem.SelectionType = ColorBandSelectionType.Band;
		//	}
		//	else if (editMode == ColorBandSetEditMode.Cutoffs)
		//	{
		//		cbListViewItem.SelectionType = ColorBandSelectionType.Cutoff;
		//	}
		//	else if (editMode == ColorBandSetEditMode.Colors)
		//	{
		//		cbListViewItem.SelectionType = ColorBandSelectionType.Color;
		//	}

		//	return cbListViewItem.SelectionType;
		//}

		private void UpdateListViewItemsWithNewSelectionType(ColorBandSetEditMode editMode)
		{
			var selectionType = ColorBandSetViewHelper.GetSelectionType(editMode);

			var currentlySelected = GetSelectedItems();

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

		private IEnumerable<CbListViewItem> GetSelectedItems()
		{
			var result = ListViewItems.Where(x => x.IsItemSelected);
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
}
