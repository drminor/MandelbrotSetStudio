using MSS.Common;
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
	internal class CbsSelectionLineFirstDraft : FrameworkElement
	{
		#region Private Fields

		private const int DRAG_TRIGGER_DIST = 3;

		private readonly Line _dragLine;
		private DragState _dragState;

		private Point _dragAnchor;
		private bool _haveMouseDown;

		private readonly VisualCollection _children;

		private double _selectionLinePosition;

		#endregion

		#region Constructor

		public CbsSelectionLineFirstDraft()
		{
			_dragLine = BuildDragLine();
			_children = new VisualCollection(this);
			_children.Add(_dragLine);

			_dragState = DragState.None;


			_dragLine.KeyUp += DragLine_KeyUp;

			MouseLeftButtonUp += HandleMouseLeftButtonUp;
			MouseLeftButtonDown += HandleMouseLeftButtonDown;

			MouseWheel += HandleMouseWheel;
			MouseMove += HandleMouseMove;

			MouseEnter += HandleMouseEnter;
			MouseLeave += HandleMouseLeave;

			Focusable = true;
		}

		private Line BuildDragLine()
		{
			var result = new Line()
			{
				Fill = new SolidColorBrush(Colors.Green), // Brushes.Transparent,
				Stroke = new SolidColorBrush(Colors.Purple),  // DrawingHelper.BuildSelectionDrawingBrush(),
				StrokeThickness = 4,
				Visibility = Visibility.Visible,
				Focusable = true
			};

			//result.SetValue(Panel.ZIndexProperty, 50);

			return result;
		}

		#endregion

		#region Events

		internal event EventHandler<CbsSelectionLineMovedEventArgs>? SelectionLineMoved;

		#endregion

		#region Public Properties

		public double SelectionLinePosition
		{
			get => _selectionLinePosition;
			set => _selectionLinePosition = value;
		}

		private int _cbHeight;

		public int CbHeight
		{
			get => _cbHeight;
			set => _cbHeight = value;
		}

		private int _cbElevation;

		public int CbElevation
		{
			get => _cbElevation;
			set => _cbElevation = value;
		}

		#endregion

		#region Public Methods

		public void Setup(Canvas canvas, int elevation, int height, double xPos)
		{
			_dragLine.Y1 = elevation;
			_dragLine.Y2 = elevation + height;
			
			SelectionLinePosition = xPos;
			_dragLine.X1 = SelectionLinePosition;
			_dragLine.X2 = SelectionLinePosition;

			canvas.Children.Add(_dragLine);

			_dragLine.SetValue(Canvas.LeftProperty, SelectionLinePosition);
			_dragLine.SetValue(Canvas.TopProperty, 0d);
			_dragLine.Stroke = new SolidColorBrush(Colors.Purple);
			_dragLine.StrokeThickness = 4;
			//_dragLine.Width = 4;
			//_dragLine.Height = CbHeight;

			_dragLine.SetValue(Panel.ZIndexProperty, 10);


			DragState = DragState.Begun;
		}


		/*

				var sl = new CbsSelectionLine();
				sl.CbElevation = CB_ELEVATION;
				sl.CbHeight = CB_HEIGHT;
				sl.SelectionLinePosition = scaledArea.Position.X;

				_canvas.Children.Add(sl);

				sl.Setup();		



		*/

		public void TearDown()
		{
			try
			{
				//_mapDisplayViewModel.PropertyChanged -= MapDisplayViewModel_PropertyChanged;

				MouseLeftButtonUp -= HandleMouseLeftButtonUp;
				MouseLeftButtonDown -= HandleMouseLeftButtonDown;

				MouseWheel -= HandleMouseWheel;
				MouseMove -= HandleMouseMove;

				MouseEnter -= HandleMouseEnter;
				MouseLeave -= HandleMouseLeave;
			}
			catch
			{
				Debug.WriteLine("SelectionRectangle encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Private Properties

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

		#endregion


		#region Event Handlers

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

				SelectionLineMoved?.Invoke(this, CbsSelectionLineMovedEventArgs.CreateCancelPreviewInstance());
			}
		}

		private void HandleMouseWheel(object sender, MouseWheelEventArgs e)
		{
			//if (!Selecting)
			//{
			//	return;
			//}

			////Debug.WriteLine("The canvas received a MouseWheel event.");

			//var controlPos = e.GetPosition(relativeTo: _canvas);
			//var posYInverted = new Point(controlPos.X, _canvas.ActualHeight - controlPos.Y);

			//var cSize = SelectedSize;

			//Rect selection;

			//if (e.Delta < 0)
			//{
			//	// Reverse roll, zooms out.
			//	//selection = Expand(SelectedPosition, SelectedSize, PITCH_TARGET);
			//	selection = Expand(SelectedPosition, SelectedSize, _pitch);
			//}
			//else if (e.Delta > 0 && cSize.Width >= _pitch * 4 && cSize.Height >= _pitch * 4)
			//{
			//	// Forward roll, zooms in.
			//	//selection = Expand(SelectedPosition, SelectedSize, -1 * PITCH_TARGET);
			//	selection = Expand(SelectedPosition, SelectedSize, -1 * _pitch);
			//}
			//else
			//{
			//	//Debug.WriteLine("MouseWheel, but no change.");
			//	return;
			//}

			//var selectedPositionWasUpdated = MoveAndSize(selection.Location, selection.Size);

			//if (selectedPositionWasUpdated)
			//{
			//	SelectedCenterPosition = posYInverted;
			//}

			//e.Handled = true;
		}

		private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_haveMouseDown = true;

			if (DragState == DragState.None)
			{
				_dragAnchor = e.GetPosition(relativeTo: this);
			}
		}

		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			HandleDragMove(e);
		}

		private void HandleSelectionMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: this);

			// Invert the y coordinate.
			var posYInverted = new Point(controlPos.X, ActualHeight - controlPos.Y);
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
					var controlPos = e.GetPosition(relativeTo: this);
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
				var controlPos = e.GetPosition(relativeTo: this);
				SetDragPosition(controlPos);
			}
		}

		private void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (!IsEnabled)
			{
				//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event -- we are disabled.");
				return;
			}

			//Debug.WriteLine($"Section Rectangle is getting a MouseLeftButtonUp event. IsFocused = {_canvas.IsFocused}. Have a mouse down event = {_haveMouseDown}, DragState = {DragState}, Selecting = {Selecting}");

			if (DragState != DragState.None)
			{
				HandleDragLine(e);
			}
		}

		private void HandleDragLine(MouseButtonEventArgs e)
		{
			//var controlPos = e.GetPosition(relativeTo: _canvas);

			//if (DragState == DragState.Begun)
			//{
			//	DragState = DragState.InProcess;
			//}
			//else
			//{
			//	DragState = DragState.None;
			//	var offset = GetDragOffset(DragLineTerminus);
			//	SelectionLineMoved?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: false));
			//}
		}

		private void HandleMouseLeave(object sender, MouseEventArgs e)
		{
			////_haveMouseDown = false;
			//if (Selecting)
			//{
			//	_selectedArea.Visibility = Visibility.Hidden;
			//}

			//else if (DragState == DragState.Begun)
			//{
			//	DragState = DragState.None;
			//	_dragLine.Visibility = Visibility.Hidden;
			//}

			//else if (DragState == DragState.InProcess)
			//{
			//	_dragLine.Visibility = Visibility.Hidden;
			//}
		}

		private void HandleMouseEnter(object sender, MouseEventArgs e)
		{
			//if (Selecting)
			//{
			//	_selectedArea.Visibility = Visibility.Visible;

			//	if (!_selectedArea.Focus())
			//	{
			//		Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the SelectedRectangle.");
			//	}
			//}
			
			//else if (DragState != DragState.None)
			//{
			//	_dragLine.Visibility = Visibility.Visible;

			//	if (!_dragLine.Focus())
			//	{
			//		Debug.WriteLine("WARNING: Canvas Enter did not move the focus to the DragLine.");
			//	}
			//}
		}

		private void Activate(Point position)
		{
			//Selecting = true;
			//Move(position);

			//if (!_selectedArea.Focus())
			//{
			//	Debug.WriteLine("WARNING: Activate did not move the focus to the SelectedRectangle");
			//}
		}

		#endregion

		#region Private Methods

		private double StopSelecting()
		{
			return 0d;
		}

		// Return the distance from the DragAnchor to the new mouse position.
		private VectorInt GetDragOffset(Point controlPos)
		{
			var startP = new PointDbl(_dragAnchor.X, ActualHeight - _dragAnchor.Y);
			var endP = new PointDbl(controlPos.X, ActualHeight - controlPos.Y);
			var vectorDbl = endP.Diff(startP);
			var result = vectorDbl.Round();

			return result;
		}

		// Reposition the Selection Rectangle, keeping it's current size.
		private void Move(Point posYInverted)
		{
			////Debug.WriteLine($"Moving the sel rec to {position}, free form.");
			////ReportPosition(posYInverted);

			//var x = DoubleHelper.RoundOff(posYInverted.X - (_selectedArea.Width / 2), _pitch);
			//var y = DoubleHelper.RoundOff(posYInverted.Y - (_selectedArea.Height / 2), _pitch);

			//if (x < 0)
			//{
			//	x = 0;
			//}

			//if (x + _selectedArea.Width > _canvas.ActualWidth)
			//{
			//	x = _canvas.ActualWidth - _selectedArea.Width;
			//}

			//if (y < 0)
			//{
			//	y = 0;
			//}

			//if (y + _selectedArea.Height > _canvas.ActualHeight)
			//{
			//	y = _canvas.ActualHeight - _selectedArea.Height;
			//}

			//var cLeft = (double)_selectedArea.GetValue(Canvas.LeftProperty);
			//var cBot = (double)_selectedArea.GetValue(Canvas.BottomProperty);

			//if (double.IsNaN(cLeft) || Math.Abs(x - cLeft) > 0.01 || double.IsNaN(cBot) || Math.Abs(y - cBot) > 0.01)
			//{
			//	SelectedCenterPosition = posYInverted;
			//	SelectedPosition = new Point(x, y);

			//	var displaySize = new SizeDbl(_canvas.ActualWidth, _canvas.ActualHeight);
			//	var (zoomPoint, factor) = GetAreaSelectedParams(Area, displaySize);
			//	var eventArgs = new AreaSelectedEventArgs(TransformType.ZoomIn, zoomPoint, factor, Area, displaySize, isPreview: true);

			//	//Debug.WriteLine($"Raising AreaSelected PREVIEW with position: {zoomPoint} and factor: {factor}");

			//	AreaSelected?.Invoke(this, eventArgs);
			//}
		}


		// Position the current end of the drag line
		private void SetDragPosition(Point controlPos)
		{
			//var dist = controlPos - _dragAnchor;

			//// Horizontal
			//var x = DoubleHelper.RoundOff(dist.X, _pitch);

			//x = _dragAnchor.X + x;

			//if (x < 0)
			//{
			//	x += _pitch;
			//}

			//if (x > _canvas.ActualWidth)
			//{
			//	x -= _pitch;
			//}

			//// Vertical
			//var y = DoubleHelper.RoundOff(dist.Y, _pitch);
			//y = _dragAnchor.Y + y;

			//if (y < 0)
			//{
			//	y += _pitch;
			//}

			//if (y > _canvas.ActualWidth)
			//{
			//	y -= _pitch;
			//}

			//var dragLineTerminus = DragLineTerminus;
			//var newDlt = new Point(x, y);

			//if ( (dragLineTerminus - newDlt).LengthSquared > 0.05 )
			//{
			//	DragLineTerminus = newDlt;

			//	var offset = GetDragOffset(DragLineTerminus);
			//	SelectionLineMoved?.Invoke(this, new ImageDraggedEventArgs(TransformType.Pan, offset, isPreview: true));
			//}
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
	}

}
