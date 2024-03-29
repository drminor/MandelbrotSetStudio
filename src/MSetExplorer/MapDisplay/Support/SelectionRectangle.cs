﻿using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
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

		private const int PITCH_TARGET = 16;
		private const int DRAG_TRIGGER_DIST = 3;
		private const int DEFAULT_SELECTION_SIZE_FACTOR = 8; // Amount to multiply actual pitch by to get the default side length of the selection rectangle.

		private readonly Canvas _canvas;
		private readonly SizeInt _blockSize;

		private SizeDbl _displaySize;

		private int _pitch;
		private Size _defaultSelectionSize;

		private readonly Rectangle _selectedArea;
		private readonly Line _dragLine;

		private bool _selecting;
		private DragState _dragState;

		private Point _dragAnchor;
		private bool _haveMouseDown;

		#endregion

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeDbl displaySize, SizeInt blockSize)
		{
			_dragState = DragState.None;

			_canvas = canvas;
			_displaySize = displaySize;
			_blockSize = blockSize;

			(_pitch, _defaultSelectionSize) = CalculatePitchAndDefaultSelectionSize(_displaySize, PITCH_TARGET);

			_selectedArea = BuildSelectionRectangle(_canvas);
			SelectedPosition = new Point();
			SelectedCenterPosition = new Point();
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

		private Rectangle BuildSelectionRectangle(Canvas canvas)
		{
			_defaultSelectionSize = GetDefaultSelectionSize(canvas, _blockSize.Width);

			var result = new Rectangle()
			{
				Width = _defaultSelectionSize.Width,
				Height = _defaultSelectionSize.Height,
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
				_displaySize = value;

				if (!_displaySize.IsNAN() & !_displaySize.IsNearZero())
				{
					(_pitch, _defaultSelectionSize) = CalculatePitchAndDefaultSelectionSize(_displaySize, PITCH_TARGET);

					//var selection = Expand(SelectedPosition, _defaultSelectionSize, 0);
					//_defaultSelectionSize.Width = selection.Width;
					//_defaultSelectionSize.Height = selection.Height;

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

		private Point SelectedCenterPosition { get; set; }

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

			//Debug.WriteLine("The canvas received a MouseWheel event.");

			var controlPos = e.GetPosition(relativeTo: _canvas);
			var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);

			var cSize = SelectedSize;

			Rect selection;

			if (e.Delta < 0)
			{
				// Reverse roll, zooms out.
				//selection = Expand(SelectedPosition, SelectedSize, PITCH_TARGET);
				selection = Expand(SelectedPosition, SelectedSize, _pitch);
			}
			else if (e.Delta > 0 && cSize.Width >= _pitch * 4 && cSize.Height >= _pitch * 4)
			{
				// Forward roll, zooms in.
				//selection = Expand(SelectedPosition, SelectedSize, -1 * PITCH_TARGET);
				selection = Expand(SelectedPosition, SelectedSize, -1 * _pitch);
			}
			else
			{
				//Debug.WriteLine("MouseWheel, but no change.");
				return;
			}

			var selectedPositionWasUpdated = MoveAndSize(selection.Location, selection.Size);

			if (selectedPositionWasUpdated)
			{
				SelectedCenterPosition = posYInverted;
			}

			e.Handled = true;
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

		private void HandleSelectionMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// Invert the y coordinate.
			var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			Move(posYInverted);
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

		private void HandleSelectionRect(MouseButtonEventArgs e)
		{
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
					var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);

					var (zoomPoint, factor) = GetAreaSelectedParams(area, displaySize);
					var eventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, area, displaySize, isPreview: false);

					Debug.WriteLine($"Raising AreaSelected with position: {zoomPoint} and factor: {factor}.");

					AreaSelected?.Invoke(this, eventArgs);
				}
				else
				{
					//Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = False.");
				}
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

		private void Activate(Point position)
		{
			Selecting = true;
			Move(position);

			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("WARNING: Activate did not move the focus to the SelectedRectangle");
			}
		}

		#endregion

		#region Private Methods

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

		// Return the distance from the DragAnchor to the new mouse position.
		private VectorInt GetDragOffset(Point controlPos)
		{
			var startP = new PointDbl(_dragAnchor.X, _canvas.ActualHeight - _dragAnchor.Y);
			var endP = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			var vectorDbl = endP.Diff(startP);
			var result = vectorDbl.Round();

			return result;
		}

		// Return the distance from the Canvas Center to the new mouse position.
		private VectorInt GetCenterOffset(PointDbl selectionCenter)
		{
			var startP = new PointDbl(_canvas.ActualWidth / 2, _canvas.ActualHeight / 2);

			//var endP = new PointDbl(selectionCenter.X, _canvas.ActualHeight - selectionCenter.Y);
			var endP = new PointDbl(selectionCenter.X, selectionCenter.Y);

			var vectorDbl = endP.Diff(startP);
			
			var result = vectorDbl.Round();

			return result;
		}

		// Reposition the Selection Rectangle, keeping it's current size.
		private void Move(Point posYInverted)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");
			//ReportPosition(posYInverted);

			var x = DoubleHelper.RoundOff(posYInverted.X - (_selectedArea.Width / 2), _pitch);
			var y = DoubleHelper.RoundOff(posYInverted.Y - (_selectedArea.Height / 2), _pitch);

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

			var cLeft = (double)_selectedArea.GetValue(Canvas.LeftProperty);
			var cBot = (double)_selectedArea.GetValue(Canvas.BottomProperty);

			if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01 || double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			{
				SelectedCenterPosition = posYInverted;
				SelectedPosition = new Point(x, y);

				var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
				var (zoomPoint, factor) = GetAreaSelectedParams(Area, displaySize);
				var eventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, Area, displaySize, isPreview: true);

				//Debug.WriteLine($"Raising AreaSelected PREVIEW with position: {zoomPoint} and factor: {factor}");

				AreaSelected?.Invoke(this, eventArgs);
			}
		}

		// Reposition the Selection Rectangle and update its size.
		// returns true if the SelectedPosition was updated
		private bool MoveAndSize(Point posYInverted, Size size)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			if (posYInverted.X < 0
				|| posYInverted.Y < 0
				|| posYInverted.X + size.Width > _canvas.ActualWidth
				|| posYInverted.Y + size.Height > _canvas.ActualHeight)
			{
				return false;
			}

			var selectedPositionWasUpdated = false;
			var cPos = SelectedPosition;
			if (double.IsNaN(cPos.X) || Math.Abs(posYInverted.X - cPos.X) > 0.01 || double.IsNaN(cPos.Y) || Math.Abs(posYInverted.Y - cPos.Y) > 0.01)
			{
				SelectedPosition = posYInverted;
				selectedPositionWasUpdated = true;
			}

			var selectedSizeWasUpdated = false;
			var cSize = SelectedSize;
			if (Math.Abs(size.Width - cSize.Width) > 0.01 || Math.Abs(size.Height - cSize.Height) > 0.01)
			{
				SelectedSize = size;
				selectedSizeWasUpdated = true;
			}

			if (selectedPositionWasUpdated | selectedSizeWasUpdated)
			{
				// TODO: When invoking the AreaSelected event from the SelectionRectangle class, should we use the DisplaySize property instead of the actual canvas size.


				var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
				var (zoomPoint, factor) = GetAreaSelectedParams(Area, displaySize);
				var eventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, Area, displaySize, isPreview: true);

				//Debug.WriteLine($"Raising AreaSelected PREVIEW with position: {zoomPoint} and factor: {factor}");

				AreaSelected?.Invoke(this, eventArgs);
			}

			return selectedPositionWasUpdated;
		}

		private (VectorInt zoomPoint, double factor) GetAreaSelectedParams(RectangleDbl area, SizeDbl displaySize)
		{
			Debug.Assert(area.Width > 0 && area.Height > 0, "Selction Rectangle has a zero or negative value its width or height.");

			var selectionCenter = area.GetCenter();
			var zoomPoint = GetCenterOffset(selectionCenter);

			CheckSelectedCenterPosition(selectionCenter);

			//var xFactor = _canvas.ActualWidth / area.Width;
			//var yFactor = _canvas.ActualHeight / area.Height;
			//var factor = Math.Min(xFactor, yFactor);

			var factor = RMapHelper.GetSmallestScaleFactor(area.Size, displaySize);

			var factor2D = displaySize.Divide(area.Size);
			var factorCheck = Math.Min(factor2D.Width, factor2D.Height);

			Debug.Assert(factorCheck == factor, "GetSmallestScaleFactor is not the same.");

			return (zoomPoint, factor);
		}

		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			var dist = controlPos - _dragAnchor;

			// Horizontal
			var x = DoubleHelper.RoundOff(dist.X, _pitch);

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
			var y = DoubleHelper.RoundOff(dist.Y, _pitch);
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

		private Rect Expand(Point p, Size s, double amount)
		{
			Size newSize;
			if (_canvas.Width >= _canvas.Height)
			{
				var w = s.Width + amount * 2;
				var h = w * (_canvas.ActualHeight / _canvas.ActualWidth);
				newSize = new Size(w, h);
			}
			else
			{
				var h = s.Height + amount * 2;
				var w = h * (_canvas.ActualWidth / _canvas.ActualHeight);
				newSize = new Size(w, h);
			}

			var pos = new Point(p.X - newSize.Width / 2, p.Y - newSize.Height / 2);

			return new Rect(pos, newSize);
		}

		private (int pitch, Size selctionSize) CalculatePitchAndDefaultSelectionSize(SizeDbl displaySize, int pitchTarget)
		{
			var dispSizeInt = displaySize.Round();
			var pitch = RMapHelper.CalculatePitch(dispSizeInt, pitchTarget);
			var defaultSideLength = RMapHelper.CalculatePitch(dispSizeInt, pitchTarget * DEFAULT_SELECTION_SIZE_FACTOR);

			var defaultSelectionSize = GetDefaultSelectionSize(_canvas, defaultSideLength);

			Debug.WriteLine($"ScreenSelection Pitch: {pitch}. SelectionSize: {defaultSelectionSize}.");

			return (pitch, defaultSelectionSize);
		}

		//private (double pitch, Size selctionSize) CalculatePitchAndDefaultSelectionSize(SizeDbl displaySize, int pitchTarget)
		//{
		//	double pitch;
		//	double defaultSideLength;

		//	if (USE_INTEGER_FOR_PITCH)
		//	{
		//		var dispSizeInt = displaySize.Round();
		//		pitch = RMapHelper.CalculatePitch(dispSizeInt, pitchTarget);
		//		defaultSideLength = RMapHelper.CalculatePitch(dispSizeInt, pitchTarget * DEFAULT_SELECTION_SIZE_FACTOR);
		//	}
		//	else
		//	{
		//		pitch = RMapHelper.CalculatePitch(displaySize, pitchTarget);
		//		defaultSideLength = RMapHelper.CalculatePitch(displaySize, pitchTarget * DEFAULT_SELECTION_SIZE_FACTOR);
		//	}

		//	var defaultSelectionSize = GetDefaultSelectionSize(_canvas, defaultSideLength);

		//	Debug.WriteLine($"ScreenSelection Pitch: {pitch}. SelectionSize: {defaultSelectionSize}.");

		//	return (pitch, defaultSelectionSize);
		//}

		private Size GetDefaultSelectionSize(Canvas canvas, double defaultSideLength)
		{
			Size result;

			var w = canvas.ActualWidth;
			var h = canvas.ActualHeight;

			if (double.IsNaN(w) || double.IsNaN(h) || w < 0.1 || h < 0.1)
			{
				return new Size(128, 128);
			}

			if (w >= h)
			{
				result = new Size(defaultSideLength, defaultSideLength * (h / w));
			}
			else
			{
				result = new Size(defaultSideLength * (w / h), defaultSideLength);
			}

			return result;
		}

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

		//// The Image Blocks Group may have it origin shifted down and to the left from the canvas's origin.
		//// Convert the point relative to the canvas' origin to coordinates relative to the Image Blocks
		//private Point ConvertToScreenCoords(Point posYInverted)
		//{
		//	var pointDbl = new PointDbl(posYInverted.X, posYInverted.Y);
		//	var screenPos = ConvertToScreenCoords(pointDbl);
		//	var result = new Point(screenPos.X, screenPos.Y);

		//	return result;
		//}

		//// The Image Blocks Group may have it origin shifted down and to the left from the canvas's origin.
		//// Convert the point relative to the canvas' origin to coordinates relative to the Image Blocks
		//private PointDbl ConvertToScreenCoords(PointDbl posYInverted)
		//{
		//	var result = posYInverted.Translate(CanvasControlOffset);

		//	return result;
		//}

		//private Point ConvertToCanvasCoords(Point posYInverted)
		//{
		//	var pointDbl = new PointDbl(posYInverted.X, posYInverted.Y);
		//	var screenPos = ConvertToCanvasCoords(pointDbl);
		//	var result = new Point(screenPos.X, screenPos.Y);

		//	return result;
		//}

		//// The Image Blocks Group may have it origin shifted down and to the left from the canvas's origin.
		//// Convert the point relative to the canvas' origin to coordinates relative to the Image Blocks
		//private PointDbl ConvertToCanvasCoords(PointDbl posYInverted)
		//{
		//	var result = posYInverted.Translate(CanvasControlOffset.Scale(-1d));

		//	return result;
		//}

		#endregion

		#region Diag

		[Conditional("DEBUG2")]
		private void CheckSelectedCenterPosition(PointDbl selectionCenter)
		{
			var selCenterPos = ScreenTypeHelper.ConvertToPointDbl(SelectedCenterPosition);

			if (!ScreenTypeHelper.IsPointDblChanged(selCenterPos, selectionCenter, threshold: 0.001))
			{
				Debug.WriteLine("Yes, we can use the Selected Position, instead of calling area.GetCenter().");
			}
		}

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

		#region Drawing Support

		//private DrawingBrush BuildDrawingBrush()
		//{
		//	var aDrawingGroup = new DrawingGroup();

		//	var inc = 2;
		//	var x = 0;
		//	var y = 0;

		//	aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black)); x += inc;
		//	aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White));

		//	x = 0;
		//	y += inc;
		//	aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White)); x += inc;
		//	aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black));

		//	var result = new DrawingBrush(aDrawingGroup)
		//	{
		//		TileMode = TileMode.Tile,
		//		ViewportUnits = BrushMappingMode.Absolute,
		//		Viewport = new Rect(0, 0, inc * 2, inc * 2)
		//	};

		//	return result;
		//}

		//private GeometryDrawing BuildDot(int x, int y, int size, SolidColorBrush brush)
		//{
		//	var result = new GeometryDrawing(
		//		brush,
		//		new Pen(brush, 0),
		//		new RectangleGeometry(new Rect(new Point(x, y), new Size(size, size)))
		//	);

		//	return result;
		//}

		#endregion
	}
}
