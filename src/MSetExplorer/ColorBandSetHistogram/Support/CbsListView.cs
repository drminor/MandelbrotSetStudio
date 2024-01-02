using MSS.Types;
using MSS.Types.MSet;
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
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MSetExplorer
{
	internal class CbsListView
	{
		#region Private Fields

		private ContextMenuDisplayRequest _displayContextMenu;
		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;

		private ListCollectionView _colorBandsView;
		private ColorBand? _currentColorBand;

		private List<Shape> _hitList;

		private CbsSelectionLine? _selectionLineBeingDragged;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsListView(Canvas canvas, ListCollectionView colorBandsView, double controlHeight, SizeDbl contentScale, bool useRealTimePreview, bool mouseIsEntered, ContextMenuDisplayRequest displayContextMenu)
		{
			_canvas = canvas;
			_colorBandsView = colorBandsView;

			_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
			(_colorBandsView as INotifyCollectionChanged).CollectionChanged += ColorBandsView_CollectionChanged;

			_colorBandLayoutViewModel = new ColorBandLayoutViewModel(contentScale, controlHeight);

			UseRealTimePreview = useRealTimePreview;
			MouseIsEntered = mouseIsEntered;
			_displayContextMenu = displayContextMenu;

			ListViewItems = new List<CbsListViewItem>();

			_hitList = new List<Shape>();

			DrawColorBands(_colorBandsView, showSectionLines: MouseIsEntered);

			_canvas.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;
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
					HilightColorBandRectangle(_currentColorBand, on: false);
				}

				_currentColorBand = value;

				if (_currentColorBand != null)
				{
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
			set => _colorBandLayoutViewModel.ContentScale = value;
		}

		public double CbrElevation => _colorBandLayoutViewModel.CbrElevation;

		public bool IsDragSelectionLineInProgress => _selectionLineBeingDragged != null;

		public bool MouseIsEntered { get; set; }

		#endregion

		#region Public Methods

		public (CbsListViewItem, ColorBandSelectionType)? MouseOverItem(Point hitPoint)
		{
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbsListViewItem, out _))
			{
				return (cbsListViewItem, ColorBandSelectionType.Cutoff);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbsListViewItem, out _))
				{
					return (cbsListViewItem, ColorBandSelectionType.Color);
				}
			}

			return null;
		}

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

				ShowSelectionLinesInternal();
			}

			MouseIsEntered = true;
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

			MouseIsEntered = false;
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
			_canvas.PreviewMouseLeftButtonDown -= Handle_PreviewMouseLeftButtonDown;

			if (_selectionLineBeingDragged != null)
			{
				_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
			}

			ClearListViewItems();
		}

		#endregion

		#region Event Handlers

		private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Handling PreviewMouseLeftButtonDown. ContentScale is {ContentScale}.");

			var cbsView = _colorBandsView;

			if (cbsView == null)
				return;

			var hitPoint = e.GetPosition(_canvas);
			if (TryGetSelectionLine(hitPoint, ListViewItems, out var cbsListViewItem, out var cbSelectionLineIndex))
			{
				var colorBandIndex = cbSelectionLineIndex.Value;

				if (colorBandIndex == ListViewItems.Count - 1)
				{
					// Cannot change the position of the last Selection Line.
					return;
				}

				var cb = cbsListViewItem.ColorBand;

				//var cbOld = GetColorBandAt(cbsView, cbSelectionLineIndex.Value);
				//Debug.Assert(cb == cbOld, "ColorBand MisMatch.");

				cbsView.MoveCurrentTo(cb);

				StartDrag(cbsListViewItem);
			}
			else
			{
				if (TryGetColorBandRectangle(hitPoint, ListViewItems, out cbsListViewItem, out var cbRectangleIndex))
				{
					var cb = cbsListViewItem.ColorBand;

					//var cbOld = GetColorBandAt(cbsView, cbRectangleIndex.Value);
					//Debug.Assert(cb == cbOld, "ColorBand MisMatch Rectangle.");

					cbsView.MoveCurrentTo(cb);
				}
			}
		}

		private void StartDrag(CbsListViewItem cbsListViewItem)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"Starting Drag. ColorBandIndex = {cbSelectionLineIndex}. ContentScale: {ContentScale}. PosX: {hitPoint.X}. Original X: {cbSelectionLine.SelectionLinePosition}.");
			//ReportColorBandRectanglesInPlay(cbSelectionLineIndex.Value);

			var colorBandIndex = cbsListViewItem.CbsSelectionLine.ColorBandIndex;
			var cbSelectionLine = cbsListViewItem.CbsSelectionLine;
			_selectionLineBeingDragged = cbSelectionLine;
			_selectionLineBeingDragged.SelectionLineMoved += HandleSelectionLineMoved;

			//Debug.WriteIf(_useDetailedDebug, $"CbsListView. Starting Drag for cbSelectionLine at index: {cbSelectionLineIndex}, Current View Position: {cbsView.CurrentPosition}, ColorBand: {cb}.");
			Debug.WriteIf(_useDetailedDebug, $"CbsListView. Starting Drag for cbSelectionLine at index: {colorBandIndex}, ColorBand: {cbsListViewItem.ColorBand}.");

			var gLeft = ListViewItems[colorBandIndex].CbsRectangle.RectangleGeometry;
			var gRight = ListViewItems[colorBandIndex + 1].CbsRectangle.RectangleGeometry;

			cbSelectionLine.StartDrag(gLeft.Rect.Width, gRight.Rect.Width);
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
					UpdateCutoff(e);
					break;

				case CbsSelectionLineDragOperation.Complete:
					_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
					_selectionLineBeingDragged = null;

					Debug.WriteLineIf(_useDetailedDebug, "Completing the SelectionBand Drag Operation.");
					UpdateCutoff(e);
					break;

				case CbsSelectionLineDragOperation.Cancel:
					_selectionLineBeingDragged.SelectionLineMoved -= HandleSelectionLineMoved;
					_selectionLineBeingDragged = null;

					UpdateCutoff(e);
					break;

				default:
					throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			CurrentColorBand = (ColorBand)_colorBandsView.CurrentItem;
		}

		private void ColorBandsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView::ColorBands_CollectionChanged. Action: {e.Action}, New Starting Index: {e.NewStartingIndex}, Old Starting Index: {e.OldStartingIndex}");

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				Debug.WriteLine($"CbsListView is Resetting.");

				//Reset();
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				// Add items
				var bands = e.NewItems?.Cast<ColorBand>() ?? new List<ColorBand>();
				foreach (var colorBand in bands)
				{
					Debug.WriteLine($"CbsListView is Adding a ColorBand: {colorBand}.");

					var idx = e.NewStartingIndex;

					var listViewItem = CreateListViewItem(_colorBandsView, idx, colorBand, showSectionLine: MouseIsEntered);

					ListViewItems.Insert(e.NewStartingIndex, listViewItem);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				// Remove items
				var bands = e.OldItems?.Cast<ColorBand>() ?? new List<ColorBand>();
				foreach (var colorBand in bands)
				{
					Debug.WriteLine($"CbsListView is Removing a ColorBand: {colorBand}");
				}
			}
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

		#endregion

		#region ColorBand Support

		private void UpdateCutoff(CbsSelectionLineMovedEventArgs e)
		{
			var cbView = _colorBandsView;

			if (cbView == null)
				return;

			var newCutoff = (int)Math.Round(e.NewXPosition / ContentScale.Width);

			var colorBandToUpdate = ListViewItems[e.ColorBandIndex].ColorBand;

			//var colorBandToUpdateOld = GetColorBandAt(cbView, e.ColorBandIndex);
			//Debug.Assert(colorBandToUpdate == colorBandToUpdateOld, "Color Band Mismatch from CbsSelectionLineMovedEventArgs.");

			if (e.Operation == CbsSelectionLineDragOperation.Move)
			{
				if (!colorBandToUpdate.IsInEditMode)
				{
					colorBandToUpdate.BeginEdit();
				}

				Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Move Operation at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Complete)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Operation=Complete at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.EndEdit();
			}
			else if (e.Operation == CbsSelectionLineDragOperation.Cancel)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. Updating ColorBand for Operation=Cancel at index: {e.ColorBandIndex} with new Cutoff: {newCutoff}, {colorBandToUpdate}.");
				colorBandToUpdate.Cutoff = newCutoff;
				colorBandToUpdate.CancelEdit();
			}
			else
			{
				throw new InvalidOperationException($"The {e.Operation} CbsSelectionLineDragOperation is not supported.");
			}
		}

		private void UpdateSelectionLinePosition(int colorBandIndex, int newCutoff)
		{
			if (ListViewItems.Count == 0)
			{
				return;
			}

			if (colorBandIndex < 0 || colorBandIndex > ListViewItems.Count - 2)
			{
				throw new InvalidOperationException($"CbsListView::UpdateSelectionLinePosition. The ColorBandIndex must be between 0 and {ListViewItems.Count - 1}, inclusive.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsListView. About to call SelectionLine::UpdatePosition. Index = {colorBandIndex}");

			var selectionLine = ListViewItems[colorBandIndex].CbsSelectionLine;

			if (ScreenTypeHelper.IsDoubleChanged(newCutoff, selectionLine.XPosition))
			{
				var cbsRectangleLeft = ListViewItems[colorBandIndex].CbsRectangle;
				var cbsRectangleRight = ListViewItems[colorBandIndex + 1].CbsRectangle;

				Debug.Assert(cbsRectangleLeft.XPosition + cbsRectangleLeft.Width == selectionLine.XPosition);
				Debug.Assert(cbsRectangleRight.XPosition == selectionLine.XPosition);

				var diff = newCutoff - selectionLine.XPosition;

				selectionLine.XPosition = newCutoff;

				cbsRectangleLeft.Width += diff;

				cbsRectangleRight.XPosition = newCutoff;
				cbsRectangleRight.Width -= diff;
			}
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

		#region Selection Line Support

		private bool TryGetSelectionLine(Point hitPoint, List<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsListViewItem? cbsListViewItem, [NotNullWhen(true)] out int? selectionLineIndex)
		{
			cbsListViewItem = null;
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
						cbsListViewItem = cbsListViewItems[cbsLinePtr];

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

		private bool TryGetColorBandRectangle(Point hitPoint, IList<CbsListViewItem> cbsListViewItems, [NotNullWhen(true)] out CbsListViewItem? cbsListViewItem, [NotNullWhen(true)] out int? colorBandRectangleIndex)
		{
			cbsListViewItem = null;
			colorBandRectangleIndex = null;

			for (var i = 0; i < cbsListViewItems.Count; i++)
			{
				var cbRectangle = cbsListViewItems[i].CbsRectangle;

				if (cbRectangle.ContainsPoint(hitPoint))
				{
					colorBandRectangleIndex = i;
					cbsListViewItem = cbsListViewItems[i];

					Debug.Assert(cbsListViewItem.CbsRectangle.ColorBandIndex == i, "CbsListViewItems ColorBandIndex Mismatch.");
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
				_colorBandLayoutViewModel, _canvas, OffsetIsSelectedChanged, isVisible, HandleContextMenuDisplayRequested);

			var listViewItem = new CbsListViewItem(colorBand, cbsRectangle, cbsSelectionLine);

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
				listViewItem.TearDown();
			}

			ListViewItems.Clear();

			//Debug.WriteLine($"After remove ColorBandRectangles. The DrawingGroup has {_drawingGroup.Children.Count} children. The height of the drawing group is: {_drawingGroup.Bounds.Height} and the location is: {_drawingGroup.Bounds.Location}");
		}

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

		#endregion

		#region Event Handlers

		private void ColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			Debug.WriteLine($"CbsListView is handling a ColorBand Property Change.");

			if (sender is ColorBand cb)
			{
				if (e.PropertyName == "Cutoff")
				{
					// This ColorBand had its Cutoff updated.
					CbsRectangle.Width = cb.Cutoff - (cb.PreviousCutoff ?? 0);
				}
				else
				{
					if (e.PropertyName == "PreviousCutoff")
					{
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
