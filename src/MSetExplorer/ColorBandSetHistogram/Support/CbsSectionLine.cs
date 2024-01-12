using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MSetExplorer
{
	internal class CbsSectionLine
	{
		#region Private Fields

		private const double SELECTION_LINE_ARROW_WIDTH = 6;
		private const double MIN_SEL_DISTANCE = 0.49;

		private static readonly Brush ACTIVE_STROKE = DrawingHelper.BuildSelectionDrawingBrush();
		private static readonly Brush DEFAULT_STROKE = new SolidColorBrush(Colors.DarkGray);

		private ColorBandLayoutViewModel _colorBandLayoutViewModel;
		private Canvas _canvas;
		private double _controlHeight;
		private double _xPosition;
		private double _cbElevation;
		private double _cbHeight;
		private double _scaleX;
		private IsSelectedChangedCallback _isSelectedChangedCallback;
		private Action<int, ColorBandSelectionType> _requestContextMenuShown;

		private double _selectionLinePosition;

		private readonly Line _dragLine;
		private readonly Polygon _topArrow;
		private double _topArrowHalfWidth;

		private DragState _dragState;

		private double _originalSectionLinePosition;

		private double? _leftWidth;
		private double? _rightWidth;
		private bool _updatingPrevious;

		private bool _isSelected;
		private bool _isUnderMouse;
		private bool _parentIsFocused;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbsSectionLine(int colorBandIndex, bool isSelected, double xPosition, ColorBandLayoutViewModel colorBandLayoutViewModel, Canvas canvas, IsSelectedChangedCallback isSelectedChangedCallback, Action<int, ColorBandSelectionType> requestContextMenuShown)
		{
			_isSelected = isSelected;
			_isUnderMouse = false;
			_dragState = DragState.None;

			ColorBandIndex = colorBandIndex;

			_colorBandLayoutViewModel = colorBandLayoutViewModel;
			_colorBandLayoutViewModel.PropertyChanged += ColorBandLayoutViewModel_PropertyChanged;
			
			_canvas = canvas;
			_controlHeight = _colorBandLayoutViewModel.ControlHeight;
			_xPosition = xPosition;

			_cbElevation = _colorBandLayoutViewModel.CbrElevation;
			_cbHeight = _colorBandLayoutViewModel.CbrHeight;

			_scaleX = _colorBandLayoutViewModel.ContentScale.Width;
			_parentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;

			_isSelectedChangedCallback = isSelectedChangedCallback;
			_requestContextMenuShown = requestContextMenuShown;

			_selectionLinePosition = _xPosition * _scaleX;
			_originalSectionLinePosition = _selectionLinePosition;

			_leftWidth = null;
			_rightWidth = null;
			_updatingPrevious = false;

			_dragLine = BuildDragLine(_selectionLinePosition, _colorBandLayoutViewModel);
			_canvas.Children.Add(_dragLine);
			_dragLine.SetValue(Panel.ZIndexProperty, 30);

			//_topArrowHalfWidth = (_cbElevation - 2) / 2;
			_topArrowHalfWidth = SELECTION_LINE_ARROW_WIDTH;
			_topArrow = BuildTopArrow(_selectionLinePosition, _colorBandLayoutViewModel);

			_topArrow.MouseUp += Handle_TopArrowMouseUp;
			//_topArrow.PreviewKeyDown += TopArrow_PreviewKeyDown;

			_canvas.Children.Add(_topArrow);
			_topArrow.SetValue(Panel.ZIndexProperty, 30);
		}

		private Line BuildDragLine(double selectionLinePosition, ColorBandLayoutViewModel layout)
		{
			var result = new Line()
			{
				Fill = Brushes.Transparent,
				Stroke = layout.ParentIsFocused ? ACTIVE_STROKE : DEFAULT_STROKE,
				StrokeThickness = 2,
				//StrokeDashArray = new DoubleCollection { 4, 4 },
				//Y1 = parentIsFocused ? elevation : 0,
				Y1 = layout.CbrElevation, // 0,
				Y2 = layout.CbrElevation + layout.CbrHeight,
				X1 = selectionLinePosition,
				X2 = selectionLinePosition,
			};

			return result;
		}

		private Polygon BuildTopArrow(double selectionLinePosition, ColorBandLayoutViewModel layout)
		{
			var result = new Polygon()
			{
				Fill = GetTopArrowFill(_isSelected),
				Stroke = Brushes.DarkGray,
				StrokeThickness = 2,
				Points = BuildTopAreaPoints(selectionLinePosition, layout),

				//Visibility = parentIsFocused ? Visibility.Visible : Visibility.Hidden
				//Visibility = Visibility.Hidden

				Visibility = _isUnderMouse ? Visibility.Visible : Visibility.Hidden
			};

			return result;
		}

		private Brush GetTopArrowFill(bool isSelected)
		{
			var result = isSelected ? Brushes.IndianRed : Brushes.Transparent;

			return result;
		}

		private PointCollection BuildTopAreaPoints(double xPosition, ColorBandLayoutViewModel layout)
		{
			var points = new PointCollection()
			{
				new Point(xPosition, layout.CbrElevation),				// The bottom of the arrow is positioned at the top of the band display
				new Point(xPosition - _topArrowHalfWidth, 0),	// Top, left is at the top of the control
				new Point(xPosition + _topArrowHalfWidth, 0),	// Top, right is at the top of the control and leaning forward into the next band
				//new Point(xPosition, 0),	// Top, right is at the top of the control
			};

			return points;
		}

		#endregion

		#region Events

		internal event EventHandler<CbsSectionLineMovedEventArgs>? SectionLineMoved;

		#endregion

		#region Public Properties

		public int ColorBandIndex { get; set; }

		public double ControlHeight
		{
			get => _controlHeight;
			set
			{
				if (value != _controlHeight)
				{
					_controlHeight = value;
					_dragLine.Y2 = CbElevation + CbHeight; // ControlHeight;
				}
			}
		}

		public double XPosition
		{
			get => _xPosition;

			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _xPosition))
				{
					_xPosition = value;
					SectionLinePosition = _xPosition * _scaleX;
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
					SectionLinePosition = _xPosition * _scaleX;
				}
			}
		}

		public double SectionLinePosition
		{
			get => _selectionLinePosition;
			set
			{
				if (value != _selectionLinePosition)
				{
					_selectionLinePosition = value;
					_dragLine.X1 = value;
					_dragLine.X2 = value;

					_topArrow.Points = BuildTopAreaPoints(_selectionLinePosition, _colorBandLayoutViewModel);
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
					//_dragLine.Y1 = ParentIsFocused ? CbElevation : 0;
					_dragLine.Y1 = CbElevation; // 0;

					//_topArrowHalfWidth = (value - 2) / 2;
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

		public bool IsUnderMouse
		{
			get => _isUnderMouse;
			set
			{
				if (value != _isUnderMouse)
				{
					_isUnderMouse = value;

					_topArrow.Visibility = _isUnderMouse ? Visibility.Visible : Visibility.Hidden;
					_dragLine.StrokeThickness = _isUnderMouse ? 3.0 : 2.0;
				}
			}
		}

		public bool ParentIsFocused
		{
			get => _parentIsFocused;

			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;

					//_dragLine.Stroke = _parentIsFocused ? ACTIVE_STROKE : DEFAULT_STROKE;
					//_dragLine.Y1 = _parentIsFocused ? CbElevation : 0;
					//_topArrow.Visibility = _parentIsFocused ? Visibility.Visible : Visibility.Hidden;

					_dragLine.Stroke = DEFAULT_STROKE;
					_dragLine.Y1 = _colorBandLayoutViewModel.CbrElevation; // 0;
					_topArrow.Visibility = _parentIsFocused
						?  _isUnderMouse
							? Visibility.Visible
							: Visibility.Hidden
						: Visibility.Hidden;


				}
			}
		}

		#endregion

		#region Public Methods

		public void StartDrag(double leftWidth, double rightWidth, bool updatingPrevious)
		{
			if (DragState != DragState.None)
			{
				throw new InvalidOperationException($"Cannot start Drag, the DragState = {DragState}.");
			}

			_leftWidth = leftWidth;
			_rightWidth = rightWidth;
			_updatingPrevious = updatingPrevious;
			_originalSectionLinePosition = SectionLinePosition;

			DragState = DragState.Begun;
		}

		public void CancelDrag()
		{
			if (DragState != DragState.None)
			{
				CancelDragInternal();
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

				_colorBandLayoutViewModel.PropertyChanged -= ColorBandLayoutViewModel_PropertyChanged;
				_topArrow.MouseUp -= Handle_TopArrowMouseUp;

				_canvas.Children.Remove(_dragLine);
				_canvas.Children.Remove(_topArrow);
			}
			catch
			{
				Debug.WriteLine("CbsSectionLine encountered an exception in TearDown.");
			}
		}

		#endregion

		#region Private Properties

		public DragState DragState
		{
			get => _dragState;
			private set
			{
				if (_dragState != value)
				{
					switch (value)
					{
						case DragState.None:

							_canvas.MouseMove -= HandleMouseMove;
							_canvas.PreviewMouseLeftButtonUp -= HandlePreviewMouseLeftButtonUp;
							ReleaseMouse();

							_leftWidth = null;
							_rightWidth = null;
							break;

						case DragState.InProcess:

							break;

						case DragState.Begun:
							CaptureMouse();
							_canvas.MouseMove += HandleMouseMove;
							_canvas.PreviewMouseLeftButtonUp += HandlePreviewMouseLeftButtonUp;
							break;

						default:
							break;
					}

					_dragState = value;
				}
			}
		}

		private bool CaptureMouse()
		{
			var result = Mouse.Capture(_canvas);
			if (result)
			{
				//Debug.WriteLine($"Beginning to Drag the SectionLine for ColorBandIndex: {ColorBandIndex}, LeftWidth: {_leftWidth}, RightWidth: {_rightWidth}.");
				_canvas.LostMouseCapture += Canvas_LostMouseCapture;
			}
			else
			{
				Debug.WriteLine($"Could not capture the mouse for ColorBandIndex: {ColorBandIndex}.");
			}

			return result;
		}

		private void ReleaseMouse()
		{
			_canvas.LostMouseCapture -= Canvas_LostMouseCapture;
			_canvas.ReleaseMouseCapture();
		}

		#endregion

		#region Event Handlers

		private void ColorBandLayoutViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandLayoutViewModel.ContentScale))
			{
				ScaleX = _colorBandLayoutViewModel.ContentScale.Width;
				_originalSectionLinePosition = SectionLinePosition;
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ControlHeight))
			{
				ControlHeight = _colorBandLayoutViewModel.ControlHeight;				
			}
			else if (e.PropertyName == nameof(ColorBandLayoutViewModel.ParentIsFocused))
			{
				ParentIsFocused = _colorBandLayoutViewModel.ParentIsFocused;
			}
		}

		private void HandleMouseMove(object sender, MouseEventArgs e)
		{
			var pos = e.GetPosition(relativeTo: _canvas);
			e.Handled = true;

			var amount = pos.X - _originalSectionLinePosition;

			if (DragState == DragState.Begun)
			{
				if (Math.Abs(amount) > MIN_SEL_DISTANCE)
				{
					DragState = DragState.InProcess;
				}
				else
				{
					return;
				}
			}

			if (IsNewPositionOk(amount))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. UpdateColorBandWidth returned true. The XPos is {pos.X}. The original position is {_originalSectionLinePosition}.");
				SectionLinePosition = pos.X;

				SectionLineMoved?.Invoke(this, new CbsSectionLineMovedEventArgs(ColorBandIndex, pos.X, _updatingPrevious, CbsSectionLineDragOperation.Move));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. UpdateColorBandWidth returned false. The XPos is {pos.X}. The original position is {_originalSectionLinePosition}.");
			}
		}

		private void HandlePreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (DragState == DragState.None)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. HandleMouseLeftButtonUp. Not Handling the DragState is {DragState}.");
				return;
			}

			var ht = VisualTreeHelper.HitTest(_canvas, Mouse.GetPosition(_canvas));
			var mouseWasOverCanvas = ht != null;

			if (!mouseWasOverCanvas)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsSectionLine is getting a MouseLeftButtonUp event. The mouse is not over the canvas, cancelling.");
				CancelDragInternal();
				return;
			}

			if (Keyboard.IsKeyDown(Key.Escape))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsSectionLine is getting a MouseLeftButtonUp event. The Escape Key is Pressed, cancelling.");
				CancelDragInternal();
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsSectionLine is getting a MouseLeftButtonUp event. Completing the Drag.");
				var dragCompleted = CompleteDrag();

				if (dragCompleted)
				{
					e.Handled = true;
				}
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. HandleMouseLeftButtonUp. MouseWasOverCanvas: {mouseWasOverCanvas}. The Keyboard focus is now on {Keyboard.FocusedElement}.");
		}

		private void Handle_TopArrowMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				NotifySelectionChanged();
				e.Handled = true;
			}
			else
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_requestContextMenuShown(ColorBandIndex, ColorBandSelectionType.Cutoff);
					e.Handled = true;
				}
			}
		}

		private void Canvas_LostMouseCapture(object sender, MouseEventArgs e)
		{
			Debug.WriteLine($"WARNING: CbsSectionLine Lost the MouseCapture. Source: {e.Source}. Sender: {sender}. Original Source: {e.MouseDevice}");
			CancelDrag();
		}

		#endregion

		#region Private Methods

		private void NotifySelectionChanged()
		{
			_isSelectedChangedCallback(ColorBandIndex, ColorBandSelectionType.Color);
		}

		private bool CompleteDrag()
		{
			var distance = Math.Abs(SectionLinePosition - _originalSectionLinePosition);

			if (distance > MIN_SEL_DISTANCE)
			{
				SectionLineMoved?.Invoke(this, new CbsSectionLineMovedEventArgs(ColorBandIndex, SectionLinePosition, _updatingPrevious, CbsSectionLineDragOperation.Complete));
				DragState = DragState.None;
				return true;
			}
			else
			{
				CancelDragInternal();
				return false;
			}
		}

		private void CancelDragInternal()
		{
			if (SectionLinePosition != _originalSectionLinePosition)
			{
				SectionLinePosition = _originalSectionLinePosition;
			}

			if (DragState == DragState.InProcess)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. CancelDragInternal. Drag was InProcess, raising SectionLineMoved with Operation = Cancel.");
				SectionLineMoved?.Invoke(this, new CbsSectionLineMovedEventArgs(ColorBandIndex, _originalSectionLinePosition, _updatingPrevious, CbsSectionLineDragOperation.Cancel));
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbsSectionLine. CancelDragInternal. Drag was only Begun, raising SectionLineMoved with Operation = NotStarted.");
				SectionLineMoved?.Invoke(this, new CbsSectionLineMovedEventArgs(ColorBandIndex, _originalSectionLinePosition, _updatingPrevious, CbsSectionLineDragOperation.NotStarted));
			}

			DragState = DragState.None;
		}

		private bool IsNewPositionOk(double amount)
		{
			if ( !(_leftWidth.HasValue && _rightWidth.HasValue)  )
			{
				throw new InvalidOperationException("The LeftWidth or RightWidth is null.");
			}

			bool result;

			if (amount < 0)
			{
				amount = amount * -1;
				result = _leftWidth > amount + (1 * _scaleX);
			}
			else
			{
				result = _rightWidth > amount + (1 * _scaleX);
			}

			return result;
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

		//	Debug.WriteLine($"The CbsSectionLine is getting a Mouse Left Button Down at {position}.");
		//}

		//private void SelectedArea_MouseWheel(object sender, MouseWheelEventArgs e)
		//{
		//	Debug.WriteLine("The CbsSectionLine received a MouseWheel event.");
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
