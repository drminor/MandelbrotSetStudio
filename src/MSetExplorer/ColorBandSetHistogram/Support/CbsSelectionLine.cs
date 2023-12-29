using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbsSelectionLine
	{
		#region Private Fields

		private const double MIN_SEL_DISTANCE = 0.49;
		private static readonly Brush _selectionLineBrush = DrawingHelper.BuildSelectionDrawingBrush();

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _cbElevation;
		private double _cbHeight;
		private readonly double _scaleX;
		private IsSelectedChanged _isSelectedChanged;

		private double _selectionLinePosition;

		private readonly RectangleGeometry _left;
		private readonly RectangleGeometry _right;

		private readonly RectangleGeometry _selLeft;
		private readonly RectangleGeometry _selRight;

		private readonly Line _dragLine;
		private readonly Polygon _topArrow;
		private double _topArrowHalfWidth;

		private DragState _dragState;

		private double _originalXPosition;

		private readonly RectangleGeometry _originalLeftGeometry;
		private readonly RectangleGeometry _originalRightGeometry;

		private bool _isSelected;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsSelectionLine(int colorBandIndex, bool isSelected, double xPosition, RectangleGeometry left, RectangleGeometry right, RectangleGeometry selLeft, RectangleGeometry selRight, 
			ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChanged isSelectedChanged)
		{
			_isSelected = isSelected;
			_dragState = DragState.None;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_canvas = canvas;
			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_scaleX = _colorBandLayoutViewModel.ContentScale.Width;
			_isSelectedChanged = isSelectedChanged;

			_selectionLinePosition = xPosition;
			_originalXPosition = xPosition;

			_left = left;
			_right = right;

			_originalLeftGeometry = new RectangleGeometry(_left.Rect);
			_originalRightGeometry = new RectangleGeometry(_right.Rect);

			_selLeft = selLeft;
			_selRight = selRight;

			_dragLine = BuildDragLine(_cbElevation, _cbHeight, xPosition);
			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);

			_topArrowHalfWidth = (_cbElevation - 2) / 2;
			_topArrow = BuildTopArrow(_selectionLinePosition);

			_topArrow.MouseUp += _topArrow_MouseUp;
			//_topArrow.PreviewKeyDown += TopArrow_PreviewKeyDown;

			_canvas.Children.Add(_topArrow);
			_topArrow.SetValue(Panel.ZIndexProperty, 30);
		}

		private Line BuildDragLine(double elevation, double height, double xPosition)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = _selectionLineBrush,
				StrokeThickness = 2,
				Y1 = elevation,
				Y2 = elevation + height,
				X1 = xPosition,
				X2 = xPosition,
				//Focusable = true
			};

			return result;
		}

		private Polygon BuildTopArrow(double xPosition)
		{
			var result = new Polygon()
			{
				Fill = GetTopArrowFill(_isSelected),
				Stroke = Brushes.DarkGray,
				StrokeThickness = 2,
				Points = BuildTopAreaPoints(xPosition),
				//Focusable = true
			};

			return result;
		}

		private Brush GetTopArrowFill(bool isSelected)
		{
			var result = isSelected ? Brushes.LightSlateGray : Brushes.GhostWhite;

			return result;
		}

		private PointCollection BuildTopAreaPoints(double xPosition)
		{
			var points = new PointCollection()
			{
				new Point(xPosition, CbElevation),				// The bottom of the arrow is positioned at the top of the band display
				new Point(xPosition - _topArrowHalfWidth, 0),	// Top, left is at the top of the control
				new Point(xPosition + _topArrowHalfWidth, 0),	// Top, right is at the top of the control
			};

			return points;
		}
		
		private void _topArrow_MouseUp(object sender, MouseButtonEventArgs e)
		{
			var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
			var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

			_isSelectedChanged(ColorBandIndex, !IsSelected, shiftKeyPressed, controlKeyPressed);
			//IsSelected = !IsSelected;
		}

		#endregion

		#region Events

		internal event EventHandler<CbsSelectionLineMovedEventArgs>? SelectionLineMoved;

		#endregion

		#region Public Properties

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					_topArrow.Fill = GetTopArrowFill(_isSelected);
				}
			}
		}

		public int ColorBandIndex { get; init; }

		public double OriginalSelectionLinePosition => _originalXPosition;

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

					_topArrow.Points = BuildTopAreaPoints(_selectionLinePosition);
				}
			}
		}

		public double CbElevation
		{
			get => _cbElevation;
			set
			{
				if (value != _cbElevation)
				{
					_cbElevation = value;
					_dragLine.Y1 = value;
					_dragLine.Y2 = CbElevation + CbHeight;

					_topArrowHalfWidth = (_cbElevation - 2) / 2;
				}
			}
		}

		public double CbHeight
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
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. The new position is {newPosition}. The original position is {_originalXPosition}.");
				SelectionLinePosition = newPosition;

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
					_canvas.Children.Remove(_topArrow);
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
					//_dragLine.Stroke.Opacity = 0;

					_dragLine.Visibility = Visibility.Collapsed;
					_topArrow.Visibility = Visibility.Collapsed;
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
					//_dragLine.Stroke.Opacity = 1;
					_dragLine.Visibility = Visibility.Visible;
					_topArrow.Visibility = Visibility.Visible;
				}
			}
			catch
			{
				Debug.WriteLine("CbsSelectionLine encountered an exception in Show.");
			}
		}

		public void StartDrag()
		{
			if (DragState != DragState.InProcess)
			{
				_originalXPosition = SelectionLinePosition;
				_originalLeftGeometry.Rect = _left.Rect;
				_originalRightGeometry.Rect = _right.Rect;

				DragState = DragState.InProcess;
			}

			Debug.WriteLine($"Beginning to Drag the SelectionLine for ColorBandIndex: {ColorBandIndex}, the Geometries are: {BuildGeometriesReport()}.");
		}

		public void CancelDrag()
		{
			DragState = DragState.None;

			if (SelectionLinePosition != _originalXPosition)
			{
				SelectionLinePosition = _originalXPosition;

				UpdateColorBandWidth(0);

				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, _originalXPosition, CbsSelectionLineDragOperation.Cancel));
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
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. UpdateColorBandWidth returned true. The XPos is {pos.X}. The original position is {_originalXPosition}.");
				SelectionLinePosition = pos.X;

				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, pos.X, CbsSelectionLineDragOperation.Move));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. UpdateColorBandWidth returned false. The XPos is {pos.X}. The original position is {_originalXPosition}.");
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
					CompleteDrag();
				}
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. HandleMouseLeftButtonUp The Keyboard focus is now on {Keyboard.FocusedElement}.");
		}

		#endregion

		#region Private Methods

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
				if (_originalLeftGeometry.Rect.Width > amount + (1 * _scaleX) && _originalRightGeometry.Rect.X > amount /*+ (1 * _scale)*/)
				{
					_left.Rect = DrawingHelper.Shorten(_originalLeftGeometry.Rect, amount);
					_right.Rect = DrawingHelper.MoveRectLeft(_originalRightGeometry.Rect, amount);

					_selLeft.Rect = DrawingHelper.CopyXAndWidth(_left.Rect, _selLeft.Rect);
					_selRight.Rect = DrawingHelper.CopyXAndWidth(_right.Rect, _selRight.Rect);

					Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. Shortening the Left ColorBandRectangle by amount: {amount}. Left Width: {_originalLeftGeometry.Rect.Width}, Right Pos: {_originalRightGeometry.Rect.X}" +
						$"New Left Rectangle Width = {_left.Rect.Width}; New Right Rectangle Position: {_right.Rect.X}");
					
					updated = true;
				}
			}
			else
			{
				if (_originalRightGeometry.Rect.Width > amount + (1 * _scaleX))
				{
					_left.Rect = DrawingHelper.Lengthen(_originalLeftGeometry.Rect, amount);
					_right.Rect = DrawingHelper.MoveRectRight(_originalRightGeometry.Rect, amount);

					_selLeft.Rect = DrawingHelper.CopyXAndWidth(_left.Rect, _selLeft.Rect);
					_selRight.Rect = DrawingHelper.CopyXAndWidth(_right.Rect, _selRight.Rect);

					Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. Lengthening the Left ColorBandRectangle by amount: {amount}. Left Width: {_originalLeftGeometry.Rect.Width}, Right Pos: {_originalRightGeometry.Rect.X}" +
						$"New Left Rectangle Width = {_left.Rect.Width}; New Right Rectangle Position: {_right.Rect.X}");

					updated = true;
				}
			}

			return updated;
		}

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

		#region Unused

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
	}
}
