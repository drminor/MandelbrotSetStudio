using MSS.Types;
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
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MSetExplorer
{
	using AnimationItemPairList = List<(IRectAnimationItem, IRectAnimationItem)>;

	internal class CbListView
	{
		#region Private Fields

		private const double ANIMATION_PIXELS_PER_MS = 700 / 1000d;     // 700 pixels per second or 0.7 pixels / millisecond

		private StoryboardDetails _storyBoardDetails1;
		private int _nextNameSuffix;

		private Canvas _canvas;

		private ListCollectionView _colorBandsView;

		private CbListViewElevations _elevations;
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

		private PushColorsAnimationInfo? _pushColorsAnimationInfo1 = null;
		private PullColorsAnimationInfo? _pullColorsAnimationInfo1 = null;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListView(Canvas canvas, ListCollectionView colorBandsView, double elevation, double controlHeight, SizeDbl contentScale, bool parentIsFocused, ColorBandSetEditMode currentCbEditMode, 
			ContextMenuDisplayRequest displayContextMenu, Action<ColorBandSetEditMode> currentCbEditModeChanged)
		{
			_canvas = canvas;

			_nextNameSuffix = 0;
			_storyBoardDetails1 = new StoryboardDetails(new Storyboard(), _canvas);
			CheckNameScope(_canvas, 0);

			_colorBandsView = colorBandsView;

			_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBandsView_CollectionChanged;

			_elevations = new CbListViewElevations(elevation, controlHeight);

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(_canvas, contentScale, parentIsFocused, ListViewItemSelectedChanged, HandleContextMenuDisplayRequested);

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

			DrawColorBands(_colorBandsView);

			_canvas.SizeChanged += Handle_SizeChanged;
			_canvas.MouseMove += Handle_MouseMove;
			_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;
		}

		private void Handle_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbListView is handling the SizeChanged event.");

			if (e.HeightChanged)
			{
				ControlHeight = e.NewSize.Height;
			}
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

		public double Elevation
		{
			get => _elevations.Elevation;
			set
			{
				if (value != _elevations.Elevation)
				{
					_elevations.Elevation = value;
					UpdateItemsElevation(_elevations);
				}
			}
		}

		public double ControlHeight
		{
			get => _elevations.ControlHeight;
			set
			{
				if (value != _elevations.ControlHeight)
				{
					_elevations.ControlHeight = value;
					UpdateItemsElevation(_elevations);
				}
			} 
		}

		public SizeDbl ContentScale
		{
			get => _colorBandLayoutViewModel.ContentScale;
			set => _colorBandLayoutViewModel.ContentScale = value;
		}

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
						Debug.WriteLine($"The Mouse is now over SectionLine: {SectionLineUnderMouse?.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void SelectedIndexWasMoved(int newColorBandIndex, int direction)
		{
			// User pressed the Left Arrow or Right Arrow key.

			var foundError = false;

			//BlendRectangleUnderMouse = null;
			//SectionLineUnderMouse = null;

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

				switch (CurrentCbEditMode)
				{
					case ColorBandSetEditMode.Cutoffs:
						SectionLineUnderMouse = ListViewItems[newColorBandIndex];
						break;

					case ColorBandSetEditMode.Colors:
						ColorBlocksUnderMouse = ListViewItems[newColorBandIndex];
						break;
					
					case ColorBandSetEditMode.Bands:
						BlendRectangleUnderMouse = ListViewItems[newColorBandIndex];
						break;
					
					default:
						break;
				}

			}

			_indexOfMostRecentlySelectedItem = newColorBandIndex;

			if (foundError)
			{
				Debug.WriteLine("Look at me.!");
			}
		}

		public (CbListViewItem, ColorBandSelectionType)? GetItemAtMousePosition(Point hitPoint)
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

					if (hitPoint.Y >= _elevations.BlendRectanglesElevation)
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

		public (int, ColorBandSelectionType)? GetItemIndexAtMousePosition(Point hitPoint)
		{
			if (TryGetSectionLineIndex(hitPoint, ListViewItems, out var distance, out var cbListViewItemIndex))
			{
				if (Math.Abs(distance) < 6)
				{
					return (cbListViewItemIndex, ColorBandSelectionType.Cutoff);
				}
				else
				{
					var selIndex = distance > 0 ? cbListViewItemIndex + 1 : cbListViewItemIndex;

					if (hitPoint.Y >= _elevations.BlendRectanglesElevation)
					{
						return (selIndex, ColorBandSelectionType.Band);
					}
					else
					{
						return (selIndex, ColorBandSelectionType.Color);
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

			_canvas.SizeChanged -= Handle_SizeChanged;
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
				if (Math.Abs(distance) < 6 && hitPoint.Y <= _elevations.ColorBlocksElevation)
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

					if (selIndex > ListViewItems.Count - 1)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Handling MouseMove. The HitPoint X coordinate is beyond the canvas. Canvas Length: {_canvas.ActualWidth}, HitPointX: {hitPoint.X}, SelIndex {selIndex}.");
						selIndex = ListViewItems.Count - 1;
					}
						 
					if (hitPoint.Y >= _elevations.BlendRectanglesElevation)
					{
						ColorBlocksUnderMouse = null;
						BlendRectangleUnderMouse = ListViewItems[selIndex];
					}
					else
					{
						ColorBlocksUnderMouse = ListViewItems[selIndex];
						BlendRectangleUnderMouse = null;
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
					if (selIndex > ListViewItems.Count - 1) selIndex = ListViewItems.Count - 1;

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

		private void SectionLineWasMoved(CbSectionLineMovedEventArgs e)
		{
			//if (e.Operation == CbSectionLineDragOperation.NotStarted)
			//{
			//	if (e.UpdatingPrevious)
			//	{
			//		_colorBandsView.MoveCurrentToPosition(e.ColorBandIndex);
			//	}

			//	Debug.WriteIf(_useDetailedDebug, $"CbListView. Drag not started. CbRectangle: {CurrentColorBandIndex} is now current.");

			//	_sectionLineBeingDragged = null;
			//	return;
			//}

			switch (e.Operation)
			{
				case CbSectionLineDragOperation.Started:
					if (e.UpdatingPrevious)
					{
						_colorBandsView.MoveCurrentToPosition(e.ColorBandIndex + 1);
					}

					UpdateCutoff(e);
					break;

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
				case CbSectionLineDragOperation.NotStarted:
					_sectionLineBeingDragged = null;
					Debug.WriteIf(_useDetailedDebug, $"CbListView. Drag not started. CbRectangle: {CurrentColorBandIndex} is now current.");
					break;

				default:
					throw new InvalidOperationException($"The {e.Operation} CbSectionLineDragOperation is not supported.");
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
				//// Remove items
				//Debug.WriteLine($"CbListView is handling CollectionChanged: Remove.");
				//var colorBand = e.OldItems?[0] as ColorBand;
				//CheckOldItems(colorBand, e.OldItems);

				//var lvi = ListViewItems.FirstOrDefault(x => x.ColorBand == colorBand);
				//if (lvi != null)
				//{
				//	Debug.WriteLine($"CbListView is removing a ColorBand: {colorBand}");

				//	var idx = _colorBandsView.IndexOf(lvi.ColorBand);

				//	AnimateBandDeletion(lvi.ColorBandIndex);
				//}
				//else
				//{
				//	Debug.WriteLine($"CbListView cannot find ColorBand: {colorBand} in the ListViewItems.");
				//}
			}
		}

		#endregion

		#region Private Methods

		private void StartDrag(CbListViewItem cbListViewItem, Point hitPoint)
		{
			var colorBandIndex = cbListViewItem.ColorBandIndex;

			if (cbListViewItem.IsLast)
			{
				// Cannot change the position of the last Selection Line.
				return;
			}

			// Positive if the mouse is to the right of the selection line, negative if to the left.
			var hitPointDistance = hitPoint.X - cbListViewItem.SectionLinePositionX;

			// True if we will be updating the Current ColorBand's PreviousCutoff value, false if updating the Cutoff
			bool updatingPrevious = hitPointDistance > 0;

			var indexForCurrentItem = updatingPrevious ? colorBandIndex + 1 : colorBandIndex;

			_colorBandsView?.MoveCurrentToPosition(colorBandIndex);

			var cbSectionLine = cbListViewItem.CbSectionLine;
			_sectionLineBeingDragged = cbSectionLine;

			Debug.WriteIf(_useDetailedDebug, $"CbListView. Starting Drag for SectionLine: {colorBandIndex}.");

			ReportColorBandRectanglesInPlay(ListViewItems, indexForCurrentItem);

			var gLeft = ListViewItems[colorBandIndex].CbRectangle.RectangleGeometry;
			var gRight = ListViewItems[colorBandIndex + 1].CbRectangle.RectangleGeometry;

			cbSectionLine.StartDrag(gLeft.Rect.Width, gRight.Rect.Width, updatingPrevious);
		}

		private bool TryGetSectionLineIndex(Point hitPoint, List<CbListViewItem> cbListViewItems, out double distance, out int listViewItemIndex)
		{
			//var diagP = ContentScale.Width * 60;

			listViewItemIndex = -1;

			double smallestAbsDist = int.MaxValue;
			double smallestDist = int.MaxValue;

			for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
			{
				var cbListViewItem = cbListViewItems[cbLinePtr];

				//if (!ScreenTypeHelper.IsDoubleChanged(diagP, cbListViewItem.SectionLinePositionX))
				//{
				//	ReportColorBands("Mouse Move at 60", ListViewItems);
				//}

				var dist = hitPoint.X - cbListViewItem.SectionLinePositionX;
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
			var listViewItem = new CbListViewItem(colorBandIndex, colorBand, _elevations, _colorBandLayoutViewModel, GetNextNameSuffix(), SectionLineWasMoved);
					
			_storyBoardDetails1.OurNameScope.RegisterName(listViewItem.Name, listViewItem);

			return listViewItem;
		}

		private void RemoveListViewItem(CbListViewItem listViewItem)
		{
			ListViewItems.Remove(listViewItem);
			listViewItem.TearDown();
			_canvas.UnregisterName(listViewItem.Name);
		}

		private string GetNextNameSuffix()
		{
			var result = _nextNameSuffix++.ToString();
			return result;
		}

		private void ListViewItemSelectedChanged(int colorBandIndex, ColorBandSetEditMode colorBandEditMode)
		{
			// The user has pressed the left mouse-button while over a Rectangle or SectionLine.

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

			switch (e.Operation)
			{
				case CbSectionLineDragOperation.Started:

					if (colorBandToUpdate.IsInEditMode)
					{
						Debug.WriteLine("WARNING: On UpdateCutoff, op = Started, the ColorBandToUpdate is already in EditMode.");
					}
					else
					{
						colorBandToUpdate.BeginEdit();
					}

					if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;

					break;
				case CbSectionLineDragOperation.Move:

					if (!colorBandToUpdate.IsInEditMode)
					{
						Debug.WriteLine("WARNING: On UpdateCutoff, op = Move, the ColorBandToUpdate is not yet in EditMode.");
						colorBandToUpdate.BeginEdit();
					}

					if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;

					break;
				case CbSectionLineDragOperation.Complete:

					if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
					colorBandToUpdate.EndEdit();

					break;
				case CbSectionLineDragOperation.Cancel:

					if (e.UpdatingPrevious) colorBandToUpdate.PreviousCutoff = newCutoff; else colorBandToUpdate.Cutoff = newCutoff;
					colorBandToUpdate.CancelEdit();

					break;
				case CbSectionLineDragOperation.NotStarted:
					// No Action 
					break;
				default:

					throw new InvalidOperationException($"The {e.Operation} CbSectionLineDragOperation is not supported.");
			}
		}

		private void UpdateItemsElevation(CbListViewElevations elevations)
		{
			foreach (var cbListViewItem in ListViewItems)
			{
				var curVal = cbListViewItem.Area;
				cbListViewItem.Area = new Rect(curVal.X, elevations.Elevation, curVal.Width, elevations.ControlHeight);
			}
		}

		#endregion

		#region Animation Support - Insertions

		public void AnimateInsertCutoff(Action<int> onAnimationComplete, int index)
		{
			//var itemBeingRemoved = ListViewItems[index];

			_storyBoardDetails1.Begin(AnimateInsertCutoffPost, onAnimationComplete, index, debounce: false);
		}

		private void AnimateInsertCutoffPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("CutoffInsertion Animation has completed.");

			//var lvi = ListViewItems[index];

			onAnimationComplete(index);
		}

		public void AnimateInsertColor(Action<int> onAnimationComplete, int index)
		{
			//var itemBeingRemoved = ListViewItems[index];

			_storyBoardDetails1.Begin(AnimateInsertColorPost, onAnimationComplete, index, debounce: false);
		}

		private void AnimateInsertColorPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("ColorInsertion Animation has completed.");

			//var lvi = ListViewItems[index];

			onAnimationComplete(index);
		}

		public void AnimateInsertBand(Action<int> onAnimationComplete, int index)
		{
			//var itemBeingRemoved = ListViewItems[index];

			_storyBoardDetails1.Begin(AnimateInsertBandPost, onAnimationComplete, index, debounce: false);
		}

		private void AnimateInsertBandPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("ColorBandInsertion Animation has completed.");

			//var lvi = ListViewItems[index];

			onAnimationComplete(index);

			_ = SynchronizeCurrentItem();
		}

		#endregion

		#region Animation Support - Deletions

		public void AnimateDeleteCutoff(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine($"AnimateDeleteCutoff. Index = {index}.");
			ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteCutoff", ListViewItems);

			// Create the class that will calcuate the 'PushColor' animation details
			var liftHeight = _elevations.ColorBlocksHeight / 2;

			_pushColorsAnimationInfo1 = new PushColorsAnimationInfo(liftHeight, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < ListViewItems.Count; i++)
			{
				var lviSource = ListViewItems[i];
				var lviDestination = i == ListViewItems.Count - 1 ? null : ListViewItems[i + 1];
				_pushColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			var startPushSyncPoint = _pushColorsAnimationInfo1.CalculateMovements();
			var endPushSyncPoint = _pushColorsAnimationInfo1.GetMaxDuration();
			var shiftMs = endPushSyncPoint - startPushSyncPoint;

			_storyBoardDetails1.RateFactor = 1;

			ApplyAnimationItemPairs(_pushColorsAnimationInfo1.AnimationItemPairs);

			if (index == 0)
			{
				// Pull the left edge of the first band so that it starts at Zero.
				var newFirstItem = ListViewItems[index + 1];
				var curVal = newFirstItem.Area;
				var newXPosition = 0;
				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(startPushSyncPoint), duration: TimeSpan.FromMilliseconds(shiftMs));
			}
			else
			{
				// Widen the band immediately before the band being deleted to take up the available room.
				var itemBeingRemoved = ListViewItems[index];
				var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;
				var preceedingItem = ListViewItems[index - 1];

				var curVal = preceedingItem.Area;
				var newWidth = curVal.Width + widthOfItemBeingRemoved;
				_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(startPushSyncPoint), duration: TimeSpan.FromMilliseconds(shiftMs));
			}

			ListViewItems[^2].CbRectangle.EndColor = ColorBandColor.Black;
			ListViewItems[^2].CbColorBlock.EndColor = ColorBandColor.Black;

			// Execute the Animation
			_storyBoardDetails1.Begin(AnimateDeleteCutoffPost, onAnimationComplete, index, debounce: true);
		}

		private void AnimateDeleteCutoffPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("ANIMATION COMPLETED\n CutoffDeletion Animation has completed.");

			_pushColorsAnimationInfo1?.MoveSourcesToDestinations();
			_pushColorsAnimationInfo1 = null;

			var lvi = ListViewItems[index];

			RemoveListViewItem(lvi);
			Reindex(lvi.ColorBandIndex);

			onAnimationComplete(index);

			_ = SynchronizeCurrentItem();
		}

		public void AnimateDeleteColor(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine($"AnimateDeleteColor. Index = {index}.");
			//ReportCanvasChildren();
			//ReportColorBands("Top of AnimateDeleteColor", ListViewItems);

			// Create a ListViewItem to hold the new source
			var newColorBand = CreateColorBandFromReservedBand(ListViewItems[^1], reservedColorBand: null);
			var newLvi = CreateListViewItem(ListViewItems.Count, newColorBand);

			// Create the class that will calcuate the 'PullColor' animation details
			var liftHeight = _elevations.ColorBlocksHeight;
			_pullColorsAnimationInfo1 = new PullColorsAnimationInfo(liftHeight, ANIMATION_PIXELS_PER_MS);

			for (var i = index; i < ListViewItems.Count; i++)
			{
				var lviDestination = ListViewItems[i];
				var lviSource = i == ListViewItems.Count - 1 ? newLvi : ListViewItems[i + 1];
				_pullColorsAnimationInfo1.Add(lviSource, lviDestination);
			}

			_ = _pullColorsAnimationInfo1.CalculateMovements();

			_storyBoardDetails1.RateFactor = 1;

			ApplyAnimationItemPairs(_pullColorsAnimationInfo1.AnimationItemPairs);

			_storyBoardDetails1.Begin(AnimateDeleteColorPost, onAnimationComplete, index, debounce: true);
		}

		private void AnimateDeleteColorPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("ANIMATION COMPLETED\n ColorDeletion Animation has completed.");

			if (_pullColorsAnimationInfo1 != null)
			{
				var newLvi = _pullColorsAnimationInfo1.AnimationItemPairs[^1].Item1.SourceListViewItem;
				_pullColorsAnimationInfo1.MoveSourcesToDestinations();

				newLvi.TearDown();
				_canvas.UnregisterName(newLvi.Name);

				_pullColorsAnimationInfo1 = null;
			}

			if (index > 0)
			{
				var prevCb = ListViewItems[index - 1];

				if (prevCb.ColorBand.BlendStyle == ColorBandBlendStyle.Next)
				{
					var cbListViewItem = ListViewItems[index];
					prevCb.CbColorBlock.EndColor = cbListViewItem.CbColorBlock.StartColor;
					prevCb.CbRectangle.EndColor = cbListViewItem.CbRectangle.StartColor;
				}
			}

			onAnimationComplete(index);

			//var lastCbListViewItem = ListViewItems[^1];

			//var lastCb = (ColorBand)_colorBandsView.GetItemAt(lastCbListViewItem.ColorBandIndex);

			//lastCbListViewItem.CbRectangle.StartColor = lastCb.StartColor;
			//lastCbListViewItem.CbRectangle.EndColor = lastCb.EndColor;
			//lastCbListViewItem.CbRectangle.Blend = lastCb.BlendStyle != ColorBandBlendStyle.None;

			//lastCbListViewItem.CbColorBlock.StartColor = lastCb.StartColor;
			//lastCbListViewItem.CbColorBlock.EndColor = lastCb.EndColor;
			//lastCbListViewItem.CbColorBlock.Blend = lastCb.BlendStyle != ColorBandBlendStyle.None;
			//lastCbListViewItem.CbColorBlock.ColorPairVisibility = Visibility.Visible;
		}

		public void AnimateDeleteBand(Action<int> onAnimationComplete, int index)
		{
			//_storyBoardDetails1.RateFactor = 5;

			var itemBeingRemoved = ListViewItems[index];
			_storyBoardDetails1.AddMakeTransparent(itemBeingRemoved.Name, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(400));

			itemBeingRemoved.ElevationsAreLocal = true;
			var curVal = itemBeingRemoved.Area;
			var newSize = new Size(curVal.Size.Width * 0.25, curVal.Size.Height * 0.25);

			_storyBoardDetails1.AddChangeSize(itemBeingRemoved.Name, "Area", from: curVal, newSize: newSize, beginTime: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(300));

			if (index == 0)
			{
				var newFirstItem = ListViewItems[index + 1];
				curVal = newFirstItem.Area;
				var newXPosition = 0;

				_storyBoardDetails1.AddChangeLeft(newFirstItem.Name, "Area", from: curVal, newX1: newXPosition, beginTime: TimeSpan.FromMilliseconds(400), duration: TimeSpan.FromMilliseconds(300));
			}
			else
			{
				var widthOfItemBeingRemoved = itemBeingRemoved.Area.Width;

				var preceedingItem = ListViewItems[index - 1];

				if (index < ListViewItems.Count - 2)
				{
					preceedingItem.ColorBand.SuccessorStartColor = ListViewItems[index + 1].ColorBand.StartColor;
				}

				curVal = preceedingItem.Area;
				var newWidth = curVal.Width + widthOfItemBeingRemoved;

				_storyBoardDetails1.AddChangeWidth(preceedingItem.Name, "Area", from: curVal, newWidth: newWidth, beginTime: TimeSpan.FromMilliseconds(400), duration: TimeSpan.FromMilliseconds(300));
			}

			_storyBoardDetails1.Begin(AnimateDeleteBandPost, onAnimationComplete, index, debounce: false);
		}

		private void AnimateDeleteBandPost(Action<int> onAnimationComplete, int index)
		{
			Debug.WriteLine("ANIMATION COMPLETED\n BandDeletion Animation has completed.");

			var lvi = ListViewItems[index];

			RemoveListViewItem(lvi);
			Reindex(lvi.ColorBandIndex);

			onAnimationComplete(index);

			_ = SynchronizeCurrentItem();
		}

		// Returns true is any update was made
		private bool SynchronizeCurrentItem()
		{
			var result = false;

			if (_colorBandsView.IsCurrentAfterLast)
			{
				_colorBandsView.MoveCurrentToLast();
			}

			if (_colorBandsView.IsCurrentBeforeFirst)
			{
				_colorBandsView.MoveCurrentToFirst();
			}

			var indexOfCurrentItem = _colorBandsView.CurrentPosition;

			var foundCItem = false;
			for (var i = 0; i < ListViewItems.Count; i++)
			{
				if (ListViewItems[i].IsCurrent)
				{
					if (i != indexOfCurrentItem)
					{
						ListViewItems[i].IsCurrent = false;
						result = true;
					}
					else
					{
						foundCItem = true;
					}
				}
			}

			if (!foundCItem)
			{
				ListViewItems[indexOfCurrentItem].IsCurrent = true;
				result = true;
			}

			return result;
		}

		private void ApplyAnimationItemPairs(AnimationItemPairList animationItemPairList)
		{
			foreach (var (block, blend) in animationItemPairList)
			{
				foreach (var tl in block.RectTransitions)
				{
					_storyBoardDetails1.AddRectAnimation(block.Name, "ColorBlockArea", tl.From, tl.To, TimeSpan.FromMilliseconds(tl.BeginMs), TimeSpan.FromMilliseconds(tl.DurationMs));
				}

				foreach (var tl in blend.RectTransitions)
				{
					_storyBoardDetails1.AddRectAnimation(blend.Name, "BlendedColorArea", tl.From, tl.To, TimeSpan.FromMilliseconds(tl.BeginMs), TimeSpan.FromMilliseconds(tl.DurationMs));
				}
			}
		}

		private ColorBand CreateColorBandFromReservedBand(CbListViewItem lastListViewItem, ReservedColorBand? reservedColorBand)
		{
			ColorBand result;

			var cb = lastListViewItem.ColorBand;

			var previousCutoff = cb.Cutoff;
			var cutOff = cb.Cutoff + 10;

			if (reservedColorBand != null)
			{
				result = new ColorBand(cutOff, reservedColorBand.StartColor, reservedColorBand.BlendStyle, reservedColorBand.EndColor, previousCutoff: previousCutoff, successorStartColor: null, percentage: 0);
			}
			else
			{
				result = new ColorBand()
				{
					Cutoff = cutOff,
					PreviousCutoff = previousCutoff,
					SuccessorStartColor = null,
					Percentage = 0
				};
			}

			return result;
		}

		#endregion

		#region Diagnostics

		[Conditional("DEGUG")]
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

		[Conditional("DEGUG2")]
		private void ReportColorBandRectanglesInPlay(List<CbListViewItem> listViewItems, int sectionLineIndex)
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

		[Conditional("DEBUG2")]
		private void ReportColorBands(string desc, List<CbListViewItem> cbListViewItems)
		{
			Debug.WriteLine($"ColorBands: {desc}");

			for (var cbLinePtr = 0; cbLinePtr < cbListViewItems.Count; cbLinePtr++)
			{
				var item = cbListViewItems[cbLinePtr];
				Debug.WriteLine($"ColorBand: {cbLinePtr} Start: {item.ColorBand.PreviousCutoff ?? -1}, End: {item.ColorBand.Cutoff}, Width: {item.ColorBand.BucketWidth}.");
			}
		}

		[Conditional("DEBUG2")]
		private void ReportCanvasChildren()
		{
			for (var i = 0; i < _canvas.Children.Count; i++)
			{
				var child = _canvas.Children[i];
				var bounds = GetBounds(child);

				if (child is FrameworkElement fe)
				{
					Debug.WriteLine($"Child:{i} {child.GetType()} Bounds: {bounds} Visibility: {fe.Visibility}");
				}
				else
				{
					Debug.WriteLine($"Child:{i} {child.GetType()} Bounds: {bounds}");
				}
			}
		}

		private Rect GetBounds(UIElement uIElement)
		{
			if (uIElement is Path p)
			{
				return p.Data.Bounds;
			}
			else if (uIElement is Line l)
			{
				var r = new Rect(l.X1, l.Y1, l.Width, l.Height);
				return r;
			}
			else if (uIElement is Polygon t)
			{
				var pts = t.Points;

				var rp = new Rect(pts[1], pts[0]);
				return rp;
			}
			else
			{
				return new Rect(0, 0, 0, 0);
			}
		}

		#endregion
	}

	//public enum ColorBandSetEditOperation
	//{
	//	InsertCutoff,
	//	DeleteCutoff,

	//	InsertColor,
	//	DeleteColor,

	//	InsertBand,
	//	DeleteBand
	//}
}
