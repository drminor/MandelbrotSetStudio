using MSetExplorer.ScreenHelpers;
using MSS.Common;
using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer.MapWindow
{
	internal class SelectionRectangle
	{
		private const int PITCH = 16;

		private readonly Canvas _canvas;
		private readonly SizeInt _blockSize; // Also used as the initial size of the selection rectangle.
		private readonly Rectangle _selectedArea;
		private readonly Line _dragLine;

		private bool _isActive;
		private bool _inDrag;
		private Point _dragAnchor;

		private bool _dragIsBeingCancelled;

		// TODO: Create new EventArg classes to 
		internal event EventHandler<AreaSelectedEventArgs> AreaSelected;
		internal event EventHandler<ScreenPannedEventArgs> ScreenPanned;

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeDbl canvasControlOffset, SizeInt blockSize)
		{
			_canvas = canvas;
			CanvasControlOffset = canvasControlOffset;
			_blockSize = blockSize;

			_selectedArea = new Rectangle()
			{
				Width = _blockSize.Width,
				Height = _blockSize.Height,
				Fill = Brushes.Transparent,
				Stroke = BuildDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = _canvas.Children.Add(_selectedArea);
			_selectedArea.SetValue(Panel.ZIndexProperty, 10);

			_isActive = false;

			_dragLine = new Line()
				{
					Fill = Brushes.Transparent,
					Stroke = BuildDrawingBrush(),
					StrokeThickness = 4,
					Visibility = Visibility.Hidden
				};

			_ = _canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 20);

			_selectedArea.KeyUp += SelectedArea_KeyUp;

			canvas.PreviewKeyUp += Canvas_PreviewKeyUp;

			canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
			canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
			canvas.MouseWheel += Canvas_MouseWheel;
			canvas.MouseMove += Canvas_MouseMove;

			canvas.MouseEnter += Canvas_MouseEnter;
			canvas.MouseLeave += Canvas_MouseLeave;

			canvas.Focusable = true;
		}

		#endregion

		#region Public Properties

		public SizeDbl CanvasControlOffset { get; set; }

		public RectangleDbl Area
		{
			get
			{
				var p = GetPosition();
				var s = GetSize();
				var result = new RectangleDbl(new PointDbl(p.X, p.Y), new SizeDbl(s.Width, s.Height));

				return result;
			}
		}


		// Return the distance from the DragAnchor to the new mouse position.
		public SizeDbl GetDragOffset(Point controlPos)
		{
			var startP = new PointDbl(_dragAnchor.X, _canvas.ActualHeight - _dragAnchor.Y);
			var endP = new PointDbl(controlPos.X, _canvas.ActualHeight - controlPos.Y);
			var result = endP.Diff(startP);

			return result;
		}

		public bool IsActive
		{
			get => _isActive;

			private set
			{
				if (_isActive != value)
				{
					if (value)
					{
						_selectedArea.Visibility = Visibility.Visible;
					}
					else
					{
						_selectedArea.Visibility = Visibility.Hidden;
						_selectedArea.Width = _blockSize.Width;
						_selectedArea.Height = _blockSize.Height;
					}

					_isActive = value;
				}
			}
		}

		public bool InDrag
		{
			get => _inDrag;

			set
			{
				if (_inDrag != value)
				{
					if (value)
					{
						Mouse.Capture(_canvas);
						_dragLine.Visibility = Visibility.Visible;
						_canvas.Focus();
					}
					else
					{
						_dragLine.Visibility = Visibility.Hidden;
						Mouse.Capture(null);
					}

					_inDrag = value;
				}
			}
		}

		#endregion

		#region Public Methods

		public void Activate(Point position, bool updateCursorPosition = true)
		{
			IsActive = true;

			if (updateCursorPosition)
			{
				SetMousePosition(position);
			}

			Move(position);

			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("Activate did not move the focus to the SelectedRectangle, free form.");
			}
		}

		public void Deactivate()
		{
			IsActive = false;
		}

		public bool Contains(Point position)
		{
			var p = GetPosition();
			var s = GetSize();
			var r = new Rect(p, s);

			var result = r.Contains(position);

			//var strResult = result ? "is contained" : "is not contained";
			//Debug.WriteLine($"Checking {p} to see if it contained by {r} and it {strResult}.");

			return result;
		}

		#endregion

		#region Event Handlers

		private void Canvas_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if (!InDrag)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- not in drag.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- cancelling drag.");
				_dragIsBeingCancelled = true;
			}
		}

		private void SelectedArea_KeyUp(object sender, KeyEventArgs e)
		{
			Debug.WriteLine($"The {e.Key} was pressed on the Selected Area.");

			if (!IsActive)
			{
				//Debug.WriteLine($"The {e.Key} was pressed, but we are not active, returning.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				IsActive = false;
			}
		}

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			//Debug.WriteLine("The canvas received a MouseWheel event.");

			var cPos = GetPosition();
			var cSize = GetSize();

			Point newPos;
			Size newSize;

			if (e.Delta > 0)
			{
				newPos = new Point(cPos.X - PITCH, cPos.Y - PITCH);
				newSize = new Size(cSize.Width + PITCH * 2, cSize.Height + PITCH * 2);
			}
			else if (e.Delta < 0 && cSize.Width >= PITCH * 4 && cSize.Height >= PITCH * 4)
			{
				newPos = new Point(cPos.X + PITCH, cPos.Y + PITCH);
				newSize = new Size(cSize.Width - PITCH * 2, cSize.Height - PITCH * 2);
			}
			else
			{
				Debug.WriteLine("MouseWheel, but no change.");
				return;
			}

			Move(newPos, newSize);

			e.Handled = true;
		}

		private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_dragAnchor = e.GetPosition(relativeTo: _canvas);
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsActive)
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

			//ReportPosition(position);
			Move(posYInverted);
		}

		private void HandleDragMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				if (!InDrag)
				{
					var dist = _dragAnchor - controlPos;
					if (Math.Abs(dist.Length) > 3)
					{
						_dragLine.X1 = _dragAnchor.X;
						_dragLine.Y1 = _dragAnchor.Y;
						_dragLine.X2 = _dragAnchor.X;
						_dragLine.Y2 = _dragAnchor.Y;

						InDrag = true;
					}
				}

				if (InDrag)
				{
					SetDragPosition(controlPos);
				}
			}

		}

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_dragIsBeingCancelled)
			{
				_dragIsBeingCancelled = false;
				InDrag = false;
				return;
			}

			if (InDrag)
			{
				HandleDragComplete(e);
			}
			else
			{
				HandleSelectionRect(e);
			}
		}

		private void HandleSelectionRect(MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			// The canvas has coordinates where the y value increases from top to bottom.
			var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);

			// Get the center of the block on which the mouse is over.
			var screenPos = ConvertToScreenCoords(posYInverted);
			var blockPosition = MapWindowHelper.GetBlockPosition(screenPos, _blockSize);

			Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {controlPos}. The blockPosition is {blockPosition} ");

			if (!IsActive)
			{
				Activate(blockPosition);
			}
			else
			{
				if (Contains(posYInverted))
				{
					//// Substract the Canvas Control Offset to convert from canvas to screen coordinates.
					//var adjArea = Area.Translate(CanvasControlOffset.Scale(-1d));
					//var adjAreaInt = adjArea.Round();

					//var adjPos = ConvertToScreenCoords(Area.Position);
					//var adjArea2 = new RectangleDbl(adjPos, Area.Size);
					//var adjArea2Int = adjArea2.Round();

					Deactivate();

					//var adjArea = GetAdjArea(Area, 2).Round();

					var adjArea = Area.Round();

					Debug.WriteLine($"Will start job here with position: {adjArea.Position}");

					//Debug.WriteLine($"Will start job here with position: {adjArea.Position}. Comp: {adjArea2.Position}" +
					//	$"The the block containing has position: {blockPosition}, {CanvasControlOffset} was subtracted from the rectangle's canvas coordindates.");

					AreaSelected?.Invoke(this, new AreaSelectedEventArgs(TransformType.Zoom, adjArea));
				}
			}
		}

		private void HandleDragComplete(MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			InDrag = false;
			var offset = GetDragOffset(controlPos).Round();
			Debug.WriteLine($"We are handling a DragComplete with offset:{offset}.");

			ScreenPanned?.Invoke(this, new ScreenPannedEventArgs(TransformType.Pan, offset));
		}

		//private RectangleDbl GetAdjArea(RectangleDbl area, int strategy)
		//{
		//	if (strategy == 0)
		//	{
		//		return area.Translate(CanvasControlOffset.Scale(-1d));
		//	}
		//	else if (strategy == 1)
		//	{
		//		return area.Translate(CanvasControlOffset);
		//	}
		//	else
		//	{
		//		return area;
		//	}
		//}

		private void Canvas_MouseLeave(object sender, MouseEventArgs e)
		{
			if (_isActive)
			{
				_selectedArea.Visibility = Visibility.Hidden;
			}

			if (_inDrag)
			{
				_dragLine.Visibility = Visibility.Hidden;
			}
		}

		private void Canvas_MouseEnter(object sender, MouseEventArgs e)
		{
			if (_isActive)
			{
				_selectedArea.Visibility = Visibility.Visible;

				if (!_selectedArea.Focus())
				{
					Debug.WriteLine("Canvas Enter did not move the focus to the SelectedRectangle.");
				}

				if (_inDrag)
				{
					_dragLine.Visibility = Visibility.Visible;
				}
			}
		}

		#endregion

		#region Diag

		// Just for Diagnostics

		private bool IsShiftKey()
		{
			return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
		}

		private bool IsCtrlKey()
		{
			return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		}

		private bool IsAltKey()
		{
			return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
		}

		private void SelectedArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var position = e.GetPosition(relativeTo: _canvas);

			Debug.WriteLine($"The SelectionRectangle is getting a Mouse Left Button Down at {position}.");
		}

		private void SelectedArea_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			Debug.WriteLine("The SelectionRectangle received a MouseWheel event.");
		}

		#endregion

		#region Private Methods

		// Reposition the Selection Rectangle, keeping it's current size.
		private void Move(Point position)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");

			var x = RoundOff(position.X - (_selectedArea.Width / 2), PITCH);
			if (x < 0)
			{
				x = 0;
			}

			if (x + _selectedArea.Width > _canvas.ActualWidth)
			{
				x = _canvas.ActualWidth - _selectedArea.Width;
			}

			var cLeft = (double)_selectedArea.GetValue(Canvas.LeftProperty);
			if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01)
			{
				_selectedArea.SetValue(Canvas.LeftProperty, x);
			}

			var y = RoundOff(position.Y - (_selectedArea.Height / 2), PITCH);
			if (y < 0)
			{
				y = 0;
			}

			if (y + _selectedArea.Height > _canvas.ActualHeight)
			{
				y = _canvas.ActualHeight - _selectedArea.Height;
			}

			var cBot = (double)_selectedArea.GetValue(Canvas.BottomProperty);
			if (double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			{
				_selectedArea.SetValue(Canvas.BottomProperty, y);
			}
		}

		// Reposition the Selection Rectangle and update its size.
		private void Move(Point position, Size size)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			if (position.X < 0
				|| position.Y < 0
				|| position.X + size.Width > _canvas.ActualWidth
				|| position.Y + size.Height > _canvas.ActualHeight)
			{
				return;
			}

			var cPos = GetPosition();
			if (position.X != cPos.X)
			{
				_selectedArea.SetValue(Canvas.LeftProperty, (double)position.X);
			}

			if (position.Y != cPos.Y)
			{
				_selectedArea.SetValue(Canvas.BottomProperty, (double)position.Y);
			}

			var cSize = GetSize();
			if (Math.Abs(size.Width - cSize.Width) > 0.01)
			{
				_selectedArea.Width = size.Width;
			}

			if (Math.Abs(size.Height - cSize.Height) > 0.01)
			{
				_selectedArea.Height = size.Height;
			}
		}

		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			var dist = controlPos - _dragAnchor;

			// Horizontal
			var x = RoundOff(dist.X, PITCH);

			x = _dragAnchor.X + x;

			if (x < 0)
			{
				x += PITCH;
			}

			if (x > _canvas.ActualWidth)
			{
				x -= PITCH;
			}

			if (Math.Abs(_dragLine.X2 - x) > 0.01)
			{
				_dragLine.X2 = x;
			}

			// Vertical
			var y = RoundOff(dist.Y, PITCH);
			y = _dragAnchor.Y + y;

			if (y < 0)
			{
				y += PITCH;
			}

			if (y > _canvas.ActualWidth)
			{
				y -= PITCH;
			}

			if (Math.Abs(_dragLine.Y2 - y) > 0.01)
			{
				_dragLine.Y2 = y;
			}

			//_dragLine.X2 = controlPos.X;
			//_dragLine.Y2 = controlPos.Y;
		}

		private Point GetPosition()
		{
			var x = (double)_selectedArea.GetValue(Canvas.LeftProperty);
			var y = (double)_selectedArea.GetValue(Canvas.BottomProperty);

			return new Point(double.IsNaN(x) ? 0 : x, double.IsNaN(y) ? 0: y);
		}

		private Size GetSize()
		{
			return new Size(_selectedArea.Width, _selectedArea.Height);
		}

		private double RoundOff(double number, int interval)
		{
			var remainder = (int) Math.IEEERemainder(number, interval);
			number += (remainder < interval / 2) ? -remainder : (interval - remainder);
			return number;
		}

		private void SetMousePosition(Point posYInverted)
		{
			var position = new Point(posYInverted.X, _canvas.ActualHeight - posYInverted.Y);
			var canvasPos = GetCanvasPosition();
			var pos = new Point(position.X + canvasPos.X, position.Y + canvasPos.Y);

			var source = (HwndSource)PresentationSource.FromVisual(_canvas);
			var hWnd = source.Handle; 
			var _ = Win32.PositionCursor(hWnd, pos);

			//Debug.WriteLine($"Activating to canvas:{position}, inv:{posYInverted}, screen:{screenPos}");
		}

		private Point GetCanvasPosition()
		{
			var generalTransform = _canvas.TransformToAncestor(Application.Current.MainWindow);
			var relativePoint = generalTransform.Transform(new Point(0, 0));

			return relativePoint;
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

		// The Image Blocks Group may have it origin shifted down and to the left from the canvas's origin.
		// Convert the point relative to the canvas' origin to coordinates relative to the Image Blocks
		public Point ConvertToScreenCoords(Point posYInverted)
		{
			var pointDbl = new PointDbl(posYInverted.X, posYInverted.Y);
			var screenPos = ConvertToScreenCoords(pointDbl);
			var result = new Point(screenPos.X, screenPos.Y);

			return result;
		}

		// The Image Blocks Group may have it origin shifted down and to the left from the canvas's origin.
		// Convert the point relative to the canvas' origin to coordinates relative to the Image Blocks
		public PointDbl ConvertToScreenCoords(PointDbl posYInverted)
		{
			var result = posYInverted.Translate(CanvasControlOffset);

			return result;
		}


		public Point GetBlockPosition(Point screenPos)
		{
			var result = MapWindowHelper.GetBlockPosition(screenPos, _blockSize);
			return new Point(result.X, result.Y);
		}

		#endregion

		#region Drawing Support

		private DrawingBrush BuildDrawingBrush()
		{
			var aDrawingGroup = new DrawingGroup();

			var inc = 2;
			var x = 0;
			var y = 0;

			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White));

			x = 0;
			y += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black));

			var result = new DrawingBrush(aDrawingGroup)
			{
				TileMode = TileMode.Tile,
				ViewportUnits = BrushMappingMode.Absolute,
				Viewport = new Rect(0, 0, inc * 2, inc * 2)
			};

			return result;
		}

		private GeometryDrawing BuildDot(int x, int y, int size, SolidColorBrush brush)
		{
			var result = new GeometryDrawing(
				brush,
				new Pen(brush, 0),
				new RectangleGeometry(new Rect(new Point(x, y), new Size(size, size)))
			);

			return result;
		}

		#endregion
	}
}
