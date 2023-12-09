using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;

namespace MSetExplorer
{
	internal class CbsSelectionLine
	{
		#region Private Fields

		private Canvas _canvas;

		private readonly Line _dragLine;

		private readonly RectangleGeometry _left;
		private readonly RectangleGeometry _right;

		private readonly RectangleGeometry _originalLeftGeometry;
		private readonly RectangleGeometry _originalRightGeometry;


		private DragState _dragState;

		private double _originalXPosition;
		private double _selectionLinePosition;
		private int _cbElevation;
		private int _cbHeight;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsSelectionLine(Canvas canvas, int elevation, int height, int colorBandIndex, double xPosition, RectangleGeometry left, RectangleGeometry right)
		{
			_canvas = canvas;
			ColorBandIndex = colorBandIndex;
			_originalXPosition = xPosition;
			_selectionLinePosition = xPosition;

			_left = left;
			_right = right;

			_originalLeftGeometry = new RectangleGeometry(_left.Rect);
			_originalRightGeometry = new RectangleGeometry(_right.Rect);

			_cbElevation = elevation;
			_cbHeight = height;
			_dragLine = BuildDragLine(elevation, height, xPosition);

			_dragState = DragState.None;

			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);
		}

		private Line BuildDragLine(int elevation, int height, double xPosition)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent, // new SolidColorBrush(Colors.Green),
				Stroke = DrawingHelper.BuildSelectionDrawingBrush(), // new SolidColorBrush(Colors.Purple),
				StrokeThickness = 2,
				Y1 = elevation,
				Y2 = elevation + height,
				X1 = xPosition,
				X2 = xPosition,
				Focusable = true
			};

			return result;
		}

		#endregion

		#region Events

		internal event EventHandler<CbsSelectionLineMovedEventArgs>? SelectionLineMoved;

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; init; }

		public double SelectionLinePosition
		{
			get => _selectionLinePosition;
			set
			{
				if (value != _selectionLinePosition)
				{
					_selectionLinePosition = value;
					_dragLine.X1 = value;
					_dragLine.X2 = value;
				}
			}
		}

		public int CbElevation
		{
			get => _cbElevation;
			set
			{
				if (value != _cbElevation)
				{
					_cbElevation = value;
					_dragLine.Y1 = value;
					_dragLine.Y2 = CbElevation + CbHeight;
				}
			}
		}

		public int CbHeight
		{
			get => _cbHeight;
			set
			{
				if (value != _cbHeight)
				{
					_cbHeight = value;
					_dragLine.Y2 = CbElevation + CbHeight;
				}
			}
		}

		#endregion

		#region Public Methods

		public bool UpdatePosition(double newPosition)
		{
			if (DragState == DragState.Begun || DragState == DragState.InProcess)
			{
				// The HandleMouseMove eventHandler is managing the SelectionLine positions.
				return false;
			}

			var amount = newPosition - _originalXPosition;

			if (ScreenTypeHelper.IsDoubleNearZero(amount))
			{
				return false;
			}

			if (UpdateColorBandWidth(amount))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The new position is {newPosition}. The original position is {_originalXPosition}.");
				SelectionLinePosition = newPosition;

				//_originalXPosition = newPosition;
				//_originalLeftGeometry.Rect = _left.Rect;
				//_originalRightGeometry.Rect = _right.Rect;

				return true;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine::UpdatePosition. The call to UpdateColorBandWidth returned false. The new position is {newPosition}. The original position is {_originalXPosition}.");
				return false;
			}
		}

		public void TearDown()
		{
			try
			{
				if (DragState != DragState.None)
				{
					DragState = DragState.None;
				}

				if (_canvas != null)
				{
					_canvas.Children.Remove(_dragLine);
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in TearDown.");
			}
		}

		public void Hide()
		{
			try
			{
				if (_canvas != null)
				{
					_dragLine.Stroke.Opacity = 0;
					//_canvas.Children.Remove(_dragLine);
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in Hide.");
			}
		}

		public void Show()
		{
			try
			{
				if (_canvas != null)
				{
					_dragLine.Stroke.Opacity = 1;
					//_canvas.Children.Remove(_dragLine);
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in Show.");
			}
		}

		public void StartDrag()
		{
			_originalXPosition = SelectionLinePosition;
			_originalLeftGeometry.Rect = _left.Rect;
			_originalRightGeometry.Rect = _right.Rect;

			DragState = DragState.InProcess;

			//Debug.WriteLine($"Beginning to Drag the SelectionLine for ColorBandIndex: {ColorBandIndex}, the Geometries are: {BuildGeometriesReport()}.");
		}

		public void CancelDrag()
		{
			DragState = DragState.None;

			UpdateColorBandWidth(0);

			SelectionLinePosition = _originalXPosition;

			SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, _originalXPosition, CbsSelectionLineDragOperation.Cancel));
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
					switch (value)
					{
						case DragState.None:

							if (_canvas != null)
							{
								_canvas.MouseMove -= HandleMouseMove;
								_canvas.MouseLeftButtonUp -= HandleMouseLeftButtonUp;
							}
							break;

						case DragState.InProcess:

							if (_canvas != null)
							{
								_canvas.MouseMove += HandleMouseMove;
								_canvas.MouseLeftButtonUp += HandleMouseLeftButtonUp;
								//var re = Keyboard.KeyUpEvent.AddOwner(typeof(CbsSelectionLine));
							}
							break;

						case DragState.Begun:
							break;

						default:
							break;
					}

					_dragState = value;
				}
			}
		}

		#endregion

		#region Event Handlers

		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.Escape))
			{
				CancelDrag();
				return;
			}

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				// The user lifted the left mouse button while the mouse was not on the canvas.
				CancelDrag();
				return;
			}

			var pos = e.GetPosition(relativeTo: _canvas);

			var amount = pos.X - _originalXPosition;

			if (UpdateColorBandWidth(amount))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"After call to UpdateColorBandWidth, The XPos is {pos.X}. The original position is {_originalXPosition}.");
				SelectionLinePosition = pos.X;

				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, pos.X, CbsSelectionLineDragOperation.Move));
			}
		}

		private void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (DragState != DragState.None)
			{
				if (Keyboard.IsKeyDown(Key.Escape))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsSelectionLine is getting a MouseLeftButtonUp event. The Escape Key is Pressed, cancelling.");
					CancelDrag();
				}
				else
				{
					var pos = e.GetPosition(relativeTo: _canvas);

					Debug.WriteLineIf(_useDetailedDebug, $"The CbsSelectionLine is getting a MouseLeftButtonUp event. Completing the Drag operation. The last XPos is {SelectionLinePosition}. The XPos is {pos.X}. The original position is {_originalXPosition}.");
					CompleteDrag();
				}
			}
		}

		#endregion

		#region Private Methods

		private const double MIN_SEL_DISTANCE = 0.49;

		private void CompleteDrag()
		{
			var distance = Math.Abs(SelectionLinePosition - _originalXPosition);

			if (distance > MIN_SEL_DISTANCE)
			{
				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, SelectionLinePosition, CbsSelectionLineDragOperation.Complete));
			}
			else
			{
				CancelDrag();
			}

			DragState = DragState.None;
		}

		private bool UpdateColorBandWidth(double amount)
		{
			//Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine[{ColorBandIndex}] is having its ColorBandWidth updated by amount: {amount}.");
			
			var updated = false;

			if (amount < 0)
			{
				amount = amount * -1;
				if (_originalLeftGeometry.Rect.Width > amount && _originalRightGeometry.Rect.X > amount)
				{
					_left.Rect = DrawingHelper.Shorten(_originalLeftGeometry.Rect, amount);
					_right.Rect = DrawingHelper.MoveRectLeft(_originalRightGeometry.Rect, amount);
					updated = true;
				}
			}
			else
			{
				if (_originalRightGeometry.Rect.Width > amount)
				{
					_left.Rect = DrawingHelper.Lengthen(_originalLeftGeometry.Rect, amount);
					_right.Rect = DrawingHelper.MoveRectRight(_originalRightGeometry.Rect, amount);
					updated = true;
				}
			}

			return updated;
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

		#endregion

		#region Diag

		private string BuildGeometriesReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Left: {_left.Rect}");
			sb.AppendLine($"Right: {_right.Rect}");
			sb.AppendLine($"Left Original: {_originalLeftGeometry.Rect}");
			sb.AppendLine($"Right Original: {_originalRightGeometry.Rect}");

			return sb.ToString();
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

		//	Debug.WriteLine($"The CbsSelectionLine is getting a Mouse Left Button Down at {position}.");
		//}

		//private void SelectedArea_MouseWheel(object sender, MouseWheelEventArgs e)
		//{
		//	Debug.WriteLine("The CbsSelectionLine received a MouseWheel event.");
		//}

		#endregion
	}

}
