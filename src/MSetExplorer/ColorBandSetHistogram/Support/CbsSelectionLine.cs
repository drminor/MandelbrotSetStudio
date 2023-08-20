using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static ScottPlot.Plottable.PopulationPlot;

namespace MSetExplorer
{
	internal class CbsSelectionLine
	{
		#region Private Fields

		private Canvas _canvas;
		private int _colorBandIndex;
		private double _originalXPosition;
		private ColorBandWidthUpdater _colorBandWidthUpdater;

		private readonly Line _dragLine;
		private DragState _dragState;

		private double _selectionLinePosition;
		private int _cbElevation;
		private int _cbHeight;

		#endregion

		#region Constructor

		public CbsSelectionLine(Canvas canvas, int elevation, int height, int colorBandIndex, double xPosition, ColorBandWidthUpdater colorBandWidthUpdater)
		{
			//_canvas = null;
			//_colorBandWidthUpdater = null;
			//_dragLine = BuildDragLine();
			//_dragState = DragState.None;

			//SelectionLinePosition = 0;
			//CbElevation = 0;
			//CbHeight = 0;


			_canvas = canvas;
			_colorBandIndex = colorBandIndex;
			_originalXPosition = xPosition;
			_colorBandWidthUpdater = colorBandWidthUpdater;

			//SelectionLinePosition = xPosition;

			_cbElevation = elevation;
			_cbHeight = height;
			_selectionLinePosition = xPosition;
			_dragLine = BuildDragLine(elevation, height, xPosition);

			_dragState = DragState.None;

			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);
			//_dragLine.Visibility = Visibility.Visible;
			_dragLine.KeyUp += DragLine_KeyUp;
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
				//Visibility = Visibility.Hidden,
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

		//public void Setup(Canvas canvas, int elevation, int height, double xPosition, ColorBandWidthUpdater colorBandWidthUpdater)
		//{
		//	_canvas = canvas;
		//	_colorBandWidthUpdater = colorBandWidthUpdater;
		//	CbElevation = elevation;
		//	CbHeight = height;
			
		//	SelectionLinePosition = xPosition;

		//	_canvas.Children.Add(_dragLine);

		//	//_dragLine.SetValue(Canvas.LeftProperty, SelectionLinePosition);
		//	//_dragLine.SetValue(Canvas.TopProperty, 0d);
		//	//_dragLine.Stroke = new SolidColorBrush(Colors.Purple);
		//	//_dragLine.StrokeThickness = 2;
		//	//_dragLine.Width = 4;
		//	//_dragLine.Height = CbHeight;

		//	_dragLine.SetValue(Panel.ZIndexProperty, 30);
		//	_dragLine.Visibility = Visibility.Visible;
		//}

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
			DragState = DragState.Begun;
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
								_canvas.MouseLeave -= HandleMouseLeave;
							}
							break;

						case DragState.Begun:

							if (_canvas != null)
							{
								_dragLine.Visibility = Visibility.Visible;

								if (!_dragLine.Focus())
								{
									Debug.WriteLine("WARNING: The SelectionLine did not receive the focus as the DragState was set to Begun.");
								}

								_canvas.MouseMove += HandleMouseMove;
								_canvas.MouseLeftButtonUp += HandleMouseLeftButtonUp;
								_canvas.MouseLeave += HandleMouseLeave;
							}
							break;

						case DragState.InProcess:
							// TODO: Check This -- is this needed?>
							_dragLine.Focus();
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

		private void DragLine_KeyUp(object sender, KeyEventArgs e)
		{
			if (DragState == DragState.None)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the DragLine not in drag.");
				return;
			}

			if (e.Key == Key.Escape)
			{
				Debug.WriteLine($"The {e.Key} was pressed on the DragLine -- cancelling drag.");
				CancelDrag();
			}
		}

		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var pos = e.GetPosition(relativeTo: _canvas);

				if (_colorBandWidthUpdater.Invoke(_colorBandIndex, _originalXPosition, pos.X))
				{
					SelectionLinePosition = pos.X;
				}
			}
			else
			{
				CancelDrag();
			}
		}

		//private void HandleMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		//{
		//	Debug.WriteLine($"The CbsSelectionLine at position: {SelectionLinePosition} got a MouseRightButtonDown event.");
		//}

		private void HandleMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLine($"The CbsSelectionLine is getting a MouseLeftButtonUp event. IsFocused = {_canvas?.IsFocused ?? false}. DragState = {DragState}.");

			if (DragState != DragState.None)
			{
				CompleteDrag();
			}
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

		#endregion

		#region Private Methods

		private void CompleteDrag()
		{
			SelectionLineMoved?.Invoke(this, new CbsSelectionLineMovedEventArgs(_colorBandIndex, SelectionLinePosition, isPreview: false));

			DragState = DragState.None;
		}

		private void CancelDrag()
		{
			DragState = DragState.None;

			SelectionLineMoved?.Invoke(this, CbsSelectionLineMovedEventArgs.CreateCancelPreviewInstance(_colorBandIndex));
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
