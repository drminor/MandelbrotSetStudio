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
		private const int PITCH_TARGET = 16;
		private const int DRAG_TRIGGER_DIST = 3;

		private readonly Canvas _canvas;
		private readonly SizeInt _blockSize; // Also used as the initial size of the selection rectangle.
		private readonly Rectangle _selectedArea;
		private readonly Line _dragLine;

		private int _pitch;
		private bool _selecting;
		private bool _dragging;
		private Point _dragAnchor;
		private bool _dragHasBegun;

		internal event EventHandler<AreaSelectedEventArgs>? AreaSelected;
		internal event EventHandler<ImageDraggedEventArgs>? ImageDragged;

		#region Constructor

		public SelectionRectangle(Canvas canvas, SizeInt blockSize)
		{
			_canvas = canvas;
			_blockSize = blockSize;

			_selectedArea = BuildSelectionRectangle(_canvas);
			_dragLine = BuildDragLine(_canvas);
			_pitch = CalculatePitch(_canvas);
			canvas.SizeChanged += Canvas_SizeChanged;

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
			var result = new Rectangle()
			{
				Width = _blockSize.Width,
				Height = _blockSize.Height,
				Fill = Brushes.Transparent,
				Stroke = BuildDrawingBrush(),
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
				Stroke = BuildDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Hidden,
				Focusable = true
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Panel.ZIndexProperty, 20);

			return result;
		}

		private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_pitch = CalculatePitch(_canvas);
		}

		// The Pitch is the narrowest canvas dimension / the value having the closest power of 2 of the value given by the narrowest canvas dimension / 16.
		private int CalculatePitch(Canvas canvas)
		{
			int result;

			if (canvas.ActualWidth < canvas.ActualHeight)
			{
				result = (int) Math.Round(canvas.ActualWidth / Math.Pow(2, Math.Round(Math.Log2(canvas.ActualWidth / PITCH_TARGET))));
			}
			else
			{
				result = (int)Math.Round(canvas.ActualHeight / Math.Pow(2, Math.Round(Math.Log2(canvas.ActualHeight / PITCH_TARGET))));
			}

			Debug.WriteLine($"The new ScreenSelection Pitch is {result}.");
			return result;
		}

		#endregion

		#region Public Properties

		public RectangleDbl Area
		{
			get
			{
				var p = SelectedPosition;
				var s = SelectedSize;
				var result = new RectangleDbl(new PointDbl(p.X, p.Y), new SizeDbl(s.Width, s.Height));

				return result;
			}
		}

		#endregion

		#region Event Handlers

		private void SelectedArea_KeyUp(object sender, KeyEventArgs e)
		{
			Debug.WriteLine($"The {e.Key} was pressed on the Selected Area.");

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
			if (!Dragging)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- not in drag.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the Canvas -- preview -- cancelling drag.");
				_dragHasBegun = false;
				Dragging = false;
			}
		}

		private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!Selecting)
			{
				return;
			}

			//Debug.WriteLine("The canvas received a MouseWheel event.");

			var cPos = SelectedPosition;
			var cSize = SelectedSize;

			Point newPos;
			Size newSize;

			if (e.Delta > 0)
			{
				newPos = new Point(cPos.X - _pitch, cPos.Y - _pitch);
				newSize = new Size(cSize.Width + _pitch * 2, cSize.Height + _pitch * 2);
			}
			else if (e.Delta < 0 && cSize.Width >= _pitch * 4 && cSize.Height >= _pitch * 4)
			{
				newPos = new Point(cPos.X + _pitch, cPos.Y + _pitch);
				newSize = new Size(cSize.Width - _pitch * 2, cSize.Height - _pitch * 2);
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
			if (!Dragging)
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
			if ((!Dragging) && e.LeftButton == MouseButtonState.Pressed)
			{
				var controlPos = e.GetPosition(relativeTo: _canvas);
				var dist = _dragAnchor - controlPos;
				if (Math.Abs(dist.Length) > DRAG_TRIGGER_DIST)
				{
					_dragLine.X1 = _dragAnchor.X;
					_dragLine.Y1 = _dragAnchor.Y;
					_dragLine.X2 = _dragAnchor.X;
					_dragLine.Y2 = _dragAnchor.Y;

					Dragging = true;
					_dragHasBegun = true;
					_dragLine.Focus();
				}
			}

			if (Dragging)
			{
				var controlPos = e.GetPosition(relativeTo: _canvas);
				SetDragPosition(controlPos);
			}
		}

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (Dragging)
			{
				HandleDragLine(e);
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

			if (!Selecting)
			{
				Debug.WriteLine($"Activating Select at {posYInverted}.");
				Activate(posYInverted);
			}
			else
			{
				if (Contains(posYInverted))
				{
					Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = True.");
					var adjArea = Area.Round();
					Deactivate();

					Debug.WriteLine($"Will start job here with position: {adjArea.Position}");
					AreaSelected?.Invoke(this, new AreaSelectedEventArgs(TransformType.ZoomIn, adjArea));
				}
				else
				{
					Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {posYInverted} Contains = False.");
				}
			}
		}

		private void HandleDragLine(MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: _canvas);

			if (_dragHasBegun)
			{
				_dragHasBegun = false;
			}
			else
			{
				Dragging = false;
				var offset = GetDragOffset(DragLineTerminus);

				Debug.WriteLine($"We are handling a DragComplete with offset:{offset}.");
				ImageDragged?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset));
			}
		}

		private void Canvas_MouseLeave(object sender, MouseEventArgs e)
		{
			if (Selecting)
			{
				_selectedArea.Visibility = Visibility.Hidden;
			}

			if (Dragging)
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
					Debug.WriteLine("Canvas Enter did not move the focus to the SelectedRectangle.");
				}
			}

			if (Dragging)
			{
				_dragLine.Visibility = Visibility.Visible;

				if (!_dragLine.Focus())
				{
					Debug.WriteLine("Canvas Enter did not move the focus to the DragLine.");
				}
			}
		}

		private void Activate(Point position)
		{
			Selecting = true;
			Move(position);

			if (!_selectedArea.Focus())
			{
				Debug.WriteLine("Activate did not move the focus to the SelectedRectangle");
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
						_selectedArea.Width = _blockSize.Width;
						_selectedArea.Height = _blockSize.Height;
					}

					_selecting = value;
				}
			}
		}

		private bool Dragging
		{
			get => _dragging;

			set
			{
				if (_dragging != value)
				{
					if (value)
					{
						_dragLine.Visibility = Visibility.Visible;
						_dragLine.Focus();
					}
					else
					{
						_dragLine.Visibility = Visibility.Hidden;
					}

					_dragging = value;
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

		#region Private Methods

		private void Deactivate()
		{
			Selecting = false;
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
			var sizeDbl = endP.Diff(startP);

			var result = new VectorInt(sizeDbl.Round());

			return result;
		}
		
		// Reposition the Selection Rectangle, keeping it's current size.
		private void Move(Point posYInverted)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, free form.");
			//ReportPosition(posYInverted);

			var x = RoundOff(posYInverted.X - (_selectedArea.Width / 2), _pitch);
			var y = RoundOff(posYInverted.Y - (_selectedArea.Height / 2), _pitch);

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
				SelectedPosition = new Point(x, y);
			}
		}

		// Reposition the Selection Rectangle and update its size.
		private void Move(Point posYInverted, Size size)
		{
			//Debug.WriteLine($"Moving the sel rec to {position}, with size: {size}");

			if (posYInverted.X < 0
				|| posYInverted.Y < 0
				|| posYInverted.X + size.Width > _canvas.ActualWidth
				|| posYInverted.Y + size.Height > _canvas.ActualHeight)
			{
				return;
			}

			var cPos = SelectedPosition;
			if (posYInverted.X != cPos.X || posYInverted.Y != cPos.Y)
			{
				SelectedPosition = posYInverted;
			}

			var cSize = SelectedSize;
			if (Math.Abs(size.Width - cSize.Width) > 0.01 || Math.Abs(size.Height - cSize.Height) > 0.01)
			{
				SelectedSize = size;
			}
		}

		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			var dist = controlPos - _dragAnchor;

			// Horizontal
			var x = RoundOff(dist.X, _pitch);

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
			var y = RoundOff(dist.Y, _pitch);
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
			}
		}

		private double RoundOff(double number, int interval)
		{
			var remainder = (int)Math.IEEERemainder(number, interval);
			number += (remainder < interval / 2) ? -remainder : (interval - remainder);
			return number;
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
