using MSS.Types;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Windows.UI.WebUI;

namespace MSetExplorer
{


	internal class CbListView
	{
		#region Private Fields

		private StoryboardDetails _bandDeletionAnimation;
		private int _nextNameSuffix;

		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private ColorBandSetEditMode _currentCbEditMode;
		private ContextMenuDisplayRequest _displayContextMenu;
		private Action<ColorBandSetEditMode> _currentCbEditModeChanged;

		private int _currentColorBandIndex;

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

			if (NameScope.GetNameScope(_canvas) == null)
			{
				NameScope.SetNameScope(_canvas, new NameScope());
			}
			else
			{
				CheckNameScope(_canvas, 0);
			}

			_bandDeletionAnimation = new StoryboardDetails(new Storyboard(), _canvas, AfterAnimateDeletion/*, new DoubleAnimation(), "Width"*/);
			//_bandDeletionAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));

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

					if (ColorBlocksUnderMouse != null)
					{
						ColorBlocksUnderMouse.SetIsRectangleUnderMouse(true, CurrentCbEditMode);
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
					var listViewItem = CreateListViewItem(idx, colorBand);
					ListViewItems.Insert(idx++, listViewItem);
				}

				idx = e.NewStartingIndex; // Math.Max(e.NewStartingIndex - 1, 0);

				Reindex(idx);
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				Debug.WriteLine($"CbListView is handling CollectionChanged: Remove.");
				var colorBand = e.OldItems?[0] as ColorBand;
				CheckOldItems(colorBand, e.OldItems);

				var lvi = ListViewItems.FirstOrDefault(x => x.ColorBand == colorBand);
				if (lvi != null)
				{
					Debug.WriteLine($"CbListView is removing a ColorBand: {colorBand}");

					var idx = _colorBandsView.IndexOf(lvi.ColorBand);

					AnimateBandDeletion(lvi.ColorBandIndex);
				}
				else
				{
					Debug.WriteLine($"CbListView cannot find ColorBand: {colorBand} in the ListViewItems.");
				}
			}
		}

		#endregion

		#region Private Methods

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

		private void DrawColorBands(ListCollectionView? listCollectionView)
		{
			ClearListViewItems();

			if (listCollectionView == null || listCollectionView.Count < 2)
			{
				return;
			}

			var lst = listCollectionView.Cast<ColorBand>().ToList();

			for (var colorBandIndex = 0; colorBandIndex < listCollectionView.Count; colorBandIndex++)
			{
				var colorBand = lst[colorBandIndex];
				var listViewItem = CreateListViewItem(colorBandIndex, colorBand);

				ListViewItems.Add(listViewItem);
			}

			ListViewItems[listCollectionView.CurrentPosition].IsCurrent = true;

			CheckNameScope(_canvas, listCollectionView.Count);
		}

		private CbListViewItem CreateListViewItem(int colorBandIndex, ColorBand colorBand)
		{
			var listViewItem = new CbListViewItem(colorBandIndex, colorBand, _colorBandLayoutViewModel, GetNextNameSuffix());
			listViewItem.CbSectionLine.SectionLineMoved += HandleSectionLineMoved;

			//_canvas.RegisterName(listViewItem.CbRectangle.BlendedBandRectangle.Name, listViewItem.CbRectangle.BlendedBandRectangle);
			_canvas.RegisterName(listViewItem.Name, listViewItem);

			return listViewItem;
		}

		private string GetNextNameSuffix()
		{
			var result = _nextNameSuffix++.ToString();
			return result;
		}

		//private CbListViewItem CreateListViewItem(int colorBandIndex, ColorBand colorBand, string nameSuffix)
		//{
		//	// Build the CbRectangle
		//	var xPosition = colorBand.PreviousCutoff ?? 0;
		//	var bandWidth = colorBand.BucketWidth; // colorBand.Cutoff - xPosition;
		//	var blend = colorBand.BlendStyle == ColorBandBlendStyle.End || colorBand.BlendStyle == ColorBandBlendStyle.Next;

		//	var cbRectangle = new CbRectangle(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel, nameSuffix);

		//	// Build the Selection Line
		//	var selectionLinePosition = colorBand.Cutoff;
		//	var cbSectionLine = new CbSectionLine(colorBandIndex, selectionLinePosition, _colorBandLayoutViewModel);

		//	// Build the Color Block
		//	var cbColorBlock = new CbColorBlock(colorBandIndex, xPosition, bandWidth, colorBand.StartColor, colorBand.ActualEndColor, blend, _colorBandLayoutViewModel);

		//	var listViewItem = new CbListViewItem(colorBand, cbRectangle, cbSectionLine, cbColorBlock);
		//	listViewItem.CbSectionLine.SectionLineMoved += HandleSectionLineMoved;

		//	return listViewItem;
		//}

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
			foreach (var listViewItem in ListViewItems)
			{
				listViewItem.CbSectionLine.SectionLineMoved -= HandleSectionLineMoved;

				//_canvas.UnregisterName(listViewItem.CbRectangle.BlendedBandRectangle.Name);
				_canvas.UnregisterName(listViewItem.Name);

				listViewItem.TearDown();
			}

			ListViewItems.Clear();
			CheckNameScope(_canvas, 0);
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

		#region Animation

		private void AnimateBandDeletion(int index)
		{
			//_bandDeletionAnimation.Storyboard.Duration = TimeSpan.FromMilliseconds(5000);

			_bandDeletionAnimation.Storyboard.Children.Clear();

			var itemBeingRemoved = ListViewItems[index];
			_bandDeletionAnimation.AddOpacityAnimation(itemBeingRemoved.Name, "Opacity", from: 1, to: 0, TimeSpan.FromMilliseconds(300), TimeSpan.Zero);

			if (index == 0)
			{
				var newFirstItem = ListViewItems[index + 1];

				var curVal = newFirstItem.PreviousCutoff;
				if (double.IsNaN(curVal))
				{
					Debug.WriteLine("Not animating -- The PreviousCutoff is NAN.");
					return;
				}

				// This updates the XPosition and Width --to keep the Cutoff the same.
				_bandDeletionAnimation.AddDoubleAnimation(newFirstItem.Name, "PreviousCutoff", from: curVal, to: 0, TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(400));
			}
			else
			{
				var widthOfItemBeingRemoved = itemBeingRemoved.Width;

				var preceedingItem = ListViewItems[index - 1];
				var curVal = preceedingItem.Width;
				var newVal = curVal + widthOfItemBeingRemoved;

				if (double.IsNaN(curVal) || double.IsNaN(newVal))
				{
					Debug.WriteLine("Not animating -- The current or new Width is NAN.");
					return;
				}

				// Update the Width and Cutoff of the preceeding band, keeping its PreviousCutoff constant.
				_bandDeletionAnimation.AddDoubleAnimation(preceedingItem.Name, "Width", from: curVal, to: newVal, TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(400));
			}

			_bandDeletionAnimation.Begin(index);
		}

		private void AfterAnimateDeletion(int index)
		{
			Debug.WriteLine("AnimateDeletion StoryBoard has completed.");

			if (index == 0)
			{
				Debug.WriteLine("Handling AfterAnimateDeletion and the index = 0.");
			}

			var lvi = ListViewItems[index];

			_canvas.UnregisterName(lvi.Name);

			if (index == 0)
			{
				var firstLvi = ListViewItems[index + 1];
				firstLvi.ColorBand.PreviousCutoff = 0;
			}
			else
			{
				// Set the on screen representation
				var precedingLvi = ListViewItems[index - 1];
				precedingLvi.Cutoff = lvi.ColorBand.Cutoff;

				// update the model
				var precedingColorBand = precedingLvi.ColorBand;
				precedingColorBand.Cutoff = lvi.ColorBand.Cutoff;
				//precedingColorBand.SuccessorStartColor = lvi.ColorBand.StartColor;
			}

			lvi.TearDown();
			ListViewItems.Remove(lvi);
			Reindex(lvi.ColorBandIndex);
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
		private void ReportNameScopeDetails(DependencyObject dependencyObject)
		{
			var o = dependencyObject.GetValue(NameScope.NameScopeProperty);

			if (o is NameScope ns)
			{
				Debug.WriteLine($"CbsListView. The Canvas already has a NameScope with {ns.Count} registered names.");
			}
			else
			{
				Debug.WriteLine($"CbsListView. The Canvas already has a NameScope but the value is unavailable.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckNameScope(DependencyObject dependencyObject, int expectedCount)
		{
			var o = dependencyObject.GetValue(NameScope.NameScopeProperty);

			if (o is NameScope ns)
			{
				Debug.Assert(ns.Count == expectedCount, $"The NameScope has {ns.Count} items, expected the count to be {expectedCount}.");
			}
			else
			{
				Debug.WriteLine($"CbsListView. The Canvas already has a NameScope but the value is unavailable.");
			}
		}

		[Conditional("DEBUG")]
		private void CheckOldItems(ColorBand? colorBand, IList? oldItems)
		{
			if (colorBand is null)
			{
				throw new InvalidOperationException("e.OldItems[0] is not a ColorBand!");
			}
			else
			{
				Debug.Assert((oldItems?.Count ?? 0) == 1, "Received more than 1 old item on Notify Collection Changed -- Remove.");
			}
		}

		#endregion

		#region Unused HitTest Logic

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

	internal class StoryboardDetails
	{
		public Action<int> _completionCallback;
		private int _callbackIndex;

		public StoryboardDetails(Storyboard storyboard, FrameworkElement containingObject, Action<int> completionCallback/*, AnimationTimeline animationTimeline, string propertyPath*/)
		{
			if (NameScope.GetNameScope(containingObject) == null)
			{
				throw new ArgumentException("The ContainingObject must have a NameScope.");
			}

			Storyboard = storyboard;
			ContainingObject = containingObject;

			//Storyboard.FillBehavior = FillBehavior.Stop;

			_completionCallback = completionCallback;

			Storyboard.Completed += Storyboard_Completed;
		}

		#region Public Properties

		public Storyboard Storyboard { get; init; }
		public FrameworkElement ContainingObject { get; init; }

		//public Duration Duration
		//{
		//	get => Storyboard.Duration;
		//	set
		//	{
		//		Storyboard.Duration = value;
		//		_animationTimeline.Duration = value;
		//	}
		//}

		#endregion

		#region Public Methods

		public int AddDoubleAnimation(string objectName, string propertyPath, double from, double to, TimeSpan duration, TimeSpan startTime)
		{
			var da = new DoubleAnimation(from, to, duration);
			da.BeginTime = startTime;
			Storyboard.SetTargetName(da, objectName);
			Storyboard.SetTargetProperty(da, new PropertyPath(propertyPath));

			Storyboard.Children.Add(da);
			return Storyboard.Children.Count;
		}

		public int AddOpacityAnimation(string objectName, string propertyPath, double from, double to, TimeSpan duration, TimeSpan startTime)
		{
			var da = new DoubleAnimation(from, to, duration);
			Storyboard.SetTargetName(da, objectName);
			Storyboard.SetTargetProperty(da, new PropertyPath(propertyPath));

			Storyboard.Children.Add(da);
			return Storyboard.Children.Count;
		}

		public void Begin(int index)
		{
			_callbackIndex = index;
			Storyboard.Begin(ContainingObject);
		}

		#endregion

		#region Private Methods

		private void Storyboard_Completed(object? sender, EventArgs e)
		{
			//Storyboard.Children.Clear();

			if (_completionCallback != null)
			{
				_completionCallback(_callbackIndex);
			}
		}

		#endregion
	}
}
