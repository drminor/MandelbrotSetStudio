using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class SelectionRectangle
	{
		#region Private Fields

		//private readonly static bool USE_INTEGER_FOR_PITCH = false;

		//private const int DEFAULT_SELECTION_SIZE_INDEX = 4; // Translates to 1/2^4 or 1/16 or 6.25% of the display size = 64 pixels for a display size of 1024
		private const int DEFAULT_SELECTION_SIZE_INDEX = 3; // Translates to 1/2^3 or 1/8 or 12.5% of the display size = 128 pixels for a display size of 1024

		private const int PITCH_TARGET = 16;
		private const int DRAG_TRIGGER_DIST = 3;

		private readonly Canvas _canvas;

		private SizeDbl _displaySize;

		private int _pitch;
		private SizeDbl _defaultSelectionSize;

		private readonly Rectangle _selectedArea;
		private readonly Line _dragLine;

		private bool _selecting;
		private DragState _dragState;

		private Point _dragAnchor;
		private bool _haveMouseDown;

		private int _selectedSizeIndex;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeDbl displaySize, SizeInt blockSize)
		{
			_dragState = DragState.None;

			_canvas = canvas;
			_displaySize = displaySize;

			_pitch = RMapHelper.CalculatePitch(_displaySize, PITCH_TARGET);

			_selectedSizeIndex = DEFAULT_SELECTION_SIZE_INDEX;
			var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);

			_defaultSelectionSize = GetSelectionSize(displaySize, percentageOfDisplaySize);
			_selectedArea = BuildSelectionRectangle(_canvas, _defaultSelectionSize);

			SelectedPosition = new Point();

			_dragLine = BuildDragLine(_canvas);

			_selectedArea.KeyUp += SelectedArea_KeyUp;
			_dragLine.KeyUp += DragLine_KeyUp;

			canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
			canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;

			canvas.MouseWheel += Canvas_MouseWheel;
			canvas.MouseMove += Canvas_MouseMove;

			canvas.MouseEnter += Canvas_MouseEnter;
			canvas.MouseLeave += Canvas_MouseLeave;

			canvas.Focusable = true;
		}

		private Rectangle BuildSelectionRectangle(Canvas canvas, SizeDbl selectionSize)
		{
			//_defaultSelectionSize = GetDefaultSelectionSize(canvas, _blockSize.Width);

			var result = new Rectangle()
			{
				Width = selectionSize.Width,
				Height = selectionSize.Height,
				Fill = Brushes.Transparent,
				Stroke = DrawingHelper.BuildSelectionDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 10);

			return result;
		}

		private Line BuildDragLine(Canvas canvas)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = DrawingHelper.BuildSelectionDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 20);

			return result;
		}

		#endregion

		#region Events

		internal event EventHandler<AreaSelectedEventArgs>? AreaSelected;
		internal event EventHandler<ImageDraggedEventArgs>? ImageDragged;

		#endregion

		#region Public Properties

		public bool IsEnabled { get; set; }

		// Same as ContentViewportSize - i.e., logical size or canvas size, not the size of the container
		public SizeDbl DisplaySize
		{
			get => _displaySize;

			set
			{
				var previousValue = _displaySize;
				_displaySize = value;

				if (!_displaySize.IsNAN() & !_displaySize.IsNearZero())
				{
					//(_pitch, _defaultSelectionSize) = CalculatePitchAndDefaultSelectionSize(_displaySize, PITCH_TARGET);

					_pitch = RMapHelper.CalculatePitch(_displaySize, PITCH_TARGET);

					_selectedSizeIndex = DEFAULT_SELECTION_SIZE_INDEX;
					var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);

					_defaultSelectionSize = GetSelectionSize(_displaySize, percentageOfDisplaySize);

					Debug.WriteLineIf(_useDetailedDebug, $"SelectionRectangle is having its DisplaySize updated from: {previousValue} to {value}. The new SelectionSize is {_defaultSelectionSize}.");

					_selectedArea.Width = _defaultSelectionSize.Width;
					_selectedArea.Height = _defaultSelectionSize.Height;
				}
			}
		}

		public RectangleDbl Area
		{
			get
			{
				var p = SelectedPosition;
				var s = SelectedSize;
				var x = new Rect(p, s);
				var result = ScreenTypeHelper.ConvertToRectangleDbl(x);

				return result;
			}
		}

		public VectorInt ZoomPoint
		{
			get
			{
				var selectionCenter = Area.GetCenter();

				var startP = new PointDbl(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);
				//var endP = new PointDbl(selectionCenter.X, _canvas.ActualHeight - selectionCenter.Y);
				var endP = new PointDbl(selectionCenter.X, selectionCenter.Y);
				var vectorDbl = endP.Diff(startP);
				var result = vectorDbl.Round();

				return result;

			}
		}

		#endregion

		#region Private Properties

		private bool Selecting
		{
			get => _selecting;

			set
			{
				if (_selecting != value)
				{
					if (value)
					{
						_selectedArea.Visibility = Visibility.Visible;
						_selectedArea.Focus();
					}
					else
					{
						_selectedArea.Visibility = Visibility.Hidden;
						_selectedArea.Width = _defaultSelectionSize.Width;
						_selectedArea.Height = _defaultSelectionSize.Height;

						AreaSelected?.Invoke(this, AreaSelectedEventArgs.CreateCancelPreviewInstance(TransformType.ZoomIn));
					}

					_selecting = value;
				}
			}
		}

		private DragState DragState
		{
			get => _dragState;
			set
			{
				if (_dragState != value)
				{
					if (value == DragState.None)
					{
						_dragLine.Visibility = Visibility.Hidden;
					}
					else if (value == DragState.Begun)
					{
						_dragLine.Visibility = Visibility.Visible;
						_dragLine.Focus();
					}
					else
					{
						_dragLine.Visibility = Visibility.Visible;

						// TODO: Check This -- is this needed?>
						_dragLine.Focus();
					}

					_dragState = value;
				}
			}
		}

		private Point SelectedPosition
		{
			get
			{
				var x = (double)_selectedArea.GetValue(Canvas.LeftProperty);
				var y = (double)_selectedArea.GetValue(Canvas.BottomProperty);

				return new Point(double.IsNaN(x) ? 0 : x, double.IsNaN(y) ? 0 : y);
			}

			set
			{
				_selectedArea.SetValue(Canvas.LeftProperty, value.X);
				_selectedArea.SetValue(Canvas.BottomProperty, value.Y);
			}
		}

		private Size SelectedSize
		{
			get
			{
				var result = new Size(_selectedArea.Width, _selectedArea.Height);
				//Debug.WriteLine($"The selected size is {result}.");
				return result;
			}

			set
			{
				//Debug.WriteLine($"The selected size is being updated to {value}.");
				_selectedArea.Width = value.Width;
				_selectedArea.Height = value.Height;
			}
		}

		private Point DragLineTerminus
		{
			get => new(_dragLine.X2, _dragLine.Y2);
			set
			{
				_dragLine.X2 = value.X;
				_dragLine.Y2 = value.Y;
			}
		}

		#endregion

		#region Public Methods

		public void TearDown()
		{
			try
			{
				//_mapDisplayViewModel.PropertyChanged -= MapDisplayViewModel_PropertyChanged;

				_canvas.MouseLeftButtonUp -= Canvas_MouseLeftButtonUp;
				_canvas.MouseLeftButtonDown -= Canvas_MouseLeftButtonDown;

				_canvas.MouseWheel -= Canvas_MouseWheel;
				_canvas.MouseMove -= Canvas_MouseMove;

				_canvas.MouseEnter -= Canvas_MouseEnter;
				_canvas.MouseLeave -= Canvas_MouseLeave;
			}
			catch
			{
				Debug.WriteLine("SelectionRectangle encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Event Handlers

		private void SelectedArea_KeyUp(object sender, KeyEventArgs e)
		{
			//Debug.WriteLine($"The {e.Key} was pressed on the Selected Area.");

			if (!Selecting)
			{
				//Debug.WriteLine($"The {e.Key} was pressed, but we are not active, returning.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				Selecting = false;
			}
		}

		private void DragLine_KeyUp(object sender, KeyEventArgs e)
		{
			if (DragState == DragState.None)
			{
				//Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- not in drag.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				//Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- cancelling drag.");
				DragState = DragState.None;

				ImageDragged?.Invoke(this, ImageDraggedEventArgs.CreateCancelPreviewInstance(TransformType.Pan));
			}
		}

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!Selecting)
			{
				return;
			}

			if (e.Delta < 0)
			{
				// Reverse roll, zooms out.
				if (TryGetNewSelection(ref _selectedSizeIndex, zoomIn: false, out var newSelection))
				{
					_ = MoveAndSize(newSelection.Value);
					e.Handled = true;
				}
			}
			else if (e.Delta > 0)
			{
				// Forward roll, zooms in.
				if (TryGetNewSelection(ref _selectedSizeIndex, zoomIn: true, out var newSelection))
				{
					_ = MoveAndSize(newSelection.Value);
					e.Handled = true;
				}
			}
			else
			{
				//Debug.WriteLine("MouseWheel, but no change.");
			}
		}

		private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_haveMouseDown = true;

			if (DragState == DragState.None)
			{
				_dragAnchor = e.GetPosition(relativeTo: _canvas);
			}
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (Selecting)
			{
				HandleSelectionMove(e);
			}
			else
			{
				HandleDragMove(e);
			}
		}

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!IsEnabled)
			{
				//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event -- we are disabled.");
				return;
			}

			//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event. IsFocused = {_canvas.IsFocused}. Have a mouse down event = {_haveMouseDown}, DragState = {DragState}, Selecting = {Selecting}");

			if (DragState == DragState.None)
			{
				if (_haveMouseDown)
				{
					_haveMouseDown = false;
					HandleSelectionRect(e);
				}
			}
			else
			{
				HandleDragLine(e);
			}
		}

		private void Canvas_MouseLeave(object sender, MouseEventArgs e)
		{
			//_haveMouseDown = false;
			if (Selecting)
			{
				_selectedArea.Visibility = Visibility.Hidden;
			}

			else if (DragState == DragState.Begun)
			{
				DragState = DragState.None;
				_dragLine.Visibility = Visibility.Hidden;
			}

			else if (DragState == DragState.InProcess)
			{
				_dragLine.Visibility = Visibility.Hidden;
			}
		}

		private void Canvas_MouseEnter(object sender, MouseEventArgs e)
		{
			if (Selecting)
			{
				_selectedArea.Visibility = Visibility.Visible;

				if (!_selectedArea.Focus())
				{
					Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the SelectedRectangle.");
				}
			}
			
			else if (DragState != DragState.None)
			{
				_dragLine.Visibility = Visibility.Visible;

				if (!_dragLine.Focus())
				{
					Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the DragLine.");
				}
			}
		}

		#endregion

		#region Private Methods

		private void HandleSelectionMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// Invert the y coordinate.
			var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			Move(posYInverted);
		}

		private void HandleSelectionRect(MouseButtonEventArgs e)
		{
			// The controlPos is the center of the selection Rectangle
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// The canvas has coordinates where the y value increases from top to bottom.
			var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);

			if (!Selecting)
			{
				//Debug.WriteLine($"Activating Select at {posYInverted}.");
				Activate(posYInverted);
			}
			else
			{
				if (Contains(posYInverted))
				{
					//Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = True.");

					var area = StopSelecting();
					RaiseAreaSelected(area, isPreview: false);
				}
				else
				{
					//Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = False.");
				}
			}
		}

		private void Activate(Point position)
		{
			Selecting = true;
			Move(position);

			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("WARNING: Activate did not move the focus to the SelectedRectangle");
			}
		}

		private RectangleDbl StopSelecting()
		{
			var snapShotOfArea = Area;
			Selecting = false;

			return snapShotOfArea;
		}

		private bool Contains(Point position)
		{
			var p = SelectedPosition;
			var s = SelectedSize;
			var r = new Rect(p, s);

			var result = r.Contains(position);

			//var strResult = result ? "is contained" : "is not contained";
			//Debug.WriteLine($"Checking {p} to see if it contained by {r} and it {strResult}.");

			return result;
		}

		// Reposition the Selection Rectangle, keeping it's current size.
		// posYInverted is the lower, left-hand corner of the selection rectangle
		private void Move(Point posYInverted)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");
			//ReportPosition(posYInverted);

			//var x = DoubleHelper.RoundOff(posYInverted.X - (_selectedArea.Width / 2), _pitch);
			//var y = DoubleHelper.RoundOff(posYInverted.Y - (_selectedArea.Height / 2), _pitch);

			var x = posYInverted.X - (_selectedArea.Width / 2);
			var y = posYInverted.Y - (_selectedArea.Height / 2);

			if (x < 0)
			{
				x = 0;
			}

			if (x + _selectedArea.Width > _canvas.ActualWidth)
			{
				x = _canvas.ActualWidth - _selectedArea.Width;
			}

			if (y < 0)
			{
				y = 0;
			}

			if (y + _selectedArea.Height > _canvas.ActualHeight)
			{
				y = _canvas.ActualHeight - _selectedArea.Height;
			}

			// x and y are the lower, left-hand point of the selection rectangle

			if (ScreenTypeHelper.IsDoubleChanged(x, SelectedPosition.X, 0.01) || ScreenTypeHelper.IsDoubleChanged(y, SelectedPosition.Y, 0.01))
			//if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01 || double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			{
				//SelectedCenterPosition = posYInverted;
				SelectedPosition = new Point(x, y);

				RaiseAreaSelected(Area, isPreview: true);
			}
		}

		// Reposition the Selection Rectangle and update its size.
		// returns true if the SelectedPosition was updated
		// posYInverted in the lower, left point of the rectangle.
		private bool MoveAndSize(RectangleDbl selection)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			var posYInverted = selection.Position;
			var size = selection.Size;

			if (posYInverted.X < 0
				|| posYInverted.Y < 0
				|| posYInverted.X + size.Width > _canvas.ActualWidth
				|| posYInverted.Y + size.Height > _canvas.ActualHeight)
			{
				return false;
			}

			var selectedPositionWasUpdated = false;
			//var cPos = SelectedPosition;

			if (ScreenTypeHelper.IsDoubleChanged(posYInverted.X, SelectedPosition.X) || ScreenTypeHelper.IsDoubleChanged(posYInverted.Y, SelectedPosition.Y))
			///if (double.IsNaN(cPos.X) || Math.Abs(posYInverted.X - cPos.X) > 0.01 || double.IsNaN(cPos.Y) || Math.Abs(posYInverted.Y - cPos.Y) > 0.01)
			{
				SelectedPosition = ScreenTypeHelper.ConvertToPoint(posYInverted);
				selectedPositionWasUpdated = true;
			}

			var selectedSizeWasUpdated = false;
			//var cSize = SelectedSize;
			//if (Math.Abs(size.Width - cSize.Width) > 0.01 || Math.Abs(size.Height - cSize.Height) > 0.01)
			if (ScreenTypeHelper.IsDoubleChanged(size.Width, SelectedSize.Width, 0.01) || ScreenTypeHelper.IsDoubleChanged(size.Height, SelectedSize.Height))
			{
				var previousSize = SelectedSize;
				SelectedSize = ScreenTypeHelper.ConvertToSize(size);
				selectedSizeWasUpdated = true;

				Debug.WriteLine($"SelectionRectangle is having its SelectionSize updated from: {previousSize} to {size}.");
			}

			if (selectedPositionWasUpdated | selectedSizeWasUpdated)
			{
				RaiseAreaSelected(Area, isPreview: true);
			}

			return selectedPositionWasUpdated;
		}

		private void RaiseAreaSelected(RectangleDbl area, bool isPreview)
		{
			var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
			var zoomPoint = GetDistanceFromCanvasCenter(area, displaySize);
			var factor = RMapHelper.GetSmallestScaleFactor(area.Size, displaySize);
			var areaSelectedEventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, area, displaySize, isPreview);

			var previewMessage = isPreview ? " PREVIEW " : string.Empty;
			Debug.WriteLineIf(_useDetailedDebug, $"Raising AreaSelected {previewMessage} with position: {zoomPoint} and factor: {factor}");

			AreaSelected?.Invoke(this, areaSelectedEventArgs);
		}

		// Return the distance from the Canvas Center to the new mouse position.
		private VectorInt GetDistanceFromCanvasCenter(RectangleDbl area, SizeDbl displaySize)
		{
			Debug.Assert(area.Width > 0 && area.Height > 0, "Selction Rectangle has a zero or negative value its width or height.");

			var canvasCenter = displaySize.Scale(0.5); // new PointDbl(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);
			var selectionCenter = area.GetCenter();
			var vectorDbl = selectionCenter.Sub(canvasCenter);

			var result = vectorDbl.Round();

			return result;
		}

		private void HandleDragMove(MouseEventArgs e)
		{
			if (!IsEnabled)
			{
				return;
			}

			if (DragState == DragState.None)
			{
				if (_haveMouseDown && e.LeftButton == MouseButtonState.Pressed)
				{
					var controlPos = e.GetPosition(relativeTo: _canvas);
					var dist = _dragAnchor - controlPos;

					if (Math.Abs(dist.Length) > DRAG_TRIGGER_DIST)
					{
						_dragLine.X1 = _dragAnchor.X;
						_dragLine.Y1 = _dragAnchor.Y;
						_dragLine.X2 = _dragAnchor.X;
						_dragLine.Y2 = _dragAnchor.Y;

						DragState = DragState.Begun;
						_haveMouseDown = false;
						_dragLine.Focus();
					}
				}
			}
			else
			{
				var controlPos = e.GetPosition(relativeTo: _canvas);
				SetDragPosition(controlPos);
			}
		}

		private void HandleDragLine(MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			if (DragState == DragState.Begun)
			{
				DragState = DragState.InProcess;
			}
			else
			{
				DragState = DragState.None;
				var offset = GetDragOffset(DragLineTerminus);
				ImageDragged?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: false));
			}
		}

		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			var dist = controlPos - _dragAnchor;

			// Horizontal
			//var x = DoubleHelper.RoundOff(dist.X, _pitch);
			var x = dist.X;

			x = _dragAnchor.X + x;

			if (x < 0)
			{
				x += _pitch;
			}

			if (x > _canvas.ActualWidth)
			{
				x -= _pitch;
			}

			// Vertical
			//var y = DoubleHelper.RoundOff(dist.Y, _pitch);
			var y = dist.Y;

			y = _dragAnchor.Y + y;

			if (y < 0)
			{
				y += _pitch;
			}

			if (y > _canvas.ActualWidth)
			{
				y -= _pitch;
			}

			var dragLineTerminus = DragLineTerminus;
			var newDlt = new Point(x, y);

			if ( (dragLineTerminus - newDlt).LengthSquared > 0.05 )
			{
				DragLineTerminus = newDlt;

				var offset = GetDragOffset(DragLineTerminus);
				ImageDragged?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: true));
			}
		}

		// Return the distance from the DragAnchor to the new mouse position.
		private VectorInt GetDragOffset(Point controlPos)
		{
			var startP = new PointDbl(_dragAnchor.X, _canvas.ActualHeight - _dragAnchor.Y);
			var endP = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			var vectorDbl = endP.Diff(startP);
			var result = vectorDbl.Round();

			return result;
		}

		private bool TryGetNewSelection(ref int selectedSizeIndex, bool zoomIn, [NotNullWhen(true)] out RectangleDbl? newSelection)
		{
			SizeDbl newSize;

			if (zoomIn)
			{
				var newSelectionSizeIndex = selectedSizeIndex + 1;

				var percentageOfDisplaySize = Math.Pow(2, -1 * newSelectionSizeIndex);
				newSize = GetSelectionSize(DisplaySize, percentageOfDisplaySize);

				if (newSize.Width <= 8 || newSize.Height <= 8)
				{
					// Cannot zoom in any futher.
					newSelection = null;
					return false;
				}

				selectedSizeIndex = newSelectionSizeIndex;
			}
			else
			{
				if (_selectedSizeIndex == 0)
				{
					// Cannot zoom out any further.
					newSelection = null;
					return false;
				}

				_selectedSizeIndex--;

				var percentageOfDisplaySize = Math.Pow(2, -1 * _selectedSizeIndex);
				newSize = GetSelectionSize(DisplaySize, percentageOfDisplaySize);
			}

			var pos = new PointDbl(SelectedPosition.X - newSize.Width / 2, SelectedPosition.Y - newSize.Height / 2);
			newSelection = new RectangleDbl(pos, newSize);
			return true;
		}

		private SizeDbl GetSelectionSize(SizeDbl displaySize, double percentageOfDisplaySize)
		{
			SizeDbl result;

			if (displaySize.IsNAN() || displaySize.IsNearZero())
			{
				return new SizeDbl(128);
			}

			if (percentageOfDisplaySize == 1)
			{
				return displaySize;
			}

			var w = displaySize.Width;
			var h = displaySize.Height;

			if (w >= h)
			{
				var adjustedHeight = RoundToNearestMulOf16(h);
				var selectedHeight = adjustedHeight * percentageOfDisplaySize;
				var selectedWidth = selectedHeight * (w / h);
				result = new SizeDbl(selectedWidth, selectedHeight);
			}
			else
			{
				var adjustedWidth = RoundToNearestMulOf16(w);
				var selectedWidth = adjustedWidth * percentageOfDisplaySize;
				var selectedHeight = selectedWidth * (h / w);
				result = new SizeDbl(selectedWidth, selectedHeight);
			}

			return result;
		}

		private double RoundToNearestMulOf16(double s)
		{
			var result = DoubleHelper.RoundOff(s, 16);
			return result;
		}

		#endregion

		#region Not Used

		//private void SetMousePosition(Point posYInverted)
		//{
		//	var position = new Point(posYInverted.X, _canvas.ActualHeight - posYInverted.Y);
		//	var canvasPos = GetCanvasPosition();
		//	var pos = new Point(position.X + canvasPos.X, position.Y + canvasPos.Y);

		//	var source = (HwndSource)PresentationSource.FromVisual(_canvas);
		//	var hWnd = source.Handle; 
		//	var _ = Win32.PositionCursor(hWnd, pos);

		//	//Debug.WriteLine($"Activating to canvas:{position}, inv:{posYInverted}, screen:{screenPos}");
		//}

		//private Point GetCanvasPosition()
		//{
		//	var generalTransform = _canvas.TransformToAncestor(Application.Current.MainWindow);
		//	var relativePoint = generalTransform.Transform(new Point(0, 0));

		//	return relativePoint;
		//}

		//private void ReportPosition(Point posYInverted)
		//{
		//	var position = new Point(posYInverted.X, _canvas.ActualHeight - posYInverted.Y);
		//	var canvasPos = GetCanvasPosition();
		//	var pos = new Point(position.X + canvasPos.X, position.Y + canvasPos.Y);

		//	HwndSource source = (HwndSource)PresentationSource.FromVisual(_canvas);
		//	IntPtr hWnd = source.Handle;
		//	var screenPos = Win32.TranslateToScreen(hWnd, pos);

		//	Debug.WriteLine($"Mouse moved to canvas:{position}, inv:{posYInverted}, screen:{screenPos}");
		//}

		//private bool IsShiftKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
		//}

		//private bool IsCtrlKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		//}

		//private bool IsAltKey()
		//{
		//	return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
		//}

		//private void SelectedArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//	var position = e.GetPosition(relativeTo: _canvas);

		//	Debug.WriteLine($"The SelectionRectangle is getting a Mouse Left Button Down at {position}.");
		//}

		//private void SelectedArea_MouseWheel(object sender, MouseWheelEventArgs e)
		//{
		//	Debug.WriteLine("The SelectionRectangle received a MouseWheel event.");
		//}

		#endregion
	}
}
