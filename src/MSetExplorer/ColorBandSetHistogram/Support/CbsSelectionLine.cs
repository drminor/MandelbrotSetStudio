using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbsSelectionLine
	{
		#region Private Fields

		private Canvas _canvas;
		private ColorBandWidthUpdater _colorBandWidthUpdater;

		private readonly Line _dragLine;
		private DragState _dragState;

		private double _originalXPosition;
		private double _selectionLinePosition;
		private int _cbElevation;
		private int _cbHeight;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsSelectionLine(Canvas canvas, int elevation, int height, int colorBandIndex, double xPosition, ColorBandWidthUpdater colorBandWidthUpdater)
		{
			_canvas = canvas;
			ColorBandIndex = colorBandIndex;
			_originalXPosition = xPosition;
			_selectionLinePosition = xPosition;
			_colorBandWidthUpdater = colorBandWidthUpdater;

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
				Y2 = height,
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
				Debug.WriteLine("SelectionRectangle encountered an exception in TearDown.");
			}
		}

		public void StartDrag()
		{
			DragState = DragState.InProcess;
		}

		public void CancelDrag(bool raiseCancelEvent)
		{
			DragState = DragState.None;

			if (raiseCancelEvent)
			{
				SelectionLineMoved?.Invoke(this, CbsSelectionLineMovedEventArgs.CreateCancelPreviewInstance(ColorBandIndex));
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
				CancelDrag(raiseCancelEvent: true);
				return;
			}

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				// The user lifted the left mouse button while the mouse was not on the canvas.
				CancelDrag(raiseCancelEvent: true);
				return;
			}

			var pos = e.GetPosition(relativeTo: _canvas);

			if (_colorBandWidthUpdater.Invoke(ColorBandIndex, _originalXPosition, pos.X))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The XPos is {pos.X}. The original position is {_originalXPosition}.");
				SelectionLinePosition = pos.X;
			}
		}

		private void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (DragState != DragState.None)
			{
				if (Keyboard.IsKeyDown(Key.Escape))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsSelectionLine is getting a MouseLeftButtonUp event. The Escape Key is Pressed, cancelling.");
					CancelDrag(raiseCancelEvent: true);
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

		private void CompleteDrag()
		{
			SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(ColorBandIndex, SelectionLinePosition, isPreview: false));

			DragState = DragState.None;
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
