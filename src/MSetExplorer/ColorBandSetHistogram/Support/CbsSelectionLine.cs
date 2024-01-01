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

		private const double SELECTION_LINE_ARROW_WIDTH = 6;
		private const double MIN_SEL_DISTANCE = 0.49;
		private static readonly Brush _selectionLineBrush = DrawingHelper.BuildSelectionDrawingBrush();

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _xPosition;
		private double _cbElevation;
		private double _cbHeight;
		private double _scaleX;
		private IsSelectedChanged _isSelectedChanged;
		private Action<int, ColorBandSelectionType> _displayContext;

		private double _selectionLinePosition;

		private readonly Line _dragLine;
		private readonly Polygon _topArrow;
		private double _topArrowHalfWidth;

		private DragState _dragState;

		private double _originalSelectionLinePosition;

		private RectangleGeometries? _rectangleGeometries;

		private bool _isSelected;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsSelectionLine(int colorBandIndex, bool isSelected, double xPosition, ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChanged isSelectedChanged, bool isVisible, Action<int, ColorBandSelectionType> displayContext)
		{
			_isSelected = isSelected;
			_dragState = DragState.None;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += _colorBandLayoutViewModel_PropertyChanged;
			_canvas = canvas;
			_xPosition = xPosition;

			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_scaleX = _colorBandLayoutViewModel.ContentScale.Width;
			_isSelectedChanged = isSelectedChanged;
			_displayContext = displayContext;

			_selectionLinePosition = _xPosition * _scaleX;
			_originalSelectionLinePosition = _selectionLinePosition;

			_dragLine = BuildDragLine(_cbElevation, _cbHeight, _selectionLinePosition, isVisible);
			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);

			//_topArrowHalfWidth = (_cbElevation - 2) / 2;
			_topArrowHalfWidth = SELECTION_LINE_ARROW_WIDTH;
			_topArrow = BuildTopArrow(_selectionLinePosition, isVisible);

			_topArrow.MouseUp += Handle_MouseUp;
			//_topArrow.PreviewKeyDown += TopArrow_PreviewKeyDown;

			_canvas.Children.Add(_topArrow);
			_topArrow.SetValue(Panel.ZIndexProperty, 30);
		}

		private void _colorBandLayoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ContentScale")
			{
				ScaleX = _colorBandLayoutViewModel.ContentScale.Width;
				_originalSelectionLinePosition = SelectionLinePosition;
			}
		}

		private Line BuildDragLine(double elevation, double height, double selectionLinePosition, bool isVisible)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = _selectionLineBrush,
				StrokeThickness = 2,
				Y1 = elevation,
				Y2 = elevation + height,
				X1 = selectionLinePosition,
				X2 = selectionLinePosition,
				//Focusable = true
				Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed
			};

			return result;
		}

		private Polygon BuildTopArrow(double selectionLinePosition, bool isVisible)
		{
			var result = new Polygon()
			{
				Fill = GetTopArrowFill(_isSelected),
				Stroke = Brushes.DarkGray,
				StrokeThickness = 2,
				Points = BuildTopAreaPoints(selectionLinePosition),
				//Focusable = true
				Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed
			};

			return result;
		}

		private Brush GetTopArrowFill(bool isSelected)
		{
			var result = isSelected ? Brushes.LightSlateGray : Brushes.Transparent;

			return result;
		}

		private PointCollection BuildTopAreaPoints(double xPosition)
		{
			var points = new PointCollection()
			{
				new Point(xPosition, CbElevation),				// The bottom of the arrow is positioned at the top of the band display
				new Point(xPosition - _topArrowHalfWidth, 0),	// Top, left is at the top of the control
				//new Point(xPosition + _topArrowHalfWidth, 0),	// Top, right is at the top of the control and leaning forward into the next band
				new Point(xPosition, 0),	// Top, right is at the top of the control
			};

			return points;
		}
		
		private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				var shiftKeyPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				var controlKeyPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

				_isSelectedChanged(ColorBandIndex, !IsSelected, shiftKeyPressed, controlKeyPressed);
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_displayContext(ColorBandIndex, ColorBandSelectionType.Cutoff);
				}
			}
		}

		#endregion

		#region Events

		internal event EventHandler<CbsSelectionLineMovedEventArgs>? SelectionLineMoved;

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public double XPosition
		{
			get => _xPosition;

			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _xPosition))
				{
					_xPosition = value;
					SelectionLinePosition = _xPosition * _scaleX;
				}
			}
		}

		public double ScaleX
		{
			get => _scaleX;

			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _scaleX))
				{
					_scaleX = value;
					SelectionLinePosition = _xPosition * _scaleX;
				}
			}
		}

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
					_dragLine.Y2 = value + CbHeight;

					_topArrowHalfWidth = (value - 2) / 2;
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

		#endregion

		#region Public Methods

		public bool UpdatePositionNotUsed(double newPosition)
		{
			if (DragState == DragState.Begun || DragState == DragState.InProcess)
			{
				// The HandleMouseMove eventHandler is managing the SelectionLine positions.
				return false;
			}

			var amount = newPosition - _originalSelectionLinePosition;

			if (ScreenTypeHelper.IsDoubleNearZero(amount))
			{
				return false;
			}

			if (UpdateColorBandWidth(amount))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. The new position is {newPosition}. The original position is {_originalSelectionLinePosition}.");
				SelectionLinePosition = newPosition;

				return true;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine::UpdatePosition. The call to UpdateColorBandWidth returned false. The new position is {newPosition}. The original position is {_originalSelectionLinePosition}.");
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

				_colorBandLayoutViewModel.PropertyChanged -= _colorBandLayoutViewModel_PropertyChanged;
				_topArrow.MouseUp -= Handle_MouseUp;

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

		public void StartDrag(RectangleGeometries rectangleGeometries)
		{
			if (DragState == DragState.InProcess)
			{
				throw new InvalidOperationException("Cannot start Drag, the DragState = InProcess.");
			}

			_rectangleGeometries = rectangleGeometries; 
			_originalSelectionLinePosition = SelectionLinePosition;

			DragState = DragState.InProcess;

			Debug.WriteLine($"Beginning to Drag the SelectionLine for ColorBandIndex: {ColorBandIndex}, the Geometries are: {rectangleGeometries}.");
		}

		public void CancelDrag()
		{
			DragState = DragState.None;

			if (SelectionLinePosition != _originalSelectionLinePosition)
			{
				SelectionLinePosition = _originalSelectionLinePosition;

				UpdateColorBandWidth(0);

				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, _originalSelectionLinePosition, CbsSelectionLineDragOperation.Cancel));
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

			var amount = pos.X - _originalSelectionLinePosition;

			if (UpdateColorBandWidth(amount))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. UpdateColorBandWidth returned true. The XPos is {pos.X}. The original position is {_originalSelectionLinePosition}.");
				SelectionLinePosition = pos.X;

				SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, pos.X, CbsSelectionLineDragOperation.Move));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. UpdateColorBandWidth returned false. The XPos is {pos.X}. The original position is {_originalSelectionLinePosition}.");
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
			var distance = Math.Abs(SelectionLinePosition - _originalSelectionLinePosition);

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
			
			if (_rectangleGeometries == null)
			{
				throw new InvalidOperationException("The RectangleGeometries is null.");
			}

			var left = _rectangleGeometries.Left;
			var right = _rectangleGeometries.Right;

			var selLeft = _rectangleGeometries.SelLeft;
			var selRight = _rectangleGeometries.SelRight;

			var originalLeftGeometry = _rectangleGeometries.OriginalLeftGeometry;
			var originalRightGeometry = _rectangleGeometries.OriginalRightGeometry;

			var updated = false;

			if (amount < 0)
			{
				amount = amount * -1;
				if (originalLeftGeometry.Rect.Width > amount + (1 * _scaleX) && originalRightGeometry.Rect.X > amount /*+ (1 * _scale)*/)
				{
					left.Rect = DrawingHelper.Shorten(originalLeftGeometry.Rect, amount);
					right.Rect = DrawingHelper.MoveRectLeft(originalRightGeometry.Rect, amount);

					//selLeft.Rect = DrawingHelper.CopyXAndWidth(left.Rect, selLeft.Rect);
					//selRight.Rect = DrawingHelper.CopyXAndWidth(right.Rect, selRight.Rect);

					Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. Shortening the Left ColorBandRectangle by amount: {amount}. " +
						$"Left Width: {originalLeftGeometry.Rect.Width / _scaleX}, Right Pos: {originalRightGeometry.Rect.X / _scaleX}" +
						$"New Left Width = {left.Rect.Width / _scaleX}; New Right Pos: {right.Rect.X / _scaleX}");
					
					updated = true;
				}
			}
			else
			{
				if (originalRightGeometry.Rect.Width > amount + (1 * _scaleX))
				{
					left.Rect = DrawingHelper.Lengthen(originalLeftGeometry.Rect, amount);
					right.Rect = DrawingHelper.MoveRectRight(originalRightGeometry.Rect, amount);

					//selLeft.Rect = DrawingHelper.CopyXAndWidth(left.Rect, selLeft.Rect);
					//selRight.Rect = DrawingHelper.CopyXAndWidth(right.Rect, selRight.Rect);

					Debug.WriteLineIf(_useDetailedDebug, $"CbsSelectionLine. Lengthening the Left ColorBandRectangle by amount: {amount}. " +
						$"Left Width: {originalLeftGeometry.Rect.Width / _scaleX}, Right Pos: {originalRightGeometry.Rect.X / _scaleX}" +
						$"New Left Width = {left.Rect.Width / _scaleX}; New Right Pos: {right.Rect.X / _scaleX}");

					updated = true;
				}
			}

			return updated;
		}

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

	internal class RectangleGeometries
	{
		public RectangleGeometry Left { get; set; }
		public RectangleGeometry Right { get; set; }

		public RectangleGeometry SelLeft { get; set; }
		public RectangleGeometry SelRight { get; set; }

		public RectangleGeometry OriginalLeftGeometry { get; init; }
		public RectangleGeometry OriginalRightGeometry { get; init; }

		public RectangleGeometries(RectangleGeometry left, RectangleGeometry right, RectangleGeometry selLeft, RectangleGeometry selRight)
		{
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
			SelLeft = selLeft ?? throw new ArgumentNullException(nameof(selLeft));
			SelRight = selRight ?? throw new ArgumentNullException(nameof(selRight));

			OriginalLeftGeometry = new RectangleGeometry(Left.Rect);
			OriginalRightGeometry = new RectangleGeometry(Right.Rect);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Left: {Left.Rect}");
			sb.AppendLine($"Right: {Right.Rect}");
			sb.AppendLine($"Left Original: {OriginalLeftGeometry.Rect}");
			sb.AppendLine($"Right Original: {OriginalRightGeometry.Rect}");

			return sb.ToString();
		}
	}
}
