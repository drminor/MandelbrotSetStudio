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
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbListView
	{
		#region Private Fields

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
		private ColorBandSetEditMode? _newEditModeIfDragIsCancelled;

		private CbListViewItem? _sectionLineUnderMouse;
		private CbListViewItem? _blendRectangleUnderMouse;
		private CbListViewItem? _colorBlocksUnderMouse;

		private int? _selectedItemsRangeAnchorIndex;
		private int? _indexOfMostRecentlySelectedItem;

		private readonly INameScope _ourNameScope;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbListView(Canvas canvas, ListCollectionView colorBandsView, double elevation, double controlHeight, SizeDbl contentScale, bool parentIsFocused, ColorBandSetEditMode currentCbEditMode, 
			ContextMenuDisplayRequest displayContextMenu, Action<ColorBandSetEditMode> currentCbEditModeChanged, INameScope nameScope)
		{
			_ourNameScope = nameScope;
			_canvas = canvas;

			_nextNameSuffix = 0;

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
			_newEditModeIfDragIsCancelled = null;

			_sectionLineUnderMouse = null;
			_blendRectangleUnderMouse = null;
			_selectedItemsRangeAnchorIndex = null;
			_indexOfMostRecentlySelectedItem = null;

			CheckNameScope(_canvas, 0);
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
						Debug.WriteLineIf(_useDetailedDebug, $"The Mouse is now over Rectangle: {_blendRectangleUnderMouse.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
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
						Debug.WriteLineIf(_useDetailedDebug, $"The Mouse is now over ColorBlocks: {_colorBlocksUnderMouse.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
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
						Debug.WriteLineIf(_useDetailedDebug, $"The Mouse is now over SectionLine: {SectionLineUnderMouse?.ColorBandIndex}, EditMode = {CurrentCbEditMode}.");
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
							Debug.WriteLine($"WARINING: CbListView. SelectionIndexWasMoved. Shift key was pressed. At least one item is selected but both the range anchor and the most recently selected item is null.");
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
			if (_newEditModeIfDragIsCancelled != null)
			{
				CurrentCbEditMode = _newEditModeIfDragIsCancelled.Value;
				_newEditModeIfDragIsCancelled = null;
			}

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

		public void ReportColorBands(string desc)
		{
			Debug.WriteLine($"ColorBands: {desc}");

			for (var cbLinePtr = 0; cbLinePtr < ListViewItems.Count; cbLinePtr++)
			{
				var item = ListViewItems[cbLinePtr];
				Debug.WriteLine($"ColorBand: {cbLinePtr} Start: {item.ColorBand.PreviousCutoff ?? -1}, End: {item.ColorBand.Cutoff}, Width: {item.ColorBand.BucketWidth}, StartColor: {item.ColorBand.StartColor}, EndColor: {item.ColorBand.EndColor}, ActualEndColor: {item.ColorBand.ActualEndColor}.");
			}
		}

		public void ReportListViewItems(string desc)
		{
			Debug.WriteLine($"ListViewItems: {desc}");

			for (var cbLinePtr = 0; cbLinePtr < ListViewItems.Count; cbLinePtr++)
			{
				var item = ListViewItems[cbLinePtr];
				Debug.WriteLine($"ListViewItem: {cbLinePtr} Start: {item.CbRectangle.BlendRectangleArea.Left}, End: {item.CbRectangle.BlendRectangleArea.Right}, Width: {item.CbRectangle.BlendRectangleArea.Width}, StartColor: {item.StartColor}, EndColor: {item.EndColor}.");
			}
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

					CurrentCbEditMode = GetEditModeFromHitpoint(hitPoint);

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
					_newEditModeIfDragIsCancelled = null;

					Debug.WriteLineIf(_useDetailedDebug, "Completing the SectionLine Drag Operation.");
					UpdateCutoff(e);
					break;

				case CbSectionLineDragOperation.Cancel:
					_sectionLineBeingDragged = null;
					_newEditModeIfDragIsCancelled = null;

					UpdateCutoff(e);
					break;
				case CbSectionLineDragOperation.NotStarted:
					_sectionLineBeingDragged = null;

					if (_newEditModeIfDragIsCancelled != null)
					{
						CurrentCbEditMode = _newEditModeIfDragIsCancelled.Value;
						_newEditModeIfDragIsCancelled = null;
					}

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
				//// Add items

				//Debug.WriteLine($"CbListView is handling CollectionChanged: Add. There are {e.NewItems?.Count ?? -1} new items.");

				//var bands = e.NewItems?.Cast<ColorBand>() ?? new List<ColorBand>();
				//var idx = e.NewStartingIndex;
				//foreach (var colorBand in bands)
				//{
				//	var listViewItem = CreateListViewItem(idx, colorBand);
				//	ListViewItems.Insert(idx++, listViewItem);
				//}

				//idx = e.NewStartingIndex; // Math.Max(e.NewStartingIndex - 1, 0);

				//Reindex(idx);
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
			_newEditModeIfDragIsCancelled = GetEditModeFromHitpoint(hitPoint);

			Debug.WriteIf(_useDetailedDebug, $"CbListView. Starting Drag for SectionLine: {colorBandIndex}.");

			ReportColorBandRectanglesInPlay(ListViewItems, indexForCurrentItem);

			var gLeft = ListViewItems[colorBandIndex].CbRectangle.RectangleGeometry;
			var gRight = ListViewItems[colorBandIndex + 1].CbRectangle.RectangleGeometry;

			cbSectionLine.StartDrag(gLeft.Rect.Width, gRight.Rect.Width, updatingPrevious);
		}

		private ColorBandSetEditMode GetEditModeFromHitpoint(Point hitPoint)
		{
			ColorBandSetEditMode result;

			if (hitPoint.Y <= _elevations.ColorBlocksElevation)
			{
				result = ColorBandSetEditMode.Cutoffs;
			}
			else if (hitPoint.Y >= _elevations.BlendRectanglesElevation)
			{
				result = ColorBandSetEditMode.Bands;
			}
			else
			{
				result = ColorBandSetEditMode.Colors;
			}

			return result;
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
			if (listCollectionView == null || listCollectionView.Count < 1)
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

		public CbListViewItem CreateListViewItem(int colorBandIndex, ColorBand colorBand)
		{
			var listViewItem = new CbListViewItem(colorBandIndex, colorBand, _elevations, _colorBandLayoutViewModel, GetNextNameSuffix(), SectionLineWasMoved);
					
			_ourNameScope.RegisterName(listViewItem.Name, listViewItem);

			return listViewItem;
		}

		public void RemoveListViewItem(CbListViewItem listViewItem)
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

		public void Reindex(int startingIndex)
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

			var indexToUpdate = e.UpdatingPrevious ? e.ColorBandIndex + 1 : e.ColorBandIndex;
			var colorBandToUpdate = ListViewItems[indexToUpdate].ColorBand;

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

					UpdateStartingOrEndingCutoff(e.UpdatingPrevious, colorBandToUpdate, e.NewCutoff, e.Operation, indexToUpdate);

					break;
				case CbSectionLineDragOperation.Move:

					if (!colorBandToUpdate.IsInEditMode)
					{
						Debug.WriteLine("WARNING: On UpdateCutoff, op = Move, the ColorBandToUpdate is not yet in EditMode.");
						colorBandToUpdate.BeginEdit();
					}

					UpdateStartingOrEndingCutoff(e.UpdatingPrevious, colorBandToUpdate, e.NewCutoff, e.Operation, indexToUpdate);

					break;
				case CbSectionLineDragOperation.Complete:

					UpdateStartingOrEndingCutoff(e.UpdatingPrevious, colorBandToUpdate, e.NewCutoff, e.Operation, indexToUpdate);
					colorBandToUpdate.EndEdit();

					break;
				case CbSectionLineDragOperation.Cancel:

					UpdateStartingOrEndingCutoff(e.UpdatingPrevious, colorBandToUpdate, e.NewCutoff, e.Operation, indexToUpdate);
					colorBandToUpdate.CancelEdit();

					break;
				case CbSectionLineDragOperation.NotStarted:
					// No Action 
					break;
				default:

					throw new InvalidOperationException($"The {e.Operation} CbSectionLineDragOperation is not supported.");
			}
		}

		private void UpdateStartingOrEndingCutoff(bool updatingPrevious, ColorBand colorBandToUpdate, double newCutoff, CbSectionLineDragOperation operation, int colorBandIndex)
		{
			var scaledValue = (int)Math.Round(newCutoff / ContentScale.Width);

			var prevMsg = updatingPrevious ? "PreviousCutoff" : "Cutoff";
			Debug.WriteLineIf(_useDetailedDebug, $"CbListView. Updating {prevMsg} for operation: {operation} at index: {colorBandIndex} with new {prevMsg}: {newCutoff}.");

			if (updatingPrevious)
			{
				colorBandToUpdate.PreviousCutoff = scaledValue;
			}
			else
			{
				colorBandToUpdate.Cutoff = scaledValue;
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

		// Returns true is any update was made
		public bool SynchronizeCurrentItem()
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

			if (_newEditModeIfDragIsCancelled == null)
			{
				Debug.WriteLine("WARNING: _newEditModeIfDragIsCancelled is null on HandleSectionLineMoved.");
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

}
